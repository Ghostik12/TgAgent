using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TgBotYandexMar.DB;
using TgBotYandexMar.Models;
using TgBotYandexMar.Services;

namespace TgBotYandexMar.Controller
{
    public class TextMessageController
    {
        private readonly ITelegramBotClient _client;
        private bool _isAddingChannel = false;
        private readonly IServiceScopeFactory _scopeFactory;
        private Dictionary<long, int> _selectedChannels = new(); // chatId -> channelId
        private bool _isConfiguringPost = false; // Флаг для настройки сборки поста
        private string _postComponents; // Список для хранения введенных компонентов
        private bool _isWaitingForAuthCode = false;
        private bool _isMaxPost = false;
        private readonly HttpClient _httpClient;
        private ChannelService _channelService;
        private (string ChangeType, KeywordSetting KeywordSetting) _changeWords;

        public TextMessageController(ITelegramBotClient client, IServiceScopeFactory serviceScopeFactory, HttpClient httpClient, ChannelService channelService)
        {
            _client = client;
            _scopeFactory = serviceScopeFactory;
            _httpClient = httpClient;
            _channelService = channelService;
        }

        internal async Task CheckUserOrAdmin(Update update)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var check = await CheckAdmins(update.Message.From.Id);
                if (check == false)
                    return;

                await _client.SendTextMessageAsync(update.Message.From.Id, "Привет! Я бот для работы с Яндекс.Маркетом.");
                _isConfiguringPost = false;
                _postComponents = "";
                _selectedChannels.Clear();
                _isWaitingForAuthCode = false;
                _isAddingChannel = false;

