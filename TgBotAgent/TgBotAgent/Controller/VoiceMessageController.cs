using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using TgBotAgent.DB;
using TgBotAgent.Models;

namespace TgBotAgent.Controller
{
    public class VoiceMessageController
    {
        private ITelegramBotClient _botClient;
        private readonly ApplicationDbContext db;

        public VoiceMessageController(ITelegramBotClient botClient, ApplicationDbContext _db)
        {
            _botClient = botClient;
            db = _db;
        }

        public async Task HandlerVois(long chatId, Update update)
        {
            var listAdmin = db.Admins.Select(a => a.ChatId);
            var user = await db.Users.Where(u => u.Id == chatId).FirstOrDefaultAsync();
            foreach (var adm in listAdmin)
            {
                try
                {
                    await _botClient.SendVoiceAsync(adm, update.Message.Voice);
                    if (user != null)
                        await _botClient.SendTextMessageAsync(adm, $"От {user.ChatId}  {user.Username}");
                    else
                        await _botClient.SendTextMessageAsync(adm, $"От пользователя, котого нет в базе данных");
                }
                catch (ApiRequestException ex)
                {
                    // Обработка ошибок
                    if (ex.ErrorCode == 403)
                    {
                        Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                    }
                    else if (ex.ErrorCode == 400)
                    {
                        Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
            }
        }
        public async Task HandlerDocument(long chatId, Update update)
        {
            var listAdmin = db.Admins.Select(a => a.ChatId);
            var user = await db.Users.Where(u => u.Id == chatId).FirstOrDefaultAsync();
            foreach (var adm in listAdmin)
            {
                try
                {
                    await _botClient.SendDocumentAsync(adm, update.Message.Document);
                    if (user != null)
                        await _botClient.SendTextMessageAsync(adm, $"От {user.ChatId}  {user.Username}");
                    else
                        await _botClient.SendTextMessageAsync(adm, $"От пользователя, котого нет в базе данных");
                }
                catch (ApiRequestException ex)
                {
                    // Обработка ошибок
                    if (ex.ErrorCode == 403)
                    {
                        Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                    }
                    else if (ex.ErrorCode == 400)
                    {
                        Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
            }
        }
        public async Task HandlerPhoto(long chatId, Update update)
        {
            var listAdmin = db.Admins.Select(a => a.ChatId);
            var user = await db.Users.Where(u => u.Id == chatId).FirstOrDefaultAsync();
            foreach (var adm in listAdmin)
            {
                try
                {
                    // Получаем самое большое фото из массива
                    var photo = update.Message.Photo.Last();

                    await _botClient.SendPhotoAsync(adm, InputFile.FromFileId(photo.FileId));
                    if(user != null)
                        await _botClient.SendTextMessageAsync(adm, $"От {user.ChatId}  {user.Username}");
                    else
                        await _botClient.SendTextMessageAsync(adm, $"От пользователя, котого нет в базе данных");
                }
                catch (ApiRequestException ex)
                {
                    // Обработка ошибок
                    if (ex.ErrorCode == 403)
                    {
                        Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                    }
                    else if (ex.ErrorCode == 400)
                    {
                        Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
            }
        }

        public async Task HandlerVideo(long chatId, Update update)
        {
            var listAdmin = db.Admins.Select(a => a.ChatId);
            var user = await db.Users.Where(u => u.Id == chatId).FirstOrDefaultAsync();
            foreach (var adm in listAdmin)
            {
                try
                {
                    await _botClient.SendVideoAsync(adm,InputFile.FromFileId(update.Message.Video.FileId));
                    if (user != null)
                        await _botClient.SendTextMessageAsync(adm, $"От {user.ChatId}  {user.Username}");
                    else
                        await _botClient.SendTextMessageAsync(adm, $"От пользователя, котого нет в базе данных");
                }
                catch (ApiRequestException ex)
                {
                    // Обработка ошибок
                    if (ex.ErrorCode == 403)
                    {
                        Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                    }
                    else if (ex.ErrorCode == 400)
                    {
                        Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
            }
        }

        public async Task HandlerAnimation(long chatId, Update update)
        {
            var listAdmin = db.Admins.Select(a => a.ChatId);
            var user = await db.Users.Where(u => u.Id == chatId).FirstOrDefaultAsync();
            foreach (var adm in listAdmin)
            {
                try
                {
                    await _botClient.SendAnimationAsync(adm, update.Message.Animation);
                    if (user != null)
                        await _botClient.SendTextMessageAsync(adm, $"От {user.ChatId}  {user.Username}");
                    else
                        await _botClient.SendTextMessageAsync(adm, $"От пользователя, котого нет в базе данных");
                }
                catch (ApiRequestException ex)
                {
                    // Обработка ошибок
                    if (ex.ErrorCode == 403)
                    {
                        Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                    }
                    else if (ex.ErrorCode == 400)
                    {
                        Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
            }
        }

        public async Task HandlerAudio(long chatId, Update update)
        {
            var listAdmin = db.Admins.Select(a => a.ChatId);
            var user = await db.Users.Where(u => u.Id == chatId).FirstOrDefaultAsync();
            foreach (var adm in listAdmin)
            {
                try
                {
                    await _botClient.SendAudioAsync(adm, update.Message.Audio);
                    if (user != null)
                        await _botClient.SendTextMessageAsync(adm, $"От {user.ChatId}  {user.Username}");
                    else
                        await _botClient.SendTextMessageAsync(adm, $"От пользователя, котого нет в базе данных");
                }
                catch (ApiRequestException ex)
                {
                    // Обработка ошибок
                    if (ex.ErrorCode == 403)
                    {
                        Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                    }
                    else if (ex.ErrorCode == 400)
                    {
                        Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
            }
        }
        public async Task HandlerContact(long chatId, Update update)
        {
            var listAdmin = db.Admins.Select(a => a.ChatId);
            var user = await db.Users.Where(u => u.Id == chatId).FirstOrDefaultAsync();
            foreach (var adm in listAdmin)
            {
                try
                {
                    await _botClient.SendContactAsync(adm, update.Message.Contact.PhoneNumber, update.Message.Contact.FirstName);
                    if (user != null)
                        await _botClient.SendTextMessageAsync(adm, $"От {user.ChatId}  {user.Username}");
                    else
                        await _botClient.SendTextMessageAsync(adm, $"От пользователя, котого нет в базе данных");
                }
                catch (ApiRequestException ex)
                {
                    // Обработка ошибок
                    if (ex.ErrorCode == 403)
                    {
                        Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                    }
                    else if (ex.ErrorCode == 400)
                    {
                        Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
            }
        }

        public async Task HandlerSticker(long chatId, Update update)
        {
            var listAdmin = db.Admins.Select(a => a.ChatId);
            var user = await db.Users.Where(u => u.Id == chatId).FirstOrDefaultAsync();
            foreach (var adm in listAdmin)
            {
                try
                {
                    await _botClient.SendStickerAsync(adm, update.Message.Sticker);
                    if (user != null)
                        await _botClient.SendTextMessageAsync(adm, $"От {user.ChatId}  {user.Username}");
                    else
                        await _botClient.SendTextMessageAsync(adm, $"От пользователя, котого нет в базе данных");
                }
                catch (ApiRequestException ex)
                {
                    // Обработка ошибок
                    if (ex.ErrorCode == 403)
                    {
                        Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                    }
                    else if (ex.ErrorCode == 400)
                    {
                        Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
            }
        }

        public async Task HandlerLocation(long chatId, Update update)
        {
            var listAdmin = db.Admins.Select(a => a.ChatId);
            var user = await db.Users.Where(u => u.Id == chatId).FirstOrDefaultAsync();
            foreach (var adm in listAdmin)
            {
                try
                {
                    await _botClient.SendLocationAsync(adm, update.Message.Location.Latitude, update.Message.Location.Longitude);
                    if (user != null)
                        await _botClient.SendTextMessageAsync(adm, $"От {user.ChatId}  {user.Username}");
                    else
                        await _botClient.SendTextMessageAsync(adm, $"От пользователя, котого нет в базе данных");
                }
                catch (ApiRequestException ex)
                {
                    // Обработка ошибок
                    if (ex.ErrorCode == 403)
                    {
                        Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                    }
                    else if (ex.ErrorCode == 400)
                    {
                        Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
            }
        }

        public async Task HandlerVideoNote(long chatId, Update update)
        {
            var listAdmin = db.Admins.Select(a => a.ChatId);
            var user = await db.Users.Where(u => u.Id == chatId).FirstOrDefaultAsync();
            foreach (var adm in listAdmin)
            {
                try
                {
                    await _botClient.SendLocationAsync(adm, update.Message.Location.Latitude, update.Message.Location.Longitude);
                    if (user != null)
                        await _botClient.SendTextMessageAsync(adm, $"От {user.ChatId}  {user.Username}");
                    else
                        await _botClient.SendTextMessageAsync(adm, $"От пользователя, котого нет в базе данных");
                }
                catch (ApiRequestException ex)
                {
                    // Обработка ошибок
                    if (ex.ErrorCode == 403)
                    {
                        Console.WriteLine($"Администратор {chatId} заблокировал бота.");
                    }
                    else if (ex.ErrorCode == 400)
                    {
                        Console.WriteLine($"Администратор {chatId} никогда не начинал диалог с ботом.");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Обработка других исключений
                    Console.WriteLine($"Неизвестная ошибка при отправке сообщения администратору {chatId}: {ex.Message}");
                }
            }
        }
    }
}
