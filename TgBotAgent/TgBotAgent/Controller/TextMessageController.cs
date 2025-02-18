using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgBotAgent.DB;
using TgBotAgent.Models;

namespace TgBotAgent.Controller
{
    public class TextMessageController
    {
        private ITelegramBotClient _clientBot;

        public TextMessageController(ITelegramBotClient clientBot)
        {
            _clientBot = clientBot;
        }

        internal async Task CheckUserOrAdmin(Update update)
        {
            var chatId = update.Message.Chat.Id;
            if (await IsAdmin(chatId))
            {
                await AdminMenu(chatId);
            }
            else
            {
                await WelcomeMessage(chatId);
            }
        }

        private async Task WelcomeMessage(long chatId)
        {
            using var db = new ApplicationDbContext();
            var message = await db.Settings.FirstOrDefaultAsync(s => s.Key == "WelcomeMessage");
            await _clientBot.SendTextMessageAsync(chatId, message.Value);
        }

        private async Task AdminMenu(long chatId)
        {
            await _clientBot.SendTextMessageAsync(chatId, "Команды для администратора:\n" +
                "/admin link id1 id2\n/admin unlink id1\n/admin addblacklist слово\n" +
                "/admin removeblacklist слово\n/admin export 2025-01-01\n/admin setwelcome приветственное сообщение\n" +
                "/admin addadmin id\n/admin setname новое имя бота");
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
                case "setname":
                    if (parts.Length < 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin setname <новое имя>");
                        return;
                    }
                    var newName = string.Join(" ", parts.Skip(2));
                    await SetBotName(chatId, newName);
                    break;
                case "addadmin":
                    if (parts.Length < 3 || parts.Length > 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin addadmin <id>");
                        return;
                    }
                    await AddAdmin(chatId, long.Parse(parts[2]));
                    break;
                case "removeadmin":
                    if (parts.Length < 3 || parts.Length > 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin removeadmin <id>");
                        return;
                    }
                    await RemoveAdmin(chatId, long.Parse(parts[2]));
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
                case "addblacklist":
                    await AddBlacklistWord(chatId, string.Join(" ", parts.Skip(2)));
                    break;
                case "removeblacklist":
                    await RemoveBlacklistWord(chatId, string.Join(" ", parts.Skip(2)));
                    break;
                case "viewmessages":
                    await ViewMessages(chatId);
                    break;
                case "export":
                    if (parts.Length < 3)
                    {
                        await _clientBot.SendTextMessageAsync(chatId, "Использование: /admin export <дата>");
                        return;
                    }
                    await ExportMessages(chatId, parts[2]);
                    break;
                default:
                    await _clientBot.SendTextMessageAsync(chatId, "Неизвестная команда.");
                    break;
            }
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
            using var db = new ApplicationDbContext();
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

            if (!messages.Any())
            {
                await _clientBot.SendTextMessageAsync(chatId, "Нет сообщений за указанную дату.");
                return;
            }

            var filePath = Path.GetTempFileName();
            await File.WriteAllLinesAsync(filePath, messages.Select(m =>
                $"{m.Timestamp}: {m.FromUserId} -> {m.ToUserId}: {m.Text}"));

            // Используем InputFile вместо InputOnlineFile
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var inputFile = InputFile.FromStream(fileStream, $"messages_{targetDate:yyyy-MM-dd}.txt");

            await _clientBot.SendDocumentAsync(chatId, inputFile);

            File.Delete(filePath);
        }

        private async Task ViewMessages(long chatId)
        {
            throw new NotImplementedException();
        }

        private async Task RemoveBlacklistWord(long chatId, string word)
        {
            using var db = new ApplicationDbContext();
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
            using var db = new ApplicationDbContext();
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
            using var db = new ApplicationDbContext();
            var userLink = await db.UserLinks
            .FirstOrDefaultAsync(ul => ul.UserId1 == userId || ul.UserId2 == userId);

            if (userLink == null)
            {
                await _clientBot.SendTextMessageAsync(chatId, "Пользователь не связан.");
                return;
            }

            db.UserLinks.Remove(userLink);
            await db.SaveChangesAsync();

            await _clientBot.SendTextMessageAsync(chatId, $"Пользователь {userLink.UserId1} и {userLink.UserId2} отвязан.");
        }

        private async Task LinkUsers(long chatId, long userId1, long userId2)
        {
            using var db = new ApplicationDbContext();
            var existingLink = await db.UserLinks
            .FirstOrDefaultAsync(ul =>
                (ul.UserId1 == userId1 && ul.UserId2 == userId2) ||
                (ul.UserId1 == userId2 && ul.UserId2 == userId1));

            if (existingLink != null)
            {
                await _clientBot.SendTextMessageAsync(chatId, "Пользователи уже связаны.");
                return;
            }

            db.UserLinks.Add(new UserLink { UserId1 = userId1, UserId2 = userId2 });
            await db.SaveChangesAsync();

            await _clientBot.SendTextMessageAsync(chatId, $"Пользователи {userId1} и {userId2} связаны.");
        }

        private async Task SetWelcomeMessage(long chatId, string welcomeText)
        {
            using var db = new ApplicationDbContext();
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
            using var db = new ApplicationDbContext();
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
            using var db = new ApplicationDbContext();
            if (await db.Admins.AnyAsync(a => a.ChatId == userId))
            {
                await _clientBot.SendTextMessageAsync(chatId, "Пользователь уже является администратором.");
                return;
            }

            db.Admins.Add(new ListAdmins { ChatId = userId });
            await db.SaveChangesAsync();

            await _clientBot.SendTextMessageAsync(chatId, $"Пользователь {userId} добавлен в список администраторов.");
        }

        private async Task<bool> IsAdmin(long chatId)
        {
            using var db = new ApplicationDbContext();
            return await db.Admins.AnyAsync(a => a.ChatId == chatId);
        }

        internal async Task HandleUserMessage(long chatId, Message message)
        {
            using var db = new ApplicationDbContext();
            var userLink = await db.UserLinks
            .FirstOrDefaultAsync(ul => ul.UserId1 == chatId || ul.UserId2 == chatId);

            if (userLink == null)
            {
                await _clientBot.SendTextMessageAsync(chatId, "Вы не связаны с другим пользователем.");
                return;
            }

            var targetUserId = userLink.UserId1 == chatId ? userLink.UserId2 : userLink.UserId1;
            await _clientBot.SendTextMessageAsync(targetUserId, message.Text);

            // Сохраняем сообщение в базе данных
            db.Messages.Add(new MessageRecord
            {
                FromUserId = chatId,
                ToUserId = targetUserId,
                Text = message.Text,
                Timestamp = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            });

            await db.SaveChangesAsync();

            // Проверка на черный список
            var blacklistWords = await db.BlacklistWords.ToListAsync();
            if (blacklistWords.Any(word => message.Text.Contains(word.Word, StringComparison.OrdinalIgnoreCase)))
            {
                await _clientBot.SendTextMessageAsync("ADMIN_CHAT_ID", $"⚠️ Сообщение от {chatId} содержит запрещенное слово:\n{message.Text}");
            }
        }
    }
}
