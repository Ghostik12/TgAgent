using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TgBotParserAli.DB;
using TgBotParserAli.Models;
using TgBotParserAli.Quartz;

namespace TgBotParserAli.Controllers
{
    public class TextMessageController
    {
        private readonly ITelegramBotClient _client;
        private readonly AppDbContext _dbContext;
        private readonly Scheduler _scheduler;
        private (string ChangeType, Channel Channel) _pendingChange;
        private bool _isAddingChannel = false;

        public TextMessageController(ITelegramBotClient client, AppDbContext dbContext, Scheduler scheduler)
        {
            _client = client;
            _dbContext = dbContext;
            _scheduler = scheduler;
        }

        internal async Task CheckUserOrAdmin(Update update)
        {
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

        internal async Task Handle(Update update, CancellationToken cancellationToken)
        {
            var messageText = update.Message.Text;
            var yorn = CheckAdmins(update);
            if (yorn == false)
                return;

            // Обработка команд
            switch (messageText)
            {
                case "🤖Добавить канал":
                    _isAddingChannel = true;
                    await _client.SendTextMessageAsync(update.Message.From.Id, "Введите данные канала в формате:\n" +
                        "Название, ID канала, Частота парсинга (например, 6:0:0), Количество товаров для парсинга," +
                        " Частота постинга (например, 2:0:0), Максимальное количество постов в день," +
                        " Минимальная цена, Максимальная цена, слова для парсинга, партнерская ссылка");
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
                case "Изменить слова для парсинга":
                    await HandleChangeRequest(update.Message.From.Id, update.Message.Text);
                    break;
                case "Назад":
                    await SendMainMenu(update.Message.From.Id);
                    break;
                default:
                    await CheckMessage(update.Message.From.Id, update.Message.Text);
                    break;
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
            if (_pendingChange.ChangeType != null && _pendingChange.Channel != null)
            {
                await ApplyChange(chatId, text);
            }
            else if (_isAddingChannel && text.Contains(','))
            {
                await AddChannel(chatId, text);
                _isAddingChannel = false;
            }
            else if(_pendingChange.Channel != null && text.Contains("Остановить"))
            {
                await ApplyChange(chatId, text);
            }
            else if (_pendingChange.Channel != null && text.Contains("Запустить"))
            {
                await ApplyChange(chatId, text);
            }
            else if (_pendingChange.Channel != null && text.Contains("Удалить"))
            {
                await ApplyChange(chatId, text);
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

        private async Task ApplyChange(long chatId, string newValue)
        {
            var channel = _pendingChange.Channel;
            var changeType = _pendingChange.ChangeType;

            if ((channel == null || changeType == null) && !newValue.Contains("Удалить") && !newValue.Contains("Запустить") && !newValue.Contains("Остановить"))
            {
                await _client.SendTextMessageAsync(chatId, "Ошибка: изменение не может быть применено.");
                return;
            }
            // Останавливаем старые таймеры для этого канала
            _scheduler.RemoveTimers(channel.Id);
            if (newValue.Contains("Удалить") || newValue.Contains("Запустить") || newValue.Contains("Остановить"))
            {
                switch (newValue) 
                {
                    case "⏸ Остановить канал":
                        channel.IsActive = false;
                        _scheduler.RemoveTimers(channel.Id); // Останавливаем таймеры
                        break;
                    case "▶️ Запустить канал":
                        channel.IsActive = true;
                        _scheduler.ScheduleJobsForChannel(channel); // Запускаем таймеры
                        break;
                    case "🗑 Удалить канал":
                        _dbContext.Channels.Remove(channel);
                        _scheduler.RemoveTimers(channel.Id); // Удаляем таймеры
                        break;
                }
            }
            else {
                // Применяем изменения
                switch (changeType)
                {
                    case "Изменить частоту парсинга":
                        if (TimeSpan.TryParse(newValue, out var parseFrequency))
                        {
                            channel.ParseFrequency = parseFrequency;
                        }
                        else
                        {
                            await _client.SendTextMessageAsync(chatId, "Неверный формат времени. Используйте формат 'часы:минуты:секунды'.");
                            return;
                        }
                        break;
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
                    case "Изменить частоту постинга":
                        if (TimeSpan.TryParse(newValue, out var postFrequency))
                        {
                            channel.PostFrequency = postFrequency;
                        }
                        else
                        {
                            await _client.SendTextMessageAsync(chatId, "Неверный формат времени. Используйте формат 'часы:минуты:секунды'.");
                            return;
                        }
                        break;
                    case "Изменить максимальное количество постов":
                        if (int.TryParse(newValue, out var maxPostsPerDay))
                        {
                            channel.MaxPostsPerDay = maxPostsPerDay;
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
                    case "Изменить слова для парсинга": // Обработка изменения слов для парсинга
                        channel.Keywords = newValue;
                        channel.ParsedCount = 0; // Сбрасываем ParsedCount
                        break;
                    default:
                        await _client.SendTextMessageAsync(chatId, "Неизвестный тип изменения.");
                        return;
                }
            }

            // Сохраняем изменения в базе данных
            await _dbContext.SaveChangesAsync();

            // Запускаем новые таймеры с обновленными настройками (если канал активен)
            if (channel.IsActive)
            {
                _scheduler.ScheduleJobsForChannel(channel);
            }

            await _client.SendTextMessageAsync(chatId, "Изменение успешно применено!");

            // Очищаем переменную
            _pendingChange = (null, null);

            // Возвращаем пользователя в меню настройки канала
            await ShowChannelSettings(chatId, channel);
        }

        private bool CheckAdmins(Update update)
        {
            var admin = _dbContext.Admin.FirstOrDefault(a => a.ChatId == update.Message.From.Id);
            return admin != null;
        }

        private async Task ShowChannelSettingsMenu(long chatId)
        {
            var channels = await _dbContext.Channels.ToListAsync();

            if (channels.Any())
            {
                var buttons = channels.Select(channel => new[] { InlineKeyboardButton.WithCallbackData(channel.Name) }).ToArray(); ;
                var menu = new InlineKeyboardMarkup(buttons);

                await _client.SendTextMessageAsync(chatId, "Выберите канал для настройки:", replyMarkup: menu);
            }
            else
            {
                await _client.SendTextMessageAsync(chatId, "Нет добавленных каналов.");
            }
        }

        private async Task ShowChannelSettings(long chatId, Channel channel)
        {
            _pendingChange.Channel = channel;

            var menu = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Изменить частоту парсинга") },
                new[] { new KeyboardButton("Изменить количество товаров") },
                new[] { new KeyboardButton("Изменить частоту постинга") },
                new[] { new KeyboardButton("Изменить максимальное количество постов") },
                new[] { new KeyboardButton("Изменить диапазон цен") },
                new[] { new KeyboardButton("Изменить партнерскую ссылку") },
                new[] { new KeyboardButton("Изменить слова для парсинга") },
                new[] { new KeyboardButton(channel.IsActive ? "⏸ Остановить канал" : "▶️ Запустить канал") },
                new[] { new KeyboardButton("🗑 Удалить канал") },
                new[] { new KeyboardButton("Назад") }
            })
            {
                ResizeKeyboard = true
            };

            await _client.SendTextMessageAsync(chatId, $"Настройки канала {channel.Name}:\n" +
                $"Частота парсинга: {channel.ParseFrequency}\n" +
                $"Количество товаров: {channel.ParseCount}\n" +
                $"Частота постинга: {channel.PostFrequency}\n" +
                $"Максимальное количество постов: {channel.MaxPostsPerDay}\n" +
                $"Диапазон цен: {channel.MinPrice} - {channel.MaxPrice}" +
                $"Слова для парсинга: {channel.Keywords}\n" +
                $"Статус: {(channel.IsActive ? "Активен" : "Остановлен")}", replyMarkup: menu);
        }

        private async Task AddChannel(long chatId, string text)
        {
            if (text.Contains(','))
            {
                var parts = text.Split(',');
                if (parts.Length == 10)
                {
                    try
                    {
                        var str = FormatReferralLink(parts[9].Trim());
                        var channel = new Channel
                        {
                            Name = parts[0].Trim(),
                            ChatId = parts[1].Trim(),
                            ParseFrequency = TimeSpan.Parse(parts[2].Trim()),
                            ParseCount = int.Parse(parts[3].Trim()),
                            PostFrequency = TimeSpan.Parse(parts[4].Trim()),
                            MaxPostsPerDay = int.Parse(parts[5].Trim()),
                            MinPrice = decimal.Parse(parts[6].Trim()),
                            MaxPrice = decimal.Parse(parts[7].Trim()),
                            Keywords = parts[8].Trim(),
                            ReferralLink = str // Реферальная ссылка
                        };

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
            new[] { new KeyboardButton("🤖Добавить канал"), new KeyboardButton("⚙️Настройка каналов") }
        })
            {
                ResizeKeyboard = true
            };

            await _client.SendTextMessageAsync(
                chatId: chatId,
                text: "🗄Главное меню:",
                replyMarkup: replyKeyboard);
        }

        internal async Task BotClient_OnCallbackQuery(CallbackQuery? callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var channelName = callbackQuery.Data; // Данные, которые мы передали в кнопку

            // Находим канал по имени
            var channel = await _dbContext.Channels.FirstOrDefaultAsync(c => c.Name == channelName);

            if (channel != null)
            {
                // Фиксируем выбранный канал
                _pendingChange.Channel = channel;

                // Показываем меню настроек для выбранного канала
                await ShowChannelSettings(chatId, channel);
            }
            else
            {
                await _client.SendTextMessageAsync(chatId, "Канал не найден.");
            }
        }
    }
}