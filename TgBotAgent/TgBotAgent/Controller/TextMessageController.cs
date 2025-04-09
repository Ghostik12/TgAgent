using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TgBotAgent.DB;
using TgBotAgent.Models;
using Update = Telegram.Bot.Types.Update;

namespace TgBotAgent.Controller
{
    public class TextMessageController
    {
        private ITelegramBotClient _clientBot;
        private static readonly Dictionary<long, int> _currentPage = new Dictionary<long, int>();
        private readonly ApplicationDbContext db;

        public TextMessageController(ITelegramBotClient clientBot, ApplicationDbContext _db)
        {
            _clientBot = clientBot;
            db = _db;
        }

        internal async Task CheckUserOrAdmin(Update update)
        {
            try
            {
                var chatId = update.Message.Chat.Id;
                var isUser = await db.Users.Where(u => u.ChatId == chatId).FirstOrDefaultAsync();
                var userLink = await db.UserLinks.Where(ul => ul.UserId1 == chatId || ul.UserId2 == chatId || ul.UserName1 == update.Message.Chat.Username || ul.UserName2 == update.Message.Chat.Username)
                        .FirstOrDefaultAsync();
                if (await IsAdmin(chatId))
                {
                    if (isUser == null)
                    {
                        var userAdd = new Users()
                        {
                            ChatId = chatId,
                            Username = update.Message.From.Username
                        };
                        db.Users.Add(userAdd);
                        await db.SaveChangesAsync();
                    }
                    await AdminMenu(chatId);
                }
                if (userLink != null)
                {
                    if (userLink.UserName1 == update.Message.Chat.Username)
                    {
                        if (userLink.UserId1 != chatId)
                            userLink.UserId1 = chatId;
                    }
                    else if (userLink.UserName2 == update.Message.Chat.Username)
                    {
                        if (userLink.UserId2 != chatId)
                            userLink.UserId2 = chatId;
                    }
                    else if (userLink.UserId1 == chatId)
                    {
                        if (userLink.UserName1 != update.Message.Chat.Username && userLink.UserName1 != "unknown")
                            userLink.UserName1 = update.Message.Chat.Username ?? "unknown";
                    }
                    else if (userLink.UserId2 == chatId)
                    {
                        if (userLink.UserName2 != update.Message.Chat.Username && userLink.UserName2 != "unknown")
                            userLink.UserName2 = update.Message.Chat.Username ?? "unknown";
                    }

                    db.UserLinks.Update(userLink);
                    await db.SaveChangesAsync();

                    if (isUser == null)
                    {
                        var user = new Users()
                        {
                            ChatId = chatId,
                            Username = update.Message.From.Username
                        };
                        db.Users.Add(user);
                        await db.SaveChangesAsync();
                    }

                    await WelcomeMessage(chatId);
                }
                else
                {
                    await _clientBot.SendTextMessageAsync(chatId, "Вы не связаны с другим пользователем.");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task WelcomeMessage(long chatId)
        {
            var message = await db.Settings.FirstOrDefaultAsync(s => s.Key == "WelcomeMessage");
            await _clientBot.SendTextMessageAsync(chatId, message.Value);
        }

        private async Task AdminMenu(long chatId)
        {
            await _clientBot.SendTextMessageAsync(chatId, "Команды для администратора:\n" +
                "/admin linku <id1> <id2>\n/admin unlinku <id>\n/admin addblacklist <слово>\n" +
                "/admin export 2025-01-01\n" +
                "/admin viewlinks\n/admin export24\n/admin linku <username1> <username2>\n/admin unlinku <username>\n/admin viewlinks\n/admin viewpairs\n" +
                "Команды писать без знаков <>\nПри добавлении через username знак @ не использовать");
        }

        internal async Task HandleCallBack(CallbackQuery? callbackQuery, ITelegramBotClient telegramClient, Update update)
        {
            throw new NotImplementedException();// Для срабатывания кнопки
        }

        internal async Task HandleAdminCommand(long chatId, string text)
        {
            var parts = text.Split(' ');
            var command = parts[1].ToLower();

            // Проверка, является ли пользователь администратором
            if (!await IsAdmin(chatId))
            {
                await _clientBot.SendTextMessageAsync(chatId, "У вас нет прав администратора.");
                return;
            }

            switch (command)
            {
                case "clearday":
                    if (parts.Length < 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin setname <новое имя>");
                        return;
                    }
                    var newDay = string.Join(" ", parts.Skip(2));
                    await SettingClearDay(chatId, newDay);
                    break;
                case "setname":
                    if (parts.Length < 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin setname <новое имя>");
                        return;
                    }
                    var newName = string.Join(" ", parts.Skip(2));
                    await SetBotName(chatId, newName);
                    break;
                case "addadmin1":
                    if (parts.Length < 3 || parts.Length > 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin addadmin <id>");
                        return;
                    }
                    await AddAdmin(chatId, long.Parse(parts[2]));
                    break;
                case "removeadmin1":
                    if (parts.Length < 3 || parts.Length > 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin removeadmin <id>");
                        return;
                    }
                    await RemoveAdmin(chatId, long.Parse(parts[2]));
                    break;
                case "adminlist":
                    await AdminList(chatId);
                    break;
                case "setwelcome":
                    await SetWelcomeMessage(chatId, string.Join(" ", parts.Skip(2)));
                    break;
                case "link":
                    if (parts.Length < 4 || parts.Length > 4)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin link <id1> <id2>");
                        return;
                    }
                    await LinkUsers(chatId, long.Parse(parts[2]), long.Parse(parts[3]));
                    break;
                case "unlink":
                    if (parts.Length < 3 || parts.Length > 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin unlink <id>");
                        return;
                    }
                    await UnlinkUser(chatId, long.Parse(parts[2]));
                    break;
                case "linku":
                    if (parts.Length < 4 || parts.Length > 4)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin linku <username1> <username2>");
                        return;
                    }
                    await LinkUsersUsername(chatId, parts[2], parts[3]);
                    break;
                case "unlinku":
                    if (parts.Length < 3 || parts.Length > 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin unlinku <username>");
                        return;
                    }
                    await UnlinkUserUsername(chatId, parts[2]);
                    break;
                case "addblacklist":
                    await AddBlacklistWord(chatId, string.Join(" ", parts.Skip(2)));
                    break;
                case "removeblacklist":
                    await RemoveBlacklistWord(chatId, string.Join(" ", parts.Skip(2)));
                    break;
                case "blacklist":
                    await GetBlackList(chatId);
                    break;
                case "viewlinks":
                    await ViewMessages(chatId, 0);
                    break;
                case "export":
                    if (parts.Length < 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin export <дата>");
                        return;
                    }
                    await ExportMessages(chatId, parts[2]);
                    break;
                case "export24":
                    await ExportMessagesForDay(chatId);
                    break;
                case "viewpairs":
                    await ShowUsersPairs(chatId);
                    break;
                default:
                    await _clientBot.SendTextMessageAsync(chatId, "Неизвестная команда.");
                    break;
            }
        }

        private async Task GetBlackList(long chatId)
        {
            var blackList = await db.BlacklistWords.Select(bl => new {bl.Word}).ToListAsync();
            if (!blackList.Any())
            {
                await _clientBot.SendTextMessageAsync(chatId, "Слов в черном списке нет.");
                return;
            }

            var blackWords = new List<string>();
            foreach (var word in blackList)
            {
                blackWords.Add($"{word.Word}");
            }

            // Отправляем список администратору
            var messageText = "Список слов:\n" + string.Join("\n", blackWords);
            await _clientBot.SendTextMessageAsync(chatId, messageText);
        }

        private async Task AdminList(long chatId)
        {
            // Получаем список администраторов из базы данных
            var admins = await db.Admins
                .Select(a => new { a.ChatId })
                .ToListAsync();

            if (!admins.Any())
            {
                await _clientBot.SendTextMessageAsync(chatId, "Администраторы не найдены.");
                return;
            }

            // Форматируем список администраторов
            var adminList = new List<string>();
            foreach (var admin in admins)
            {
                // Получаем username администратора (если есть)
                var user = await db.Users.FirstOrDefaultAsync(u => u.ChatId == admin.ChatId);
                var username = user?.Username ?? "Unknown";

                adminList.Add($"ID: {admin.ChatId}, Username: @{username}");
            }

            // Отправляем список администратору
            var messageText = "Список администраторов:\n" + string.Join("\n", adminList);
            await _clientBot.SendTextMessageAsync(chatId, messageText);
        }

        private async Task ShowUsersPairs(long chatId)
        {
            // Получаем все пары из базы данных
            var userPairs = await db.UserLinks
                .Select(ul => new { User1 = ul.UserId1, User2 = ul.UserId2, User3 = ul.UserName1, User4 = ul.UserName2})
                .ToListAsync();

            if (!userPairs.Any())
            {
                await _clientBot.SendTextMessageAsync(chatId, "Нет связанных пар пользователей.");
                return;
            }

            // Создаем Inline-кнопки для каждой пары
            var inlineButtons = userPairs
                .Select(p => InlineKeyboardButton.WithCallbackData($"{p.User3} <-> {p.User4}", $"export_pair_{p.User1}_{p.User2}"))
                .ToList();

            // Группируем кнопки по 2 в строке
            var inlineKeyboard = new InlineKeyboardMarkup(inlineButtons
                .Select((button, index) => new { button, index })
                .GroupBy(x => x.index / 2)
                .Select(g => g.Select(x => x.button).ToArray())
                .ToArray());

            // Отправляем сообщение с кнопками
            await _clientBot.SendTextMessageAsync(chatId, "Выберите пару для экспорта:", replyMarkup: inlineKeyboard);
        }

        private async Task ExportMessagesForDay(long chatId)
        {
            DateTime startDate;
            // Экспорт за последние 24 часа
            startDate = DateTime.UtcNow.AddHours(-24);

            // Получаем сообщения за указанный период
            var messages = await db.Messages
                .Where(m => m.Timestamp >= startDate)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            if (!messages.Any())
            {
                await _clientBot.SendTextMessageAsync(chatId, "Нет сообщений за указанный период.");
                return;
            }

            // Группируем сообщения по дням
            var groupedMessages = messages
                .GroupBy(m => m.Timestamp.Date)
                .OrderBy(g => g.Key)
                .ToList();

            // Создаем временный файл
            var filePath = Path.GetTempFileName();

            // Записываем данные в файл
            using (var writer = new StreamWriter(filePath))
            {
                foreach (var group in groupedMessages)
                {
                    // Заголовок для дня
                    await writer.WriteLineAsync($"============= {group.Key:yyyy-MM-dd} =============");

                    // Сообщения за день
                    foreach (var message in group.OrderBy(m => m.Timestamp))
                    {
                        await writer.WriteLineAsync($"{message.Timestamp:HH:mm:ss}: {message.FromUsername} ({message.FromUserId}) -> {message.ToUsername} ({message.ToUserId}): {message.Text}");
                    }

                    // Пустая строка между днями
                    await writer.WriteLineAsync();
                }
            }

            // Отправляем файл
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var inputFile = InputFile.FromStream(fileStream, $"messages_{startDate:yyyy-MM-dd}.txt");

            await _clientBot.SendDocumentAsync(chatId, inputFile);

            // Удаляем временный файл
            File.Delete(filePath);
        }

        private async Task UnlinkUserUsername(long chatId, string userName)
        {
            var userLink = await db.UserLinks
            .FirstOrDefaultAsync(ul => ul.UserName1 == userName || ul.UserName2 == userName);

            if (userLink == null)
            {
                await _clientBot.SendTextMessageAsync(chatId, "Пользователь не связан.");
                return;
            }

            db.UserLinks.Remove(userLink);
            await db.SaveChangesAsync();
            _currentPage.Remove(chatId);

            await _clientBot.SendTextMessageAsync(chatId, $"Пользователь {userLink.UserName1} и {userLink.UserName2} отвязан.");
        }

        private async Task LinkUsersUsername(long chatId, string userName1, string userName2)
        {
            try
            {
                var existingLink = await db.UserLinks
                .FirstOrDefaultAsync(ul =>
                    (ul.UserName1 == userName1 && ul.UserName2 == userName2) ||
                    (ul.UserName1 == userName2 && ul.UserName2 == userName1));

                if (existingLink != null)
                {
                    await _clientBot.SendTextMessageAsync(chatId, "Пользователи уже связаны.");
                    return;
                }

                db.UserLinks.Add(new UserLink { UserName1 = userName1, UserName2 = userName2, UserId1 = 0, UserId2 = 0 });
                await db.SaveChangesAsync();
                _currentPage.Remove(chatId);

                await _clientBot.SendTextMessageAsync(chatId, $"Пользователи {userName1} и {userName2} связаны.");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Неизвестная ошибка: {ex.Message}");
            }
        }

        private async Task SettingClearDay(long chatId, string newDay)
        {
            var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "ClearDay");
            if (setting == null)
            {
                db.Settings.Add(new Setting { Key = "ClearDay", Value = newDay });
            }
            else
            {
                setting.Value = newDay;
            }

            await db.SaveChangesAsync();
            await _clientBot.SendTextMessageAsync(chatId, "Часто чистки записей обновлена.");
        }

