﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TgBotParserAli.DB;
using TgBotParserAli.Models;
using TgBotParserAli.Quartz;

namespace TgBotParserAli.Controllers
{
    public class TextMessageController
    {
        private readonly ITelegramBotClient _client;
        private readonly Scheduler _scheduler;
        private (string ChangeType, Channel Channel) _pendingChange;
        private (string ChangeType, KeywordSetting Channel) _changeWords;
        private bool _isAddingChannel = false;
        private readonly IServiceScopeFactory _scopeFactory;
        private bool _isEditWord = false;
        private Dictionary<long, string> _userEditState = new ();

        public TextMessageController(ITelegramBotClient client, Scheduler scheduler, IServiceScopeFactory serviceScopeFactory)
        {
            _client = client;
            _scheduler = scheduler;
            _scopeFactory = serviceScopeFactory;
        }

        internal async Task CheckUserOrAdmin(Update update)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var admin = await _dbContext.Admin.FirstOrDefaultAsync(a => a.ChatId == update.Message.From.Id);
                var admins = _dbContext.Admin.Select(a => a.ChatId);
                if (admin != null)
                {
                    if (update.Message.Text != "/menu")
                    {
                        await _client.SendTextMessageAsync(update.Message.From.Id, "Привет! Я бот для постинга товаров с AliExpress.");
                        await SendMainMenu(update.Message.From.Id);
                    }
                    else
                    {
                        await SendMainMenu(update.Message.From.Id);
                    }
                }
                else
                {
                    foreach (var adm in admins)
                    {
                        await _client.SendTextMessageAsync(adm, $"Данный пользователь зашел в бота: {update.Message.From.Username} ({update.Message.From.Id})");
                    }
                }
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
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var messageText = update.Message.Text;
                var yorn = await CheckAdmins(update);
                if (yorn == false)
                    return;

