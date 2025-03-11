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
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public PostJob(AppDbContext appDbContext, ITelegramBotClient botClient, VkLinkShortener linkShortener, DbContextOptions<AppDbContext> dbContextOptions)
        {
            _dbContext = appDbContext;
            _botClient = botClient;
            _linkShortener = linkShortener;
            _dbContextOptions = dbContextOptions;
        }

        public async Task Execute(Channel channel, KeywordSetting keywordSetting)
        {
            await _semaphore.WaitAsync();
            try
            {
                using (var dbContext = new AppDbContext(_dbContextOptions)) // Передайте _dbContextOptions в конструктор PostJob
                {
                    if (!channel.IsActive || !keywordSetting.IsPosting)
                    {
                        return; // Пропускаем неактивные каналы или ключевые слова
                    }

                    Console.WriteLine($"Постинг товаров для канала {channel.Name}...");

                    // Получаем товары для текущего ключевого слова
                    var products = await dbContext.Products
                        .AsNoTracking()
                        .Where(p => !p.IsPosted && p.Keyword == keywordSetting.Keyword)
                        .ToListAsync();

                    if (!products.Any())
                    {
                        Console.WriteLine($"Нет товаров для постинга в канале {channel.Name} (ключевое слово: {keywordSetting.Keyword}).");
                        return;
                    }

                    // Выбираем случайный товар
                    var random = new Random();
                    var product = products[random.Next(products.Count)];

                    try
                    {
                        // Проверяем, не достигнут ли лимит постов за день
                        if (channel.PostedToday >= channel.MaxPostsPerDay)
                        {
                            Console.WriteLine($"Лимит постов за день достигнут для канала {channel.Name}.");
                            return;
                        }

                        // Отправляем сообщение с товаром
                        await SendProductMessageAsync(channel, product, _botClient, _linkShortener);

                        // Помечаем товар как опубликованный
                        product.IsPosted = true;
                        dbContext.Products.Update(product);

                        // Увеличиваем счетчик опубликованных постов за день
                        channel.PostedToday++;
                        dbContext.Channels.Update(channel);

                        // Сохраняем изменения
                        await dbContext.SaveChangesAsync();

                        Console.WriteLine($"Товар {product.Name} успешно опубликован.");
                    }
                    catch (ApiRequestException ex)
                    {
                        Console.WriteLine($"Ошибка при отправке товара {product.Name}: {ex.Message}");
                        channel.FailedPosts++; // Увеличиваем счетчик неудачных постов
                        await dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            finally 
            {
                _semaphore.Release();
            }
        }

        private async Task SendProductMessageAsync(Channel channel, Product product, ITelegramBotClient botClient, VkLinkShortener linkShortener)
        {
            try{
                using (var dbContext = new AppDbContext(_dbContextOptions))
                {
                    var postSettings = await dbContext.PostSetting
                        .FirstOrDefaultAsync(ps => ps.ChannelId == channel.Id);

                    // Формируем ссылку
                    var productUrl = $"{channel.ReferralLink}{product.ProductId}.html";
                    var uriStr = new Uri(productUrl);
                    var finalUrl = channel.UseShortLinks
                        ? await linkShortener.ShortenLinkAsync(uriStr)
                        : productUrl;

                    // Формируем сообщение
                    string message;
                    if (postSettings == null ||
                        (string.IsNullOrEmpty(postSettings.PriceTemplate) &&
                        string.IsNullOrEmpty(postSettings.TitleTemplate) &&
                        string.IsNullOrEmpty(postSettings.CaptionTemplate)))
                    {
                        // Стандартный формат, если настройки отсутствуют
                        message = $"<b>{product.Name}</b>\n" +
                                  $"Цена: <s>{product.Price}</s> {product.DiscountedPrice} RUB\n" +
                                  $"Ссылка: {finalUrl}";
                    }
                    else
                    {
                        // Формируем сообщение в соответствии с порядком из настроек
                        var messageParts = new List<string>();

                        var orderParts = postSettings.Order.Split(','); // Разбиваем порядок на части

                        foreach (var part in orderParts)
                        {
                            switch (part)
                            {
                                case "Price":
                                    messageParts.Add($"Цена: <s>{product.Price}</s> {product.DiscountedPrice} RUB");
                                    break;
                                case "Title":
                                    messageParts.Add($"<b>{product.Name}</b>");
                                    break;
                                case "Caption":
                                    messageParts.Add(postSettings.CaptionTemplate);
                                    break;
                            }
                        }

                        messageParts.Add($"Ссылка: {finalUrl}");
                        message = string.Join("\n", messageParts);
                    }

                    // Отправляем сообщение с фотографиями
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

                        await botClient.SendMediaGroupAsync(channel.ChatId, mediaGroup);
                    }
                    else
                    {
                        // Если нет фото, отправляем только текст
                        await botClient.SendTextMessageAsync(channel.ChatId, message, parseMode: ParseMode.Html);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