        private async Task SetBotName(long chatId, string newName)
        {
            try
            {
                await _clientBot.SetMyNameAsync(newName);
                await _clientBot.SendTextMessageAsync(chatId, $"Имя бота изменено на: {newName}");
            }
            catch (Exception ex)
            {
                await _clientBot.SendTextMessageAsync(chatId, $"Ошибка при изменении имени бота: {ex.Message}");
            }
        }

        private async Task ExportMessages(long chatId, string date)
        {
            if (!DateTime.TryParse(date, out var targetDate))
            {
                await _clientBot.SendTextMessageAsync(chatId, "Некорректная дата.");
                return;
            }

            var utcTargetDate = DateTime.SpecifyKind(targetDate, DateTimeKind.Utc);
            var messages = await db.Messages
                .Where(m => m.Timestamp.Date == utcTargetDate)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            var listUsId = new Dictionary<long, string>();
            foreach (var message in messages)


            if (!messages.Any())
            {
                await _clientBot.SendTextMessageAsync(chatId, "Нет сообщений за указанную дату.");
                return;
            }
            // Группируем сообщения по парам пользователей
            // Группируем сообщения по дням
            var groupedMessages = messages
                .GroupBy(m => m.Timestamp.Date)
                .OrderBy(g => g.Key)
                .ToList();

            // Создаем временный файл
            var filePath = Path.GetTempFileName();

            // Записываем данные в файл
            using (var writer = new StreamWriter(filePath))
            {
                foreach (var group in groupedMessages)
                {
                    // Заголовок для дня
                    await writer.WriteLineAsync($"============= {group.Key:yyyy-MM-dd} =============");

                    // Сообщения за день
                    foreach (var message in group.OrderBy(m => m.Timestamp))
                    {
                        await writer.WriteLineAsync($"{message.Timestamp:HH:mm:ss}: {message.FromUsername} ({message.FromUserId}) -> {message.ToUsername} ({message.ToUserId}): {message.Text}");
                    }

                    // Пустая строка между днями
                    await writer.WriteLineAsync();
                }
            }

            // Отправляем файл
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var inputFile = InputFile.FromStream(fileStream, $"messages_{targetDate:yyyy-MM-dd}.txt");

            await _clientBot.SendDocumentAsync(chatId, inputFile);

            // Удаляем временный файл
            File.Delete(filePath);
        }

