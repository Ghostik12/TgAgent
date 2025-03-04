using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using TgBotParserAli.DB;
using Microsoft.EntityFrameworkCore;
using TgBotParserAli.Models;
using Telegram.Bot.Exceptions;

namespace TgBotParserAli.Quartz
{
    public class PostJob
    {
        private readonly AppDbContext _dbContext;
        private readonly ITelegramBotClient _botClient;
        private bool _isRunning = false; // Флаг для блокировки
        private CancellationTokenSource _cancellationTokenSource; // Источник токена отмены

        public PostJob(AppDbContext dbContext, ITelegramBotClient botClient)
        {
            _dbContext = dbContext;
            _botClient = botClient;
        }

        public async Task Execute(Channel channel)
        {
            // Если задача уже выполняется, пропускаем новый запуск
            if (_isRunning || !channel.IsActive) // Добавлена проверка на IsActive
            {
                Console.WriteLine("Задача уже выполняется или канал остановлен. Пропускаем запуск.");
                return;
            }
            try
            {
                _isRunning = true; // Блокируем задачу
                _cancellationTokenSource = new CancellationTokenSource(); // Создаем новый токен отмены

                var cancellationToken = _cancellationTokenSource.Token;

                Console.WriteLine($"Постинг товаров для канала {channel.Name}...");

                // Получаем товары, которые ещё не были опубликованы
                var products = await _dbContext.Products
                    .Where(p => p.ChannelId == channel.Id && !p.IsPosted)
                    .Take(channel.MaxPostsPerDay) // Ограничиваем количество постов
                    .ToListAsync();

                foreach (var product in products)
                {
                    try
                    {
                        // Проверяем, не была ли задача отменена
                        cancellationToken.ThrowIfCancellationRequested();

                        // Формируем сообщение
                        var message = $"<b>{product.Name}</b>\n" +
                                      $"Цена: <s>{product.Price}</s> {product.DiscountedPrice} RUB\n" +
                                      $"{channel.ReferralLink}{product.ProductId}.html";
                        if (product.Images != null)
                        {
                            // Отправляем фото и описание
                            var mediaGroup = product.Images
                                .Select(image => new InputMediaPhoto(image))
                                .ToList();


                            // Отправляем медиагруппу
                            await _botClient.SendMediaGroupAsync(channel.ChatId, mediaGroup);
                        }

                        // Отправляем текстовое сообщение
                        await _botClient.SendTextMessageAsync(
                            chatId: channel.ChatId,
                            text: message,
                            parseMode: ParseMode.Html);

                        // Помечаем товар как опубликованный
                        product.IsPosted = true;

                        // Сохраняем изменения
                        await _dbContext.SaveChangesAsync();

                        // Добавляем задержку, основанную на частоте постинга
                        Console.WriteLine($"Ожидание {channel.PostFrequency} до следующего поста...");
                        await Task.Delay(channel.PostFrequency);
                    }
                    catch (ApiRequestException ex)
                    {
                        Console.WriteLine($"Ошибка при отправке товара {product.Name}: {ex.Message}");
                        // Пропускаем этот товар и продолжаем с остальными
                    }
                }

                Console.WriteLine($"Постинг завершен. Опубликовано {products.Count} товаров.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении постинга: {ex.Message}");
            }
            finally
            {
                _isRunning = false; // Разблокируем задачу
            }
        }

        // Метод для отмены задачи
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
