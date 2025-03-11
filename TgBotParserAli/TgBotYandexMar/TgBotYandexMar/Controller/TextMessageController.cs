using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
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
        private List<string> _postComponents = new(); // Список для хранения введенных компонентов
        private bool _isWaitingForAuthCode = false;
        private readonly HttpClient _httpClient;
        private ChannelService _channelService;

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
                _postComponents.Clear();
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
                    case "Назад":
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
                throw new Exception($"Ошибка при получении токена доступа: {tokenResponse.StatusCode}");
            }

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenResult = JsonSerializer.Deserialize<TokenResponse>(tokenContent);

            if (tokenResult == null || string.IsNullOrEmpty(tokenResult.AccessToken))
            {
                throw new Exception("Не удалось получить токен доступа.");
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
            if (_postComponents.Count == 0)
            {
                // Обработка первого элемента
                if (messageText.Equals("цена", StringComparison.OrdinalIgnoreCase))
                {
                    _postComponents.Add("Price");
                    await _client.SendTextMessageAsync(chatId, "Введите второй элемент поста (название или подпись):");
                }
                else if (messageText.Split(' ').Length == 1)
                {
                    _postComponents.Add("Title");
                    _postComponents.Add(messageText);
                    await _client.SendTextMessageAsync(chatId, "Введите третий элемент поста (подпись):");
                }
                else
                {
                    _postComponents.Add("Caption");
                    _postComponents.Add(messageText);
                    await _client.SendTextMessageAsync(chatId, "Введите второй элемент поста (цена или название):");
                }
            }
            else if (_postComponents.Count == 1)
            {
                // Обработка второго элемента
                if (messageText.Equals("цена", StringComparison.OrdinalIgnoreCase))
                {
                    _postComponents.Add("Price");
                    await _client.SendTextMessageAsync(chatId, "Введите третий элемент поста (название или подпись):");
                }
                else if (messageText.Split(' ').Length == 1)
                {
                    _postComponents.Add("Title");
                    _postComponents.Add(messageText);
                    await _client.SendTextMessageAsync(chatId, "Введите третий элемент поста (подпись):");
                }
                else
                {
                    _postComponents.Add("Caption");
                    _postComponents.Add(messageText);
                    await _client.SendTextMessageAsync(chatId, "Введите третий элемент поста (цена или название):");
                }
            }
            else if (_postComponents.Count == 2)
            {
                // Обработка третьего элемента
                if (messageText.Equals("цена", StringComparison.OrdinalIgnoreCase))
                {
                    _postComponents.Add("Price");
                }
                else if (messageText.Split(' ').Length == 1)
                {
                    _postComponents.Add("Title");
                    _postComponents.Add(messageText);
                }
                else
                {
                    _postComponents.Add("Caption");
                    _postComponents.Add(messageText);
                }

                // Сохраняем настройки сборки поста
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var channel = await dbContext.Channels
                        .Include(c => c.PostSettings)
                        .FirstOrDefaultAsync(c => c.Id == _selectedChannels[chatId]);

                    if (channel == null)
                    {
                        await _client.SendTextMessageAsync(chatId, "Канал не найден.");
                        return;
                    }

                    if (channel.PostSettings == null)
                    {
                        channel.PostSettings = new PostSettings();
                    }

                    channel.PostSettings.Order = string.Join(",", _postComponents);
                    await dbContext.SaveChangesAsync();
                }

                await _client.SendTextMessageAsync(chatId, "Настройки сборки поста сохранены!");
                _isConfiguringPost = false;
                _postComponents.Clear();
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
                    new[] { new KeyboardButton("Назад") }
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
                        _postComponents.Clear();
                        await _client.SendTextMessageAsync(chatId, "Введите первый элемент поста (цена, название или подпись):");
                        break;

                    case "/stat":
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