        private async Task ViewMessages(long chatId, int page)
        {
            // Получаем все пары из базы данных
            var userPairs = await db.UserLinks
                .Select(ul => new { User1 = ul.UserId1, User2 = ul.UserId2, User3 = ul.UserName1, User4 = ul.UserName2 })
                .ToListAsync();

            if (!userPairs.Any())
            {
                await _clientBot.SendTextMessageAsync(chatId, "Нет связанных пар пользователей.");
                return;
            }
            // Разделяем пары на страницы
            const int pairsPerPage = 10;
            var totalPages = (int)Math.Ceiling(userPairs.Count / (double)pairsPerPage);

            // Проверяем, что запрошенная страница существует
            if (page < 0 || page >= totalPages)
            {
                await _clientBot.SendTextMessageAsync(chatId, "Страница не найдена.");
                return;
            }

            // Получаем пары для текущей страницы
            var pairsOnPage = userPairs
                .Skip(page * pairsPerPage)
                .Take(pairsPerPage)
                .ToList();

            // Форматируем список пар
            var pairsText = string.Join("\n", pairsOnPage.Select(p => $"{p.User1} {p.User3} <-> {p.User2} {p.User4}"));

            var inlineButtons = new List<InlineKeyboardButton>();

            // Добавляем кнопку "Назад", если это не первая страница
            if (page > 0)
            {
                inlineButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"pairs_prev_{page}"));
            }
            // Добавляем кнопку "Вперед", если это не последняя страница
            if (page < totalPages - 1)
            {
                inlineButtons.Add(InlineKeyboardButton.WithCallbackData("Вперед ➡️", $"pairs_next_{page}"));
            }

