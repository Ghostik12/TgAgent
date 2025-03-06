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
        private readonly ITelegramBotClient _botClient;
        private readonly AppDbContext _dbContext;
        private readonly VkLinkShortener _linkShortener;

        public PostJob(AppDbContext appDbContext, ITelegramBotClient botClient, VkLinkShortener linkShortener)
        {
            _dbContext = appDbContext;
            _botClient = botClient;
            _linkShortener = linkShortener;
        }

        public async Task Execute(Channel channel, KeywordSetting keywordSetting)
        {
            if (!channel.IsActive || !keywordSetting.IsPosting)
            {
                return; // Пропускаем неактивные каналы или ключевые слова
            }

            Console.WriteLine($"Постинг товаров для канала {channel.Name}...");

            // Получаем товары для текущего ключевого слова
            var products = await _dbContext.Products
                .Where(p => p.ChannelId == channel.Id && !p.IsPosted && p.Keyword == keywordSetting.Keyword)
                .ToListAsync();

            foreach (var product in products)
            {
                try
                {
                    // Проверяем, не достигнут ли лимит постов за день
                    if (channel.PostedToday >= channel.MaxPostsPerDay)
                    {
                        Console.WriteLine($"Лимит постов за день достигнут для канала {channel.Name}.");
                        break;
                    }
                    // Отправляем сообщение с товаром
                    await SendProductMessageAsync(channel, product, _botClient, _linkShortener);

                    // Помечаем товар как опубликованный
                    product.IsPosted = true;
                    _dbContext.Products.Update(product);

                    // Увеличиваем счетчик опубликованных постов за день
                    channel.PostedToday++;
                    _dbContext.Channels.Update(channel);

                    // Сохраняем изменения
                    await _dbContext.SaveChangesAsync();

                    // Ждем перед постингом следующего товара
                    await Task.Delay(keywordSetting.PostFrequency);
                }
                catch (ApiRequestException ex)
                {
                    Console.WriteLine($"Ошибка при отправке товара {product.Name}: {ex.Message}");
                    channel.FailedPosts++; // Увеличиваем счетчик неудачных постов
                    await _dbContext.SaveChangesAsync();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private async Task SendProductMessageAsync(Channel channel, Product product, ITelegramBotClient botClient, VkLinkShortener linkShortener)
        {
            try{
                // Формируем ссылку
                var productUrl = $"{channel.ReferralLink}{product.ProductId}.html";
                var uriStr = new Uri(productUrl);
                var finalUrl = channel.UseShortLinks
                    ? await linkShortener.ShortenLinkAsync(uriStr)
                    : productUrl;

                // Формируем сообщение
                var message = $"<b>{product.Name}</b>\n" +
                              $"Цена: <s>{product.Price}</s> {product.DiscountedPrice} RUB\n" +
                              $"Ссылка: {finalUrl}";

                // Если есть изображения, отправляем их как медиагруппу
                if (product.Images != null && product.Images.Any())
                {
                    var mediaGroup = new List<InputMediaPhoto>();

                    // Первое фото с текстом (подписью)
                    mediaGroup.Add(new InputMediaPhoto(product.Images.First())
                    {
                        Caption = message,
                        ParseMode = ParseMode.Html
                    });

                    // Остальные фото (без текста)
                    foreach (var image in product.Images.Skip(1))
                    {
                        mediaGroup.Add(new InputMediaPhoto(image));
                    }

                    // Отправляем медиагруппу
                    await botClient.SendMediaGroupAsync(
                        chatId: channel.ChatId,
                        media: mediaGroup);
                }
                else
                {
                    // Если нет фото, отправляем только текст
                    await botClient.SendTextMessageAsync(
                        chatId: channel.ChatId,
                        text: message,
                        parseMode: ParseMode.Html);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