                await SendMainMenu(update.Message.From.Id);
            }
        }

        internal async Task Handle(Update update, CancellationToken cancellationToken)
        {
            if (update.Message == null)
            {
                Console.WriteLine("Сообщение не содержит данных.");
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var messageText = update.Message.Text;
                var yorn = await CheckAdmins(update.Message.From.Id);
                if (yorn == false) return;

                // Обработка команд
                switch (messageText)
                {
                    case "🤖Добавить канал":
                        _isAddingChannel = true;
                        await _client.SendTextMessageAsync(update.Message.From.Id, "Введите данные канала в формате:\n" +
                            "Название, ID канала, Количество товаров для парсинга, Максимальное количество постов в день, " +
                            "ключевые слова (через ;), частота парсинга для каждого слова Ч:М:С(через ;), " +
                            "частота постинга для каждого слова Ч:М:С(через ;), API-ключ, CLID, Client ID, ClientSecret, RedirectUrl");
                        var menu = new ReplyKeyboardMarkup(new[]
                        {
                        new[] { new KeyboardButton("Назад") }
                    })
                        {
                            ResizeKeyboard = true
                        };
                        await _client.SendTextMessageAsync(update.Message.From.Id, "Назад", replyMarkup: menu);
                        break;
                    case "⚙️Настройка каналов":
                        await ShowChannelList(update.Message.From.Id);
                        break;
                    case "🧹Очистить таблицу товаров":
                        //await ClearProductsTable(update.Message.From.Id);
                        break;
                    case "🔑 Авторизоваться":
                        if (!_selectedChannels.TryGetValue(update.Message.From.Id, out var channelId))
                        {
                            await _client.SendTextMessageAsync(update.Message.From.Id, "Сначала выберите канал для авторизации.");
                            return;
                        }

                        var channel = await dbContext.Channels.FindAsync(channelId);
                        if (channel == null)
                        {
                            await _client.SendTextMessageAsync(update.Message.From.Id, "Канал не найден.");
                            return;
                        }

                        var oauthUrl = GenerateOAuthUrl(channel.ClientId, channel.RedirectUrl);
                        await _client.SendTextMessageAsync(update.Message.From.Id, $"Перейдите по ссылке для авторизации: {oauthUrl}\nПосле авторизации введите код, который вы получите.");
                        _isWaitingForAuthCode = true; // Устанавливаем флаг ожидания кода
                        break;
                    case "Изменить максимальное количество постов":
                        if (_selectedChannels[update.Message.From.Id] == null)
                        {
                            await _client.SendTextMessageAsync(update.Message.From.Id, "Канал не найден.");
                            return;
                        }
                        _isMaxPost = true;
                        await _client.SendTextMessageAsync(update.Message.From.Id, "Введите новое максимальное количество постов:");
                        break;
                    case "Изменить ключевые слова":
                        EditKeywords(update.Message.From.Id);
                        break;
                    case "📊Статистика":
                        await ShowStatisticChannels(update.Message.From.Id);
                        break;
                    case "Назад":
                        _isConfiguringPost = false;
                        _postComponents = "";
                        _selectedChannels.Clear();
                        _isWaitingForAuthCode = false;
                        _isAddingChannel = false;
                        await SendMainMenu(update.Message.From.Id);
                        break;
                    default:
                        if (_isAddingChannel)
                        {
                            await AddChannel(update.Message.From.Id, messageText);
                        }
                        else if (_isConfiguringPost)
                        {
                            await HandlePostConfiguration(update.Message.From.Id, messageText);
                        }
                        else if(_changeWords.ChangeType != null && _changeWords.KeywordSetting != null)
                        {
                            await ChangeWords(update.Message.From.Id, messageText);
                        }
                        else if (_isMaxPost)
                        {
                            await ChangeMaxPost(update.Message.From.Id, messageText);
                        }
                        else if (_isWaitingForAuthCode)
                        {
                            try
                            {
                                var selectedChannel = await dbContext.Channels.FirstOrDefaultAsync(c => c.Id == _selectedChannels[update.Message.From.Id]);
                                if (selectedChannel == null)
                                {
                                    await _client.SendTextMessageAsync(update.Message.From.Id, "Канал не найден1.");
                                    return;
                                }

                                // Получаем OAuth-токен
                                var oauthToken = await GetOAuthTokenAsync(selectedChannel.ClientId, selectedChannel.ClientSecret, selectedChannel.RedirectUrl, messageText);
                                await SaveOAuthTokenAsync(selectedChannel.Id, oauthToken, 3600); // Сохраняем токен на 1 час

                                await _client.SendTextMessageAsync(update.Message.From.Id, "Токен успешно получен и сохранен!");
                                _channelService.AddChannel(selectedChannel);
                                _isWaitingForAuthCode = false; // Сбрасываем флаг

                            }
                            catch (Exception ex)
                            {
                                await _client.SendTextMessageAsync(update.Message.From.Id, $"Ошибка при получении токена: {ex.Message}");
                            }
                        }
                        else if (_selectedChannels.ContainsKey(update.Message.From.Id))
                        {
                            await HandleSettingsUpdate(update.Message.From.Id, messageText);
                        }
                        else
                        {
                            await HandleChannelSelection(update.Message.From.Id, messageText);
                        }
                        break;
                }
            }
        }

        private async Task ShowStatisticChannels(long chatId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var channels = await dbContext.Channels.ToListAsync();

                var stats = new StringBuilder();
                foreach (var channel in channels)
                {
                    var channelStat = await dbContext.ChannelStats.FirstOrDefaultAsync(cs => cs.ChannelId == channel.Id);
                    var postedCount = channelStat.PostedCount;
                    var notPostedCount = await dbContext.Products
                        .CountAsync(p => p.ChannelId == channel.Id && !p.IsPosted);

                    stats.AppendLine($"Канал: {channel.Name}");
                    stats.AppendLine($"- Опубликовано сегодня: {postedCount}/{channel.MaxPostsPerDay}");
                    stats.AppendLine($"- Не опубликовано: {channelStat.FailedCount}");
                    stats.AppendLine();
                }
                    await _client.SendTextMessageAsync(chatId, stats.ToString());
            }
        }

        private async Task ChangeMaxPost(long chatId, string newValue)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var channel = await _dbContext.Channels.FirstOrDefaultAsync(c => c.Id == _selectedChannels[chatId]);
                if (int.TryParse(newValue, out var maxPostsPerDay))
                {
                    channel.MaxPostsPerDay = maxPostsPerDay;
                    // Проверяем условия для возобновления парсинга
                    _channelService.AddChannel(channel);
                    await _client.SendTextMessageAsync(chatId, $"Максимальное количество постов в день изменено на {maxPostsPerDay}.");
                }
                else
                {
                    await _client.SendTextMessageAsync(chatId, "Неверный формат числа.");
                    return;
                }
                _dbContext.Channels.Update(channel);
                _isMaxPost = false;

                // Сохраняем изменения в базе данных
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task ChangeWords(long chatId, string newValue)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var parts = newValue.Split(',');
                var keyword = parts[0].Trim();
                if (parts.Length != 3)
                    return;
                TimeSpan.TryParse(parts[1].Trim(), out var parseFreq);
                TimeSpan.TryParse(parts[2].Trim(), out var postFreq);
                var channel = _selectedChannels[chatId];
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                switch (_changeWords.ChangeType)
                {
                    case "edit_keyword": // Обработка изменения слов для парсинга
                        var keywordSetting = _changeWords.KeywordSetting as KeywordSetting;

                        if (parts.Length == 3 && TimeSpan.TryParse(parts[1].Trim(), out var parseFrequency) && TimeSpan.TryParse(parts[2].Trim(), out var postFrequency))
                        {
                            keywordSetting.Keyword = keyword;
                            keywordSetting.ParseFrequency = parseFrequency;
                            keywordSetting.PostFrequency = postFrequency;

                            // Обновляем или создаем запись в KeywordStat
                            var keywordStat = await _dbContext.KeywordStats
                                .FirstOrDefaultAsync(k => k.KeywordSettingId == keywordSetting.Id);

                            if (keywordStat != null)
                            {
                                // Если запись существует, обновляем ключевое слово и сбрасываем счетчик
                                keywordStat.ParsedCount = 0; // Сбрасываем счетчик
                                keywordStat.LastParsedAt = DateTime.UtcNow;
                                _dbContext.KeywordStats.Update(keywordStat);
                            }
                            else
                            {
                                // Если записи нет, создаем новую
                                keywordStat = new KeywordStat
                                {
                                    KeywordSettingId = keywordSetting.Id,
                                    ParsedCount = 0, // Начинаем с нулевого счетчика
                                    LastParsedAt = DateTime.UtcNow
                                };
                                _dbContext.KeywordStats.Add(keywordStat);
                            }

                            _dbContext.KeywordSettings.Update(keywordSetting);
                            await _dbContext.SaveChangesAsync();
                            await _client.SendTextMessageAsync(chatId, "Настройки ключевого слова успешно обновлены!");
                        }
                        else
                        {
                            await _client.SendTextMessageAsync(chatId, "Неверный формат данных. Используйте формат: 'ключевое слово, частота парсинга, частота постинга' (например, 'магнитола, 6:0:0, 2:0:0')");
                        }
                        break;
                    case "add_keyword":
                        var partsA = newValue.Split(',');
                        if (partsA.Length == 3)
                        {
                            var keywordA = partsA[0].Trim();
                            if (TimeSpan.TryParse(partsA[1].Trim(), out var parseFrequencyA) && TimeSpan.TryParse(partsA[2].Trim(), out var postFrequencyA))
                            {
                                var channelA = await _dbContext.Channels.FirstOrDefaultAsync(c => c.Id == channel);

                                channelA.KeywordSettings.Add(new KeywordSetting
                                {
                                    Keyword = keywordA,
                                    ParseFrequency = parseFrequencyA,
                                    PostFrequency = postFrequencyA
                                });

                                await _dbContext.SaveChangesAsync();
                                await _client.SendTextMessageAsync(chatId, "Ключевое слово успешно добавлено!");
                            }
                            else
                            {
                                await _client.SendTextMessageAsync(chatId, "Неверный формат данных. Используйте формат: 'ключевое слово, частота парсинга, частота постинга' (например, 'магнитола, 6:0:0, 2:0:0')");
                            }
                        }
                        break;
                    default:
                        await _client.SendTextMessageAsync(chatId, "Неизвестный тип изменения.");
                        return;
                }

                // Сохраняем изменения в базе данных
                await _dbContext.SaveChangesAsync();

                var channelS = await _dbContext.Channels.FirstOrDefaultAsync(c => c.Id == channel);
                // Запускаем новые таймеры с обновленными настройками (если канал активен)
                if (channelS.IsActive)
                {
                    _channelService.StartKeywordTimer(keyword, parseFreq, postFreq);
                }

                await _client.SendTextMessageAsync(chatId, "Изменение успешно применено!");

                // Очищаем переменные
                _changeWords = (null, null);

                // Возвращаем пользователя в меню настройки канала
                await ShowChannelSettings(chatId);
            }
        }

        private async Task EditKeywords(long chatId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Получаем ключевые слова для канала из таблицы KeywordSettings
                var keywordSettings = await dbContext.KeywordSettings
                    .Where(k => k.ChannelId == _selectedChannels[chatId])
                    .ToListAsync();

                // Создаем кнопки для каждого ключевого слова
                var buttons = keywordSettings
                    .Select(k => new[]
                    {
                InlineKeyboardButton.WithCallbackData
                (
                    $"{k.Keyword} (Парсинг: {k.ParseFrequency}, Постинг: {k.PostFrequency})",
                    $"edit_keyword_{k.Id}"
                )
                    })
                    .ToList();

                // Добавляем кнопку для добавления нового ключевого слова
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Добавить ключевое слово", "add_keyword") });

                // Создаем меню с кнопками
                var menu = new InlineKeyboardMarkup(buttons);

                // Отправляем сообщение с меню
                await _client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Выберите ключевое слово для редактирования:",
                    replyMarkup: menu
                );
            }
        }

        private async Task<string> GetOAuthTokenAsync(string clientId, string clientSecret, string redirectUrl, string authCode)
        {
            var tokenUrl = "https://oauth.yandex.ru/token";
            var tokenRequestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authCode),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUrl)
            });

            var tokenResponse = await _httpClient.PostAsync(tokenUrl, tokenRequestBody);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка при получении токена доступа: {tokenResponse.StatusCode}");
            }

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenResult = JsonSerializer.Deserialize<TokenResponse>(tokenContent);

            if (tokenResult == null || string.IsNullOrEmpty(tokenResult.AccessToken))
            {
                Console.WriteLine("Не удалось получить токен доступа.");
            }

            return tokenResult.AccessToken;
        }

        private async Task SaveOAuthTokenAsync(int channelId, string token, int expiresIn)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var channel = await dbContext.Channels.FindAsync(channelId);

                if (channel == null)
                {
                    throw new Exception("Канал не найден.");
                }

                channel.OAuthToken = token;
                channel.OAuthTokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
                await dbContext.SaveChangesAsync();
            }
        }

        private async Task HandlePostConfiguration(long chatId, string messageText)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var channel = await dbContext.Channels
                        .Include(c => c.PostSettings)
                        .FirstOrDefaultAsync(c => c.Id == _selectedChannels[chatId]);
                    // Если PostSettings отсутствует, создаем новый объект
                    if (channel.PostSettings == null)
                    {
                        channel.PostSettings = new PostSettings
                        {
                            PriceTemplate = "",
                            TitleTemplate = "",
                            CaptionTemplate = "",
                            Order = "",
                            ChannelId = channel.Id, // Устанавливаем связь с каналом
                            ShowRating = true,     // Значения по умолчанию
                            ShowOpinionCount = true
                        };
                        dbContext.PostSetting.Add(channel.PostSettings); // Добавляем в контекст
                    }
                    switch (_postComponents)
                    {
                        case "PostSettings_First":
                            if (messageText.Trim().ToLower() == "цена")
                            {
                                channel.PostSettings.PriceTemplate = "1"; // Например, "1" для цены
                                channel.PostSettings.Order = "Price,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите второе значение (название или подпись):");
                                _postComponents = "PostSettings_Second";
                            }
                            else if (messageText.Trim().ToLower() == "название")
                            {
                                channel.PostSettings.TitleTemplate = "1"; // Например, "1" для названия
                                channel.PostSettings.Order = "Title,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите второе значение (цена или подпись):");
                                _postComponents = "PostSettings_Second";
                            }
                            else
                            {
                                channel.PostSettings.CaptionTemplate = messageText; // Подпись
                                channel.PostSettings.Order = "Caption,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите второе значение (цена или название):");
                                _postComponents = "PostSettings_Second";
                            }
                            await dbContext.SaveChangesAsync();
                            return;
                        case "PostSettings_Second":
                            if (messageText.Trim().ToLower() == "цена")
                            {
                                channel.PostSettings.PriceTemplate = "2"; // Например, "2" для цены
                                channel.PostSettings.Order += "Price,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите третье значение (название или подпись):");
                                _postComponents = "PostSettings_Third";
                            }
                            else if (messageText.Trim().ToLower() == "название")
                            {
                                channel.PostSettings.TitleTemplate = "2"; // Например, "2" для названия\
                                channel.PostSettings.Order += "Title,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите третье значение (цена или подпись):");
                                _postComponents = "PostSettings_Third";
                            }
                            else
                            {
                                channel.PostSettings.CaptionTemplate = messageText; // Подпись
                                channel.PostSettings.Order += "Caption,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите третье значение (цена или название):");
                                _postComponents = "PostSettings_Third";
                            }
                            await dbContext.SaveChangesAsync();
                            return;
                        case "PostSettings_Third":
                            if (messageText.Trim().ToLower() == "цена")
                            {
                                channel.PostSettings.PriceTemplate = "3"; // Например, "3" для цены
                                channel.PostSettings.Order += "Price"; // Добавляем порядок
                            }
                            else if (messageText.Trim().ToLower() == "название")
                            {
                                channel.PostSettings.TitleTemplate = "3"; // Например, "3" для названия
                                channel.PostSettings.Order += "Title"; // Добавляем порядок
                            }
                            else
                            {
                                channel.PostSettings.CaptionTemplate = messageText; // Подпись
                                channel.PostSettings.Order += "Caption"; // Добавляем порядок
                            }
                            await dbContext.SaveChangesAsync();
                            await _client.SendTextMessageAsync(chatId, "Настройки сборки поста сохранены.");
                            _postComponents = "";
                            _isConfiguringPost = false;
                            break;
                    }
                    //await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в сборке: {ex.ToString()}");
            }
        }

        private async Task HandleChannelSelection(long chatId, string? channelName)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var channel = await dbContext.Channels
                    .FirstOrDefaultAsync(c => c.Name == channelName);

                if (channel == null)
                {
                    await _client.SendTextMessageAsync(chatId, "Канал не найден.");
                    return;
                }

                // Сохраняем выбранный канал
                _selectedChannels[chatId] = channel.Id;

                // Выводим настройки канала
                await ShowChannelSettings(chatId);
            }
        }

        private async Task ShowChannelSettings(long chatId)
        {
            if (!_selectedChannels.TryGetValue(chatId, out var channelId))
            {
                await _client.SendTextMessageAsync(chatId, "Канал не выбран.");
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var channel = await dbContext.Channels
                    .Include(c => c.KeywordSettings)
                    .Include(c => c.PostSettings)
                    .FirstOrDefaultAsync(c => c.Id == channelId);

                if (channel == null)
                {
                    await _client.SendTextMessageAsync(chatId, "Канал не найден.");
                    return;
                }

                // Выводим текущие настройки
                var settingsMessage = $"Текущие настройки канала {channel.Name}:\n" +
                                     $"Точный поиск: {(channel.UseExactMatch ? "Включен" : "Выключен")}\n" +
                                     $"Использовать минимальную цену: {(channel.UseLowPrice ? "Включено" : "Выключено")}\n" +
                                     $"Показывать рейтинг: {(channel.ShowRating ? "Включен" : "Выключен")}\n" +
                                     $"Показывать количество отзывов: {(channel.ShowOpinionCount ? "Включено" : "Выключено")}\n";

                await _client.SendTextMessageAsync(chatId, settingsMessage);

                // Предлагаем изменить настройки
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("🔑 Авторизоваться") },
                    new[] { new KeyboardButton("Изменить точный поиск") },
                    new[] { new KeyboardButton("Изменить использование минимальной цены") },
                    new[] { new KeyboardButton("Изменить показ рейтинга") },
                    new[] { new KeyboardButton("Изменить показ отзывов") },
                    new[] { new KeyboardButton("Изменить ключевые слова") },
                    new[] { new KeyboardButton("Статистика канала") },
                    new[] { new KeyboardButton("Изменить максимальное количество постов") },
                    new[] { new KeyboardButton("Настроить сборку поста") },
                    new[] { new KeyboardButton("Назад") },
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };

                await _client.SendTextMessageAsync(chatId, "Выберите настройку для изменения:", replyMarkup: keyboard);
            }
        }

        private async Task HandleSettingsUpdate(long chatId, string? messageText)
        {
            if (!_selectedChannels.TryGetValue(chatId, out var channelId))
            {
                await _client.SendTextMessageAsync(chatId, "Канал не выбран.");
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var channel = await dbContext.Channels
                    .FirstOrDefaultAsync(c => c.Id == channelId);

                if (channel == null)
                {
                    await _client.SendTextMessageAsync(chatId, "Канал не найден.");
                    return;
                }

                switch (messageText)
                {
                    case "Изменить точный поиск":
                        channel.UseExactMatch = !channel.UseExactMatch;
                        await dbContext.SaveChangesAsync();
                        await _client.SendTextMessageAsync(chatId, $"Точный поиск теперь {(channel.UseExactMatch ? "включен" : "выключен")}.");
                        break;

                    case "Изменить использование минимальной цены":
                        channel.UseLowPrice = !channel.UseLowPrice;
                        await dbContext.SaveChangesAsync();
                        await _client.SendTextMessageAsync(chatId, $"Использование минимальной цены теперь {(channel.UseLowPrice ? "включено" : "выключено")}.");
                        break;

                    case "Изменить показ рейтинга":
                        channel.ShowRating = !channel.ShowRating;
                        await dbContext.SaveChangesAsync();
                        await _client.SendTextMessageAsync(chatId, $"Показ рейтинга теперь {(channel.ShowRating ? "включен" : "выключен")}.");
                        break;

                    case "Изменить показ отзывов":
                        channel.ShowOpinionCount = !channel.ShowOpinionCount;
                        await dbContext.SaveChangesAsync();
                        await _client.SendTextMessageAsync(chatId, $"Показ отзывов теперь {(channel.ShowOpinionCount ? "включен" : "выключен")}.");
                        break;

                    case "Настроить сборку поста":
                        _isConfiguringPost = true;
                        _postComponents = "PostSettings_First";
                        await _client.SendTextMessageAsync(chatId, "Введите первый элемент поста (цена, название или подпись):");
                        break;

                    case "Статистика канала":
                        await ChannelStats(chatId);
                        break;

                    case "Назад":
                        await ShowChannelList(chatId);
                        break;
                }
            }
        }

        private async Task ChannelStats(long chatId)
        {
            if (!_selectedChannels.TryGetValue(chatId, out var channelId))
            {
                await _client.SendTextMessageAsync(chatId, "Канал не выбран.");
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var channelStat = await dbContext.ChannelStats
                    .Include(cs => cs.Channel)
                    .FirstOrDefaultAsync(cs => cs.ChannelId == channelId);

                if (channelStat == null)
                {
                    await _client.SendTextMessageAsync(chatId, "Статистика для канала не найдена.");
                    return;
                }

                var statsMessage = $"Статистика для канала {channelStat.Channel.Name}:\n" +
                                   $"Опубликовано постов: {channelStat.PostedCount}\n" +
                                   $"Неудачных попыток: {channelStat.FailedCount}\n" +
                                   $"Последнее обновление: {channelStat.LastUpdatedAt:dd.MM.yyyy HH:mm}";

                await _client.SendTextMessageAsync(chatId, statsMessage);
            }
        }

        private async Task ShowChannelList(long chatId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var channels = await dbContext.Channels
                    .ToListAsync();

                if (channels.Count == 0)
                {
                    await _client.SendTextMessageAsync(chatId, "У вас нет добавленных каналов.");
                    return;
                }

                // Создаем кнопки для каждого канала
                var buttons = channels.Select(channel => new[]
                {
            new KeyboardButton(channel.Name)
        }).ToArray();

                var replyMarkup = new ReplyKeyboardMarkup(buttons)
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };

                await _client.SendTextMessageAsync(chatId, "Выберите канал для настройки:", replyMarkup: replyMarkup);
            }
        }

        private async Task AddChannel(long chatId, string messageText)
        {
            try
            {
                // Парсим введенные данные
                var parts = messageText.Split(',');
                if (parts.Length != 12)
                {
                    await _client.SendTextMessageAsync(chatId, "Неверный формат данных. Пожалуйста, введите данные в правильном формате.");
                    return;
                }

                // Создаем новый канал
                var channel = new Channel
                {
                    Name = parts[0].Trim(),
                    ChatId = parts[1].Trim(),
                    ParseCount = int.Parse(parts[2].Trim()),
                    MaxPostsPerDay = int.Parse(parts[3].Trim()),
                    ApiKey = parts[7].Trim(), // API-ключ
                    Clid = parts[8].Trim(),   // CLID
                    ClientId = parts[9].Trim(), // Client ID
                    ClientSecret = parts[10].Trim(), // Client Secret
                    RedirectUrl = parts[11].Trim(), // Redirect URL
                    OAuthToken = "пусто", // Инициализируем OAuth-токен
                    UseExactMatch = false,   // По умолчанию выключен
                    UseLowPrice = false,      // По умолчанию выключен
                    ShowRating = false,       // По умолчанию выключен
                    ShowOpinionCount = false, // По умолчанию выключен
                    OAuthTokenExpiresAt = DateTime.UtcNow,
                    KeywordSettings = new List<KeywordSetting>()
                };

                // Парсим ключевые слова и их настройки
                var keywords = parts[4].Split(';');
                var parseFrequencies = parts[5].Split(';');
                var postFrequencies = parts[6].Split(';');

                if (keywords.Length != parseFrequencies.Length || keywords.Length != postFrequencies.Length)
                {
                    await _client.SendTextMessageAsync(chatId, "Количество ключевых слов, частот парсинга и постинга должно совпадать.");
                    return;
                }

                for (int i = 0; i < keywords.Length; i++)
                {
                    channel.KeywordSettings.Add(new KeywordSetting
                    {
                        Keyword = keywords[i].Trim(),
                        ParseFrequency = TimeSpan.Parse(parseFrequencies[i].Trim()),
                        PostFrequency = TimeSpan.Parse(postFrequencies[i].Trim())
                    });
                }

                // Добавляем канал в базу данных
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    dbContext.Channels.Add(channel);
                    await dbContext.SaveChangesAsync();
                }

                await _client.SendTextMessageAsync(chatId, "Канал успешно добавлен!");
                _isAddingChannel = false;
                _channelService.AddChannel(channel);
            }
            catch (Exception ex)
            {
                await _client.SendTextMessageAsync(chatId, $"Ошибка при добавлении канала: {ex.Message}");
            }
        }

        private async Task SendMainMenu(long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
            new[] { new KeyboardButton("🤖Добавить канал"), new KeyboardButton("⚙️Настройка каналов") },
            new[] { new KeyboardButton("📊Статистика"), new KeyboardButton("🧹Очистить таблицу товаров") }
            })
            {
                ResizeKeyboard = true
            };

            await _client.SendTextMessageAsync(
                chatId: chatId,
                text: "🗄Главное меню:",
                replyMarkup: replyKeyboard);
        }

        private async Task<bool> CheckAdmins(long chatId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var admin = await dbContext.Admin.FirstOrDefaultAsync(a => a.ChatId == chatId);
                if (admin != null)
                    return true;
                else
                    return false;
            }
        }

        private string GenerateOAuthUrl(string clientId, string redirectUrl)
        {
            return $"https://oauth.yandex.ru/authorize?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUrl)}";
        }

        internal async Task BotClient_OnCallbackQuery(CallbackQuery? callbackQuery)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var chatId = callbackQuery.Message.Chat.Id;
                var data = callbackQuery.Data;


                if (data.StartsWith("edit_keyword_"))
                {
                    var keywordId = int.Parse(data.Replace("edit_keyword_", ""));
                    var keywordSetting = await _dbContext.KeywordSettings.FirstOrDefaultAsync(k => k.Id == keywordId);

                    if (keywordSetting != null)
                    {
                        await _client.SendTextMessageAsync(chatId, $"Редактирование ключевого слова: {keywordSetting.Keyword}\n" +
                            $"Текущая частота парсинга: {keywordSetting.ParseFrequency}\n" +
                            $"Текущая частота постинга: {keywordSetting.PostFrequency}\n" +
                            "Введите новое ключевое слово, частоту парсинга и частоту постинга в формате: 'ключевое слово, частота парсинга, частота постинга' (например, 'магнитола, 6:0:0, 2:0:0')\"");

                        _changeWords = ("edit_keyword", keywordSetting);
                    }
                }
                else if (data == "add_keyword")
                {
                    await _client.SendTextMessageAsync(chatId, "Введите новое ключевое слово и его настройки в формате: 'ключевое слово, частота парсинга, частота постинга' (например, 'магнитола, 6:0:0, 2:0:0')");
                    _changeWords = ("add_keyword", _changeWords.KeywordSetting);
                }
                else
                {
                    await _client.SendTextMessageAsync(chatId, "Канал не найден.");
                }
            }
        }

        public class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; } // Время жизни токена в секундах

            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; } // Опционально, если нужен refresh token
        }
    }
}