            // Добавляем кнопку "Закрыть"
            inlineButtons.Add(InlineKeyboardButton.WithCallbackData("❌ Закрыть", "close"));

            // Создаем клавиатуру
            var inlineKeyboard = new InlineKeyboardMarkup(new[] { inlineButtons });

            try
            {
                // Проверяем, есть ли сохраненное сообщение для этого чата
                if (_currentPage.ContainsKey(chatId))
                {
                    // Пытаемся отредактировать сообщение
                    await _clientBot.EditMessageTextAsync(
                        chatId,
                        _currentPage[chatId],
                        $"Список связанных пар (страница {page + 1} из {totalPages}):\n{pairsText}",
                        replyMarkup: inlineKeyboard
                    );
                }
                else
                {
                    // Если сообщение не сохранено, отправляем новое
                    var message = await _clientBot.SendTextMessageAsync(
                        chatId,
                        $"Список связанных пар (страница {page + 1} из {totalPages}):\n{pairsText}",
                        replyMarkup: inlineKeyboard
                    );
                    _currentPage[chatId] = message.MessageId; // Сохраняем ID сообщения
                }
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("message to edit not found"))
            {
                // Если сообщение не найдено (например, удалено), отправляем новое
                var message = await _clientBot.SendTextMessageAsync(
                    chatId,
                    $"Список связанных пар (страница {page + 1} из {totalPages}):\n{pairsText}",
                    replyMarkup: inlineKeyboard
                );
                _currentPage[chatId] = message.MessageId; // Обновляем ID сообщения
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
            {
                // Игнорируем ошибку, если сообщение не изменилось
                var message = await _clientBot.SendTextMessageAsync(
                    chatId,
                    $"Список связанных пар (страница {page + 1} из {totalPages}):\n{pairsText}",
                    replyMarkup: inlineKeyboard
                );
                _currentPage[chatId] = message.MessageId; // Обновляем ID сообщения
            }
        }