                // Обработка команд
                switch (messageText)
                {
                    case "🤖Добавить канал":
                        _isAddingChannel = true;
                        await _client.SendTextMessageAsync(update.Message.From.Id, "Название, ID канала, Количество товаров для парсинга," +
                            " Максимальное количество постов в день, Минимальная цена, Максимальная цена, партнерская ссылка," +
                            " ключевые слова (через ;), частота парсинга для каждого слова Ч:М:С(через ;)," +
                            " частота постинга для каждого слова Ч:М:С(через ;)");
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
                        await ShowChannelSettingsMenu(update.Message.From.Id);
                        break;
                    case "📊Статистика":
                        await ShowChannelStats(update.Message.From.Id);
                        break;
                    case "🧹Очистить таблицу товаров":
                        await ClearProductsTable(update.Message.From.Id);
                        break;
                    case "🗑 Удалить канал":
                        var channel = _pendingChange.Channel;
                        var chatId = update.Message.From.Id;
                        await DeleteChannel(chatId, channel);
                        break;
                    case "▶️ Запустить канал":
                        var channelS = _pendingChange.Channel;
                        var chatIdS = update.Message.From.Id;
                        await StartChannel(chatIdS, channelS);
                        break;
                    case "⏸ Остановить канал":
                        var channelStop = _pendingChange.Channel;
                        var chatIdStop = update.Message.From.Id;
                        await StopChannel(chatIdStop, channelStop);
                        break;
                    case "Сборка поста":
                        await HandlePostSettings(update.Message.From.Id);
                        break;
                    case "Изменить частоту парсинга":
                        await HandleChangeRequest(update.Message.From.Id, update.Message.Text);
                        break;
                    case "Изменить количество товаров":
                        await HandleChangeRequest(update.Message.From.Id, update.Message.Text);
                        break;
                    case "Изменить частоту постинга":
                        await HandleChangeRequest(update.Message.From.Id, update.Message.Text);
                        break;
                    case "Изменить максимальное количество постов":
                        await HandleChangeRequest(update.Message.From.Id, update.Message.Text);
                        break;
                    case "Изменить диапазон цен":
                        await HandleChangeRequest(update.Message.From.Id, update.Message.Text);
                        break;
                    case "Изменить партнерскую ссылку":
                        await HandleChangeRequest(update.Message.From.Id, update.Message.Text);
                        break;
                    case "Включить сокращение ссылок":
                    case "Отключить сокращение ссылок":
                        await ToggleUseShortLinks(update.Message.From.Id, update.Message.Text);
                        break;
                    case "Изменить ключевые слова":
                        if (_pendingChange.Channel != null)
                        {
                            await EditKeywords(update.Message.From.Id, _pendingChange.Channel);
                        }
                        else
                        {
                            await _client.SendTextMessageAsync(update.Message.From.Id, "Канал не выбран.");
                        }
                        break;
                    case "Назад":
                        await SendMainMenu(update.Message.From.Id);
                        break;
                    default:
                        await CheckMessage(update.Message.From.Id, update.Message.Text);
                        break;
                }
            }
        }

        private async Task HandlePostSettings(long chatId)
        {
            var channel = _pendingChange.Channel;

            if (channel == null)
            {
                await _client.SendTextMessageAsync(chatId, "Канал не выбран.");
                return;
            }

            // Запрашиваем первое значение
            await _client.SendTextMessageAsync(chatId, "Введите первое значение (цена, название или подпись):");
            _pendingChange.ChangeType = "PostSettings_First";

            // Сохраняем состояние для следующего шага
            _pendingChange.Channel = channel;
        }

        private async Task StopChannel(long chatId, Channel channel)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                channel.IsActive = false;
                _scheduler.RemoveTimers(channel.Id);
                _dbContext.Channels.Update(channel);
                await _dbContext.SaveChangesAsync(); // Останавливаем таймеры
                await _client.SendTextMessageAsync(chatId, $"Канал {channel.Name} остановлен.");
            }
        }

        private async Task StartChannel(long chatId, Channel channel)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                channel.IsActive = true;
                _scheduler.ScheduleJobsForChannel(channel);
                _dbContext.Channels.Update(channel);
                await _dbContext.SaveChangesAsync();
                await _client.SendTextMessageAsync(chatId, $"Канал {channel.Name} запущен.");
            }
        }

        private async Task DeleteChannel(long chatId, Channel channel)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                _scheduler.RemoveTimers(channel.Id);
                // Удаляем все связанные данные
                var products = _dbContext.Products.Where(p => p.ChannelId == channel.Id).ToList();
                var stats = _dbContext.KeywordStats.Where(ks => ks.ChannelId == channel.Id).ToList();
                var keywordSettings = _dbContext.KeywordSettings.Where(k => k.ChannelId == channel.Id).ToList();

                _dbContext.Products.RemoveRange(products);
                _dbContext.KeywordStats.RemoveRange(stats);
                _dbContext.KeywordSettings.RemoveRange(keywordSettings);
                _dbContext.Channels.Remove(channel);

                await _dbContext.SaveChangesAsync();
                await _client.SendTextMessageAsync(chatId, $"Канал {channel.Name} удален.");
            }
        }

        private async Task ToggleUseShortLinks(long chatId, string? command)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var channel = _pendingChange.Channel;

                if (channel == null)
                {
                    await _client.SendTextMessageAsync(chatId, "Канал не выбран.");
                    return;
                }

                // Определяем новое значение UseShortLinks
                bool newValue = command == "Включить сокращение ссылок";

                // Обновляем значение в базе данных
                channel.UseShortLinks = newValue;
                dbContext.Channels.Update(channel);
                await dbContext.SaveChangesAsync();

                // Уведомляем пользователя
                await _client.SendTextMessageAsync(chatId, $"Сокращение ссылок {(newValue ? "включено" : "отключено")} для канала {channel.Name}.");

                // Возвращаем пользователя в меню настройки канала
                await ShowChannelSettings(chatId, channel);
            }
        }

        private async Task HandleChangeRequest(long chatId, string changeType)
        {
            var channel = _pendingChange.Channel;

            if (channel == null)
            {
                    await _client.SendTextMessageAsync(chatId, "Канал не выбран.");
                    return;
            }

            _pendingChange.ChangeType = changeType;

            await _client.SendTextMessageAsync(chatId, $"Введите новое значение для {changeType.ToLower()}:");
        }

        private async Task CheckMessage(long chatId, string text)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (_pendingChange.ChangeType != null && _pendingChange.Channel != null &&
                    _pendingChange.ChangeType != "PostSettings_First" && _pendingChange.ChangeType != "PostSettings_Second" &&
                    _pendingChange.ChangeType != "PostSettings_Third" && _pendingChange.ChangeType != "PostSettings_Four")
                {
                    await ApplyChange(chatId, text);
                }
                else if(_pendingChange.ChangeType == "PostSettings_First" || _pendingChange.ChangeType == "PostSettings_Second" ||
                    _pendingChange.ChangeType == "PostSettings_Third" || _pendingChange.ChangeType == "PostSettings_Four")
                {
                    await PostChange(chatId, text);
                }
                else if (_isAddingChannel && text.Contains(','))
                {
                    await AddChannel(chatId, text);
                    _isAddingChannel = false;
                }
                else if(_changeWords.ChangeType != null && _changeWords.Channel != null)
                {
                    await ApplyChange(chatId, text);
                }
                else if (_isEditWord)
                {
                    await EditAddWord(text, chatId);
                }
                else
                {
                    var channel = await _dbContext.Channels.FirstOrDefaultAsync(c => c.Name == text);
                    if (channel != null)
                    {
                        await ShowChannelSettings(chatId, channel);
                    }
                    else
                    {
                        await _client.SendTextMessageAsync(chatId, "Канал не найден.");
                    }
                }
            }
        }

        private async Task EditAddWord(string text, long chatId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Если пользователь нажал на кнопку "Добавить ключевое слово"
                if (text == "Добавить ключевое слово")
                {
                    await _client.SendTextMessageAsync(
                        chatId,
                        "Введите новое ключевое слово, частоту парсинга и частоту постинга в формате: 'ключевое слово, частота парсинга, частота постинга' (например, 'магнитола, 6:0:0, 2:0:0')"
                    );
                    _userEditState[chatId] = "add";
                }
                // Если нажата кнопка с ключевым словом (например, "Котики (Парсинг: 5, Постинг: 2)")
                else if (text.Contains("(Парсинг:") && text.Contains("Постинг:"))
                {
                    // Извлекаем текущее слово из текста кнопки
                    var currentKeyword = text.Split('(')[0].Trim();

                    // Запрашиваем новые данные
                    await _client.SendTextMessageAsync(
                        chatId,
                        $"Введите новое ключевое слово, частоту парсинга и частоту постинга в формате: 'ключевое слово, частота парсинга, частота постинга' (например, 'магнитола, 6:0:0, 2:0:0')\""
                    );
                    // Сохраняем выбранное слово для проверки в следующем шаге
                    _userEditState[chatId] = currentKeyword;
                }
                else
                {
                    if (_userEditState[chatId] != "add") 
                    {
                        Console.WriteLine($"Ключевое слово до: {_userEditState[chatId]}");
                        var keywordSetting = await _dbContext.KeywordSettings.FirstOrDefaultAsync(kw => kw.Keyword == _userEditState[chatId]);
                        if (keywordSetting == null) 
                        {
                            Console.WriteLine("KeywordSettings null");
                            return;
                        }
                        _scheduler.StopAndCleanTimers(keywordSetting.Id);
                        var parts = text.Split(',');
                        var keyword = parts[0].Trim();
                        if (parts.Length == 3 && TimeSpan.TryParse(parts[1].Trim(), out var parseFrequency) && TimeSpan.TryParse(parts[2].Trim(), out var postFrequency))
                        {
                            keywordSetting.Channel = _pendingChange.Channel;
                            keywordSetting.Keyword = keyword;
                            keywordSetting.ParseFrequency = parseFrequency;
                            keywordSetting.PostFrequency = postFrequency;

                            // Обновляем или создаем запись в KeywordStat
                            var keywordStat = await _dbContext.KeywordStats
                                .FirstOrDefaultAsync(k => k.Keyword == keywordSetting.Keyword);

                            if (keywordStat != null)
                            {
                                // Если запись существует, обновляем ключевое слово и сбрасываем счетчик
                                keywordStat.Keyword = keyword;
                                keywordStat.Count = 0; // Сбрасываем счетчик
                                keywordStat.LastUpdated = DateTime.UtcNow;
                                _dbContext.KeywordStats.Update(keywordStat);
                                await _dbContext.SaveChangesAsync();
                            }
                            else
                            {
                                // Если записи нет, создаем новую
                                keywordStat = new KeywordStat
                                {
                                    ChannelId = keywordSetting.ChannelId,
                                    Keyword = keyword,
                                    Count = 0, // Начинаем с нулевого счетчика
                                    LastUpdated = DateTime.UtcNow
                                };
                                _dbContext.KeywordStats.Add(keywordStat);
                            }

                            _dbContext.KeywordSettings.Update(keywordSetting);
                            await _dbContext.SaveChangesAsync();
                            await _client.SendTextMessageAsync(chatId, "Настройки ключевого слова успешно обновлены!");
                            _scheduler.UpdateTimersForKeyword(keywordSetting);
                            _isEditWord = false;
                            SendMainMenu(chatId);
                        }
                        else
                        {
                            await _client.SendTextMessageAsync(chatId, "Неверный формат данных. Используйте формат: 'ключевое слово, частота парсинга, частота постинга' (например, 'магнитола, 6:0:0, 2:0:0')");
                        }
                    }
                    else
                    {
                        var parts = text.Split(',');
                        var keyword = parts[0].Trim();
                        if (parts.Length == 3 && TimeSpan.TryParse(parts[1].Trim(), out var parseFrequency) && TimeSpan.TryParse(parts[2].Trim(), out var postFrequency))
                        {
                            var keyWord = new KeywordSetting
                            {
                                ChannelId = _pendingChange.Channel.Id,
                                Channel = _pendingChange.Channel,
                                Keyword = keyword,
                                ParseFrequency = parseFrequency,
                                PostFrequency = postFrequency
                            };
                            _dbContext.KeywordSettings.Add(keyWord);

                            var keywordStat = new KeywordStat
                            {
                                ChannelId = _pendingChange.Channel.Id,
                                Keyword = keyword,
                                Count = 0, // Начинаем с нулевого счетчика
                                LastUpdated = DateTime.UtcNow
                            };
                            _dbContext.KeywordStats.Add(keywordStat);
                            await _dbContext.SaveChangesAsync();
                            await _client.SendTextMessageAsync(chatId, "Настройки ключевого слова успешно обновлены!");
                            _scheduler.UpdateTimersForKeyword(keyWord);
                            _userEditState.Remove(chatId);
                            _isEditWord = false;
                            SendMainMenu(chatId);
                        }
                    }
                }
            }
        }

        private async Task PostChange(long chatId, string newValue)
        {
            var channel = _pendingChange.Channel;
            var changeType = _pendingChange.ChangeType;

            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (changeType.StartsWith("PostSettings_"))
                {
                    var postSettings = await _dbContext.PostSetting
                        .FirstOrDefaultAsync(ps => ps.ChannelId == channel.Id);

                    if (postSettings == null)
                    {
                        postSettings = new PostSettings
                        {
                            ChannelId = channel.Id,
                            PriceTemplate = "", // Инициализируем пустой строкой
                            TitleTemplate = "", // Инициализируем пустой строкой
                            CaptionTemplate = "", // Инициализируем пустой строкой
                            UrlTemplate = "", // Инициализируем пустой строкой
                            Order = "" // Инициализируем пустой строкой
                        };
                        _dbContext.PostSetting.Add(postSettings);
                    }

                    switch (changeType)
                    {
                        case "PostSettings_First":
                            if (newValue.Trim().ToLower() == "цена")
                            {
                                postSettings.PriceTemplate = "1"; // Например, "1" для цены
                                postSettings.Order = "Price,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите второе значение (название, ссылка или подпись):");
                                _pendingChange.ChangeType = "PostSettings_Second";
                            }
                            else if (newValue.Trim().ToLower() == "название")
                            {
                                postSettings.TitleTemplate = "1"; // Например, "1" для названия
                                postSettings.Order = "Title,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите второе значение (цена, ссылка или подпись):");
                                _pendingChange.ChangeType = "PostSettings_Second";
                            }
                            else if(newValue.Trim().ToLower() == "ссылка")
                            {
                                postSettings.UrlTemplate = "1";
                                postSettings.Order = "Url,";
                                await _client.SendTextMessageAsync(chatId, "Введите второе значение (цена, название или подпись):");
                                _pendingChange.ChangeType = "PostSettings_Second";
                            }
                            else
                            {
                                postSettings.CaptionTemplate = newValue; // Подпись
                                postSettings.Order = "Caption,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите второе значение (цена, ссылка или название):");
                                _pendingChange.ChangeType = "PostSettings_Second";
                            }
                            await _dbContext.SaveChangesAsync();
                            break;

                        case "PostSettings_Second":
                            if (newValue.Trim().ToLower() == "цена")
                            {
                                postSettings.PriceTemplate = "2"; // Например, "2" для цены
                                postSettings.Order += "Price,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите третье значение (название, ссылка или подпись):");
                                _pendingChange.ChangeType = "PostSettings_Third";
                            }
                            else if (newValue.Trim().ToLower() == "название")
                            {
                                postSettings.TitleTemplate = "2"; // Например, "2" для названия\
                                postSettings.Order += "Title,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите третье значение (цена, ссылка или подпись):");
                                _pendingChange.ChangeType = "PostSettings_Third";
                            }
                            else if (newValue.Trim().ToLower() == "ссылка")
                            {
                                postSettings.UrlTemplate = "2";
                                postSettings.Order += "Url,";
                                await _client.SendTextMessageAsync(chatId, "Введите третье значение (цена, название или подпись):");
                                _pendingChange.ChangeType = "PostSettings_Third";
                            }
                            else
                            {
                                postSettings.CaptionTemplate = newValue; // Подпись
                                postSettings.Order += "Caption,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите третье значение (цена, ссылка или название):");
                                _pendingChange.ChangeType = "PostSettings_Third";
                            }
                            await _dbContext.SaveChangesAsync();
                            break;

                        case "PostSettings_Third":
                            if (newValue.Trim().ToLower() == "цена")
                            {
                                postSettings.PriceTemplate = "3"; // Например, "3" для цены
                                postSettings.Order += "Price,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите четвертое значение (цена, название или подпись):");
                                _pendingChange.ChangeType = "PostSettings_Four";
                            }
                            else if (newValue.Trim().ToLower() == "название")
                            {
                                postSettings.TitleTemplate = "3"; // Например, "3" для названия
                                postSettings.Order += "Title,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите четвертое значение (цена, название или подпись):");
                                _pendingChange.ChangeType = "PostSettings_Four";
                            }
                            else if (newValue.Trim().ToLower() == "ссылка")
                            {
                                postSettings.UrlTemplate = "3";
                                postSettings.Order += "Url,";
                                await _client.SendTextMessageAsync(chatId, "Введите четвертое значение (цена, название или подпись):");
                                _pendingChange.ChangeType = "PostSettings_Four";
                            }
                            else
                            {
                                postSettings.CaptionTemplate = newValue; // Подпись
                                postSettings.Order += "Caption,"; // Добавляем порядок
                                await _client.SendTextMessageAsync(chatId, "Введите четвертое значение (цена, название или подпись):");
                                _pendingChange.ChangeType = "PostSettings_Four";
                            }
                            await _dbContext.SaveChangesAsync();
                            break;

                        case "PostSettings_Four":
                            if (newValue.Trim().ToLower() == "цена")
                            {
                                postSettings.PriceTemplate = "4"; // Например, "4" для цены
                                postSettings.Order += "Price"; // Добавляем порядок
                            }
                            else if (newValue.Trim().ToLower() == "название")
                            {
                                postSettings.TitleTemplate = "4"; // Например, "4" для названия
                                postSettings.Order += "Title"; // Добавляем порядок
                            }
                            else if (newValue.Trim().ToLower() == "ссылка")
                            {
                                postSettings.UrlTemplate = "4";
                                postSettings.Order += "Url";
                            }
                            else
                            {
                                postSettings.CaptionTemplate = newValue; // Подпись
                                postSettings.Order += "Caption"; // Добавляем порядок
                            }
                            await _dbContext.SaveChangesAsync();

                            await _client.SendTextMessageAsync(chatId, "Настройки сборки поста сохранены.");
                            _pendingChange.ChangeType = null;
                            break;
                    }
                }
            }
        }

        private async Task ApplyChange(long chatId, string newValue)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var channel = _pendingChange.Channel;
                var changeType = _pendingChange.ChangeType;
                var keywords = _changeWords.Channel;
                var type = _changeWords.ChangeType;

                if ((channel == null && changeType == null && keywords == null && type == null) && !newValue.Contains("Удалить") && !newValue.Contains("Запустить") && !newValue.Contains("Остановить"))
                {
                    await _client.SendTextMessageAsync(chatId, "Ошибка: изменение не может быть применено.");
                    return;
                }
                // Останавливаем старые таймеры для этого канала
                _scheduler.RemoveTimers(channel.Id);

                if (keywords != null && type != null)
                {
                    switch (type)
                    {
                        case "edit_keyword": // Обработка изменения слов для парсинга
                            var keywordSetting = _changeWords.Channel as KeywordSetting;

                            var parts = newValue.Split(',');
                            var keyword = parts[0].Trim();
                            if (parts.Length == 3 && TimeSpan.TryParse(parts[1].Trim(), out var parseFrequency) && TimeSpan.TryParse(parts[2].Trim(), out var postFrequency))
                            {
                                keywordSetting.Keyword = keyword;
                                keywordSetting.ParseFrequency = parseFrequency;
                                keywordSetting.PostFrequency = postFrequency;

                                // Обновляем или создаем запись в KeywordStat
                                var keywordStat = await _dbContext.KeywordStats
                                    .FirstOrDefaultAsync(k => k.ChannelId == channel.Id && k.Keyword == keywordSetting.Keyword);

                                if (keywordStat != null)
                                {
                                    // Если запись существует, обновляем ключевое слово и сбрасываем счетчик
                                    keywordStat.Keyword = keyword;
                                    keywordStat.Count = 0; // Сбрасываем счетчик
                                    keywordStat.LastUpdated = DateTime.UtcNow;
                                    _dbContext.KeywordStats.Update(keywordStat);
                                }
                                else
                                {
                                    // Если записи нет, создаем новую
                                    keywordStat = new KeywordStat
                                    {
                                        ChannelId = channel.Id,
                                        Keyword = keyword,
                                        Count = 0, // Начинаем с нулевого счетчика
                                        LastUpdated = DateTime.UtcNow
                                    };
                                    _dbContext.KeywordStats.Add(keywordStat);
                                }

                                _dbContext.KeywordSettings.Update(keywordSetting);
                                await _dbContext.SaveChangesAsync();
                                await _client.SendTextMessageAsync(chatId, "Настройки ключевого слова успешно обновлены!");
                                _scheduler.UpdateTimersForKeyword(keywordSetting);
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
                                    var channelA = _pendingChange.Channel as Channel;

                                    channelA.KeywordSettings.Add(new KeywordSetting
                                    {
                                        Keyword = keywordA,
                                        ParseFrequency = parseFrequencyA,
                                        PostFrequency = postFrequencyA
                                    });

                                    await _dbContext.SaveChangesAsync();
                                    await _client.SendTextMessageAsync(chatId, "Ключевое слово успешно добавлено!");
                                    _scheduler.UpdateTimersForKeyword(channelA.KeywordSettings.FirstOrDefault(kw => kw.Keyword == keywordA));
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
                }
                else
                {
                    // Применяем изменения
                    switch (changeType)
                    {
                        case "Изменить количество товаров":
                            if (int.TryParse(newValue, out var parseCount))
                            {
                                channel.ParseCount = parseCount;
                            }
                            else
                            {
                                await _client.SendTextMessageAsync(chatId, "Неверный формат числа.");
                                return;
                            }
                            break;
                        case "Изменить максимальное количество постов":
                            if (int.TryParse(newValue, out var maxPostsPerDay))
                            {
                                channel.MaxPostsPerDay = maxPostsPerDay;
                                // Проверяем условия для возобновления парсинга
                                _scheduler.RestartParsingIfConditionsMet(channel);
                                await _client.SendTextMessageAsync(chatId, $"Максимальное количество постов в день изменено на {maxPostsPerDay}.");
                            }
                            else
                            {
                                await _client.SendTextMessageAsync(chatId, "Неверный формат числа.");
                                return;
                            }
                            break;
                        case "Изменить диапазон цен":
                            var prices = newValue.Split('-');
                            if (prices.Length == 2 && decimal.TryParse(prices[0], out var minPrice) && decimal.TryParse(prices[1], out var maxPrice))
                            {
                                channel.MinPrice = minPrice;
                                channel.MaxPrice = maxPrice;

                                channel.ParsedCount = 0; // Сбрасываем ParsedCount
                            }
                            else
                            {
                                await _client.SendTextMessageAsync(chatId, "Неверный формат диапазона цен. Используйте формат: минимальная цена - максимальная цена");
                                return;
                            }
                            break;
                        case "Изменить партнерскую ссылку":
                            channel.ReferralLink = FormatReferralLink(newValue);
                            break;
                        default:
                            await _client.SendTextMessageAsync(chatId, "Неизвестный тип изменения.");
                            return;
                    }
                }

                _dbContext.Channels.Update(channel);

                // Сохраняем изменения в базе данных
                await _dbContext.SaveChangesAsync();

                // Запускаем новые таймеры с обновленными настройками (если канал активен)
                if (channel.IsActive)
                {
                    _scheduler.ScheduleJobsForChannel(channel);
                }

                await _client.SendTextMessageAsync(chatId, "Изменение успешно применено!");

                // Очищаем переменные
                _pendingChange = (null, null);
                _changeWords = (null, null);

                // Возвращаем пользователя в меню настройки канала
                await ShowChannelSettings(chatId, channel);
            }
        }

        private async Task<bool> CheckAdmins(Update update)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var admin = await _dbContext.Admin.FirstOrDefaultAsync(a => a.ChatId == update.Message.From.Id);
                if (admin != null)
                    return true;
                else
                    return false;
            }
        }

        private async Task ShowChannelSettingsMenu(long chatId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var channels = await _dbContext.Channels.ToListAsync();

                if (channels.Any())
                {
                    var buttons = channels.Select(channel => new KeyboardButton(channel.Name)).ToArray();
                    var menu = new ReplyKeyboardMarkup(new[] { buttons })
                    {
                        ResizeKeyboard = true // Опционально: делает кнопки меньшего размера
                    };

                    await _client.SendTextMessageAsync(chatId, "Выберите канал для настройки:", replyMarkup: menu);
                }
                else
                {
                    await _client.SendTextMessageAsync(chatId, "Нет добавленных каналов.");
                }
            }
        }

        private async Task ShowChannelSettings(long chatId, Channel channel)
        {
            _pendingChange.Channel = channel;

            var menu = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Изменить количество товаров") },
                new[] { new KeyboardButton("Изменить максимальное количество постов") },
                new[] { new KeyboardButton("Изменить диапазон цен") },
                new[] { new KeyboardButton("Изменить партнерскую ссылку") },
                new[] { new KeyboardButton("Изменить ключевые слова") },
                new[] { new KeyboardButton("Сборка поста") },
                new[] { new KeyboardButton(channel.UseShortLinks ? "Отключить сокращение ссылок" : "Включить сокращение ссылок") },
                new[] { new KeyboardButton(channel.IsActive ? "⏸ Остановить канал" : "▶️ Запустить канал") },
                new[] { new KeyboardButton("🗑 Удалить канал") },
                new[] { new KeyboardButton("Назад") }
            })
            {
                ResizeKeyboard = true
            };

            await _client.SendTextMessageAsync(chatId, $"Настройки канала {channel.Name}:\n" +
                $"Количество товаров: {channel.ParseCount}\n" + 
                $"Максимальное количество постов: {channel.MaxPostsPerDay}\n" +
                $"Диапазон цен: {channel.MinPrice} - {channel.MaxPrice}\n" +
                $"Сокращение ссылок: {(channel.UseShortLinks ? "Включено" : "Отключено")}\n" +
                $"Статус: {(channel.IsActive ? "Активен" : "Остановлен")}", replyMarkup: menu);
        }

        private async Task AddChannel(long chatId, string text)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (text.Contains(','))
                {
                    var parts = text.Split(',');
                    if (parts.Length == 10)
                    {
                        try
                        {
                            var channel = new Channel
                            {
                                Name = parts[0].Trim(),
                                ChatId = parts[1].Trim(),
                                ParseCount = int.Parse(parts[2].Trim()),
                                MaxPostsPerDay = int.Parse(parts[3].Trim()),
                                MinPrice = decimal.Parse(parts[4].Trim()),
                                MaxPrice = decimal.Parse(parts[5].Trim()),
                                ReferralLink = FormatReferralLink(parts[6].Trim()),
                                IsActive = true,
                                UseShortLinks = false
                            };

                            // Обрабатываем ключевые слова и их настройки
                            var keywords = parts[7].Trim().Split(';').Select(k => k.Trim()).ToList();
                            var parseFrequencies = parts[8].Trim().Split(';').Select(p => TimeSpan.Parse(p.Trim())).ToList();
                            var postFrequencies = parts[9].Trim().Split(';').Select(p => TimeSpan.Parse(p.Trim())).ToList();
                            channel.Keywords = keywords.ToString();
                            if (keywords.Count != parseFrequencies.Count || keywords.Count != postFrequencies.Count)
                            {
                                await _client.SendTextMessageAsync(chatId, "Количество ключевых слов и настроек не совпадает.");
                                return;
                            }

                            for (int i = 0; i < keywords.Count; i++)
                            {
                                channel.KeywordSettings.Add(new KeywordSetting
                                {
                                    Keyword = keywords[i],
                                    ParseFrequency = parseFrequencies[i],
                                    PostFrequency = postFrequencies[i]
                                });
                            }

                            _dbContext.Channels.Add(channel);
                            await _dbContext.SaveChangesAsync();

                            // Динамически добавляем задачи для нового канала
                            _scheduler.ScheduleJobsForChannel(channel);

                            await _client.SendTextMessageAsync(chatId, "Канал успешно добавлен!");
                            await SendMainMenu(chatId);
                        }
                        catch (FormatException)
                        {
                            await _client.SendTextMessageAsync(chatId, "Неверный формат данных. Убедитесь, что введены корректные значения.");
                        }
                    }
                    else
                    {
                        await _client.SendTextMessageAsync(chatId, "Неверный формат данных. Попробуйте снова.");
                    }
                }
            }
        }

        private string FormatReferralLink(string referralLink)
        {
            if (referralLink.StartsWith("https://aliclick.shop/"))
            {
                return $"{referralLink}?to=http%3A%2F%2Faliexpress.ru%2Fitem%2F";
            }
            else if (referralLink.StartsWith("https://shopnow.pub/"))
            {
                return $"{referralLink}&to=https%3A%2F%2Faliexpress.ru%2Fitem%2F";
            }
            else
            {
                throw new ArgumentException("Неподдерживаемый формат реферальной ссылки.");
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

            // Очищаем переменные
            _pendingChange = (null, null);
            _changeWords = (null, null);
        }

        internal async Task BotClient_OnCallbackQuery(CallbackQuery? callbackQuery)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var chatId = callbackQuery.Message.Chat.Id;
                var data = callbackQuery.Data;

                // Находим канал по имени
                var channel = await _dbContext.Channels.FirstOrDefaultAsync(c => c.Name == data);

                if (channel != null)
                {
                    // Фиксируем выбранный канал
                    _pendingChange.Channel = channel;

                    // Показываем меню настроек для выбранного канала
                    await ShowChannelSettings(chatId, channel);
                }
                else if (data.StartsWith("edit_keyword_"))
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
                    _changeWords = ("add_keyword", _changeWords.Channel);
                }
                else
                {
                    await _client.SendTextMessageAsync(chatId, "Канал не найден.");
                }
            }
        }

        private async Task ShowChannelStats(long chatId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var channels = await dbContext.Channels.ToListAsync();

                var stats = new StringBuilder();
                foreach (var channel in channels)
                {
                    var postedCount = channel.PostedToday;
                    var notPostedCount = await dbContext.Products
                        .CountAsync(p => p.ChannelId == channel.Id && !p.IsPosted);

                    stats.AppendLine($"Канал: {channel.Name}");
                    stats.AppendLine($"- Опубликовано сегодня: {postedCount}/{channel.MaxPostsPerDay}");
                    stats.AppendLine($"- Не опубликовано: {channel.FailedPosts}");
                    stats.AppendLine();
                }

                await _client.SendTextMessageAsync(chatId, stats.ToString());
            }
        }

        private async Task ClearProductsTable(long chatId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Очищаем таблицу Products
                var products = await dbContext.Products.ToListAsync();
                dbContext.Products.RemoveRange(products);
                await dbContext.SaveChangesAsync();

                await _client.SendTextMessageAsync(chatId, "Таблица Products успешно очищена.");
            }
        }

        private async Task EditKeywords(long chatId, Channel channel)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Получаем ключевые слова для канала
                var keywordSettings = await dbContext.KeywordSettings
                    .Where(k => k.ChannelId == channel.Id)
                    .ToListAsync();

                // Создаем кнопки для каждого ключевого слова
                var buttons = keywordSettings
                    .Select(k => new KeyboardButton(
                        $"{k.Keyword} (Парсинг: {k.ParseFrequency}, Постинг: {k.PostFrequency})"
                    ))
                    .ToList();

                // Добавляем кнопку для добавления нового ключевого слова
                buttons.Add(new KeyboardButton("Добавить ключевое слово"));

                // Создаем Reply-клавиатуру (кнопки внизу экрана)
                var menu = new ReplyKeyboardMarkup(buttons.Chunk(1).ToArray()) // Chunk(1) = одна кнопка в строке
                {
                    ResizeKeyboard = true, // Автоматически подгоняет размер кнопок
                    OneTimeKeyboard = true // Скрывает клавиатуру после нажатия (опционально)
                };

                // Отправляем сообщение с клавиатурой
                await _client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Выберите ключевое слово для редактирования:",
                    replyMarkup: menu
                );

                _isEditWord = true;
            }
        }
    }
}