        private async Task RemoveBlacklistWord(long chatId, string word)
        {
            var blacklistWord = await db.BlacklistWords.FirstOrDefaultAsync(b => b.Word == word);
            if (blacklistWord == null)
            {
                await _clientBot.SendTextMessageAsync(chatId, $"Слово '{word}' не найдено в черном списке.");
                return;
            }

            db.BlacklistWords.Remove(blacklistWord);
            await db.SaveChangesAsync();

            await _clientBot.SendTextMessageAsync(chatId, $"Слово '{word}' удалено из черного списка.");
        }

        private async Task AddBlacklistWord(long chatId, string word)
        {
            var wordDb = await db.BlacklistWords.FirstOrDefaultAsync(x => x.Word == word);
            if(wordDb != null)
            {
                await _clientBot.SendTextMessageAsync(chatId, $"Слово '{word}' уже есть в черном списоке.");
                return;
            }
            db.BlacklistWords.Add(new BlacklistWord { Word = word });
            await db.SaveChangesAsync();

            await _clientBot.SendTextMessageAsync(chatId, $"Слово '{word}' добавлено в черный список.");
        }

        private async Task UnlinkUser(long chatId, long userId)
        {
            var userLink = await db.UserLinks
            .FirstOrDefaultAsync(ul => ul.UserId1 == userId || ul.UserId2 == userId);

            if (userLink == null)
            {
                await _clientBot.SendTextMessageAsync(chatId, "Пользователь не связан.");
                return;
            }

            db.UserLinks.Remove(userLink);
            await db.SaveChangesAsync();
            _currentPage.Remove(chatId);

            await _clientBot.SendTextMessageAsync(chatId, $"Пользователь {userLink.UserId1} и {userLink.UserId2} отвязан.");
        }

        private async Task LinkUsers(long chatId, long userId1, long userId2)
        {
            var existingLink = await db.UserLinks
            .FirstOrDefaultAsync(ul =>
                (ul.UserId1 == userId1 && ul.UserId2 == userId2) ||
                (ul.UserId1 == userId2 && ul.UserId2 == userId1));

            if (existingLink != null)
            {
                await _clientBot.SendTextMessageAsync(chatId, "Пользователи уже связаны.");
                return;
            }

            db.UserLinks.Add(new UserLink { UserId1 = userId1, UserId2 = userId2, UserName1 = "unknown1", UserName2 = "unknown1" });
            await db.SaveChangesAsync();
            _currentPage.Remove(chatId);

            await _clientBot.SendTextMessageAsync(chatId, $"Пользователи {userId1} и {userId2} связаны.");
        }

        private async Task SetWelcomeMessage(long chatId, string welcomeText)
        {
            var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "WelcomeMessage");
            if (setting == null)
            {
                db.Settings.Add(new Setting { Key = "WelcomeMessage", Value = welcomeText });
            }
            else
            {
                setting.Value = welcomeText;
            }

            await db.SaveChangesAsync();
            await _clientBot.SendTextMessageAsync(chatId, "Приветственное сообщение обновлено.");
        }

        private async Task RemoveAdmin(long chatId, long userId)
        {
            var admin = await db.Admins.FirstOrDefaultAsync(a => a.ChatId == userId);
            if (admin == null)
            {
                await _clientBot.SendTextMessageAsync(chatId, "Пользователь не является администратором.");
                return;
            }

            db.Admins.Remove(admin);
            await db.SaveChangesAsync();

            await _clientBot.SendTextMessageAsync(chatId, $"Пользователь {userId} удален из списка администраторов.");
        }

        private async Task AddAdmin(long chatId, long userId)
        {
            if (await db.Admins.AnyAsync(a => a.ChatId == userId))
            {
                await _clientBot.SendTextMessageAsync(chatId, "Пользователь уже является администратором.");
                return;
            }
            db.Admins.Add(new ListAdmins { ChatId = userId });
            db.Users.Add(new Users() { ChatId = chatId, Username = "unknown" });

            await db.SaveChangesAsync();
            await _clientBot.SendTextMessageAsync(chatId, $"Пользователь {userId} добавлен в список администраторов.");
        }

        private async Task<bool> IsAdmin(long chatId)
        {
            return await db.Admins.AnyAsync(a => a.ChatId == chatId);
        }

        internal async Task HandleUserMessage(long chatId, Message message)
        {
            try
            {
                var userLink = await db.UserLinks
                    .FirstOrDefaultAsync(ul => ul.UserId1 == chatId || ul.UserId2 == chatId);
                var listAdmin = db.Admins.Select(a => a.ChatId);
                var utcTargetDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                var blacklistWords = await db.BlacklistWords.ToListAsync();

                if (userLink == null)
                {
                    await _clientBot.SendTextMessageAsync(chatId, "Вы не связаны с другим пользователем.");
                    return;
                }

                // Определяем получателя
                var toUserId = userLink.UserId1 == message.From.Id ? userLink.UserId2 : userLink.UserId1;

                // Получаем username получателя
                var toUser = await db.Users.FirstOrDefaultAsync(u => u.ChatId == toUserId);
                var toUsername = toUser?.Username ?? "Unknown";

                // Сохраняем сообщение в базу данных
                db.Messages.Add(new MessageRecord
                {
                    FromUserId = message.From.Id,
                    FromUsername = message.From.Username,
                    ToUserId = toUserId,
                    ToUsername = toUsername,
                    Text = message.Text,
                    Timestamp = utcTargetDate
                });

                await db.SaveChangesAsync();


                // Проверка на черный список
                if (blacklistWords.Any(word => message.Text.Contains(word.Word, StringComparison.OrdinalIgnoreCase)))
                {
                    foreach (var adm in listAdmin)
                    {
                        try
                        {
                            await _clientBot.SendTextMessageAsync(adm, $"⚠️ Сообщение от {chatId} содержит запрещенное слово:\n{message.Text}");
                        }
                        catch (ApiRequestException ex)
                        {
                            // Обработка ошибок
                            if (ex.ErrorCode == 403)
                            {
                                await _clientBot.SendTextMessageAsync(adm, $"Администратор {chatId} заблокировал бота.");
                                Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                            }
                            else if (ex.ErrorCode == 400)
                            {
                                await _clientBot.SendTextMessageAsync(adm, $"Администратор {chatId} никогда не начинал диалог с ботом.");
                                Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                            }
                            else
                            {
                                await _clientBot.SendTextMessageAsync(adm, $"Ошибка при отправке сообщения администратор {chatId}: {ex.Message}");
                                Console.WriteLine($"Ошибка при отправке сообщения администратор {chatId}: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            await _clientBot.SendTextMessageAsync(adm, $"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                            // Обработка других исключений
                            Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                        }
                    }
                }

                // Пересылка сообщения
                if (message.Photo != null && message.Photo.Any())
                {
                    // Если фото несколько — отправляем их как медиагруппу
                    if (message.Photo.Length > 1)
                    {
                        var mediaGroup = message.Photo
                            .Select(p => new InputMediaPhoto(p.FileId))
                            .ToList();

                        await _clientBot.SendMediaGroupAsync(
                            chatId: toUserId,
                            media: mediaGroup);
                    }
                    else
                    {
                        // Если фото одно — отправляем его отдельно
                        var photo = message.Photo.Last();
                        await _clientBot.SendPhotoAsync(
                            chatId: toUserId,
                            photo: photo.FileId,
                            caption: message.Caption);
                    }
                }
                else if (!string.IsNullOrEmpty(message.Text))
                {
                    await _clientBot.SendTextMessageAsync(toUserId, message.Text);
                }
            }
            catch(ApiRequestException ex) 
            {
                var listAdmin = db.Admins.Select(a => a.ChatId);
                foreach (var adm in listAdmin)
                {
                    await _clientBot.SendTextMessageAsync(adm, $"Ошибка при отправке сообщения {chatId}: {ex.Message}");
                }
                Console.WriteLine($"Ошибка при отправке сообщения {chatId}: {ex.Message}");
            }
            catch (Exception ex)
            {
                var listAdmin = db.Admins.Select(a => a.ChatId);
                foreach (var adm in listAdmin)
                {
                    await _clientBot.SendTextMessageAsync(adm, $"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
                // Обработка других исключений
                Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
            }
        }

        public async Task BotClient_OnCallbackQuery(CallbackQuery e)
        {
            var chatId = e.Message.Chat.Id;
            var data = e.Data;

            if (data.StartsWith("pairs_"))
            {
                var parts = data.Split('_');
                var action = parts[1]; // "prev" или "next"
                var currentPage = int.Parse(parts[2]);

                // Вычисляем новую страницу
                var newPage = action == "prev" ? currentPage - 1 : currentPage + 1;

                // Отображаем новую страницу
                await ViewMessages(chatId, newPage);
            }

            if (data.StartsWith("export_"))
            {
                // Получаем ID пользователей из callback data
                var parts = data.Split('_');
                var userId1 = long.Parse(parts[2]);
                var userId2 = long.Parse(parts[3]);

                // Формируем файл с перепиской для выбранной пары
                await ExportPairMessages(chatId, userId1, userId2);
            }

            // Подтверждаем обработку CallbackQuery
            await _clientBot.AnswerCallbackQueryAsync(e.Id);

            if (data.StartsWith("close"))
            {
                await _clientBot.DeleteMessageAsync(chatId, e.Message.MessageId);
                _currentPage.Remove(chatId);
            }

            // Подтверждаем обработку CallbackQuery
            await _clientBot.AnswerCallbackQueryAsync(e.Id);
        }

        private async Task ExportPairMessages(long chatId, long userId1, long userId2)
        {
            // Получаем переписку для выбранной пары
            var messages = await db.Messages
                .Where(m => (m.FromUserId == userId1 && m.ToUserId == userId2) ||
                            (m.FromUserId == userId2 && m.ToUserId == userId1))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            if (!messages.Any())
            {
                await _clientBot.SendTextMessageAsync(chatId, "Нет сообщений для выбранной пары.");
                return;
            }

            // Создаем временный файл
            var filePath = Path.GetTempFileName();

            // Записываем данные в файл
            using (var writer = new StreamWriter(filePath))
            {
                foreach (var message in messages)
                {
                    await writer.WriteLineAsync($"{message.Timestamp:yyyy-MM-dd HH:mm:ss}: {message.FromUsername} -> {message.ToUsername}: {message.Text}");
                }
            }

            // Отправляем файл
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var inputFile = InputFile.FromStream(fileStream, $"messages_{userId1}_{userId2}.txt");

            await _clientBot.SendDocumentAsync(chatId, inputFile);

            // Удаляем временный файл
            File.Delete(filePath);
        }
    }
}
