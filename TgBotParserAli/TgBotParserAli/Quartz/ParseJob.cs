using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TgBotParserAli.DB;
using TgBotParserAli.Models;

namespace TgBotParserAli.Quartz
{
    public class ParseJob
    {
        private readonly EpnApiClient _epnApiClient;
        private readonly AppDbContext _dbContext;
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;
        private static readonly SemaphoreSlim _parseSemaphore = new SemaphoreSlim(1, 1);

        public ParseJob(AppDbContext appDbContext, EpnApiClient epnApiClient, DbContextOptions<AppDbContext> dbContextOptions)
        {
            _dbContext = appDbContext;
            _epnApiClient = epnApiClient;
            _dbContextOptions = dbContextOptions;
        }

        public async Task Execute(Channel channel, KeywordSetting keywordSetting)
        {
            await _parseSemaphore.WaitAsync();
            try
            {
                using (var dbContext = new AppDbContext(_dbContextOptions))
                {

                    // Получаем количество неопубликованных товаров
                    var unpublishedProductsCount = await dbContext.Products
                        .CountAsync(p => p.ChannelId == channel.Id && !p.IsPosted);

                    // Проверяем, нужно ли начинать парсинг
                    if (unpublishedProductsCount >= channel.MaxPostsPerDay)
                    {
                        Console.WriteLine($"Количество неопубликованных товаров ({unpublishedProductsCount}) достигло лимита ({channel.MaxPostsPerDay}). Парсинг остановлен.");
                        return;
                    }

                    if (!channel.IsActive || !keywordSetting.IsParsing)
                    {
                        return; // Пропускаем неактивные каналы или ключевые слова
                    }

                    Console.WriteLine($"Парсинг товаров для канала {channel.Name}...");

                    Console.WriteLine($"Парсинг для ключевого слова: {keywordSetting.Keyword}");

                    // Парсим товары для текущего ключевого слова
                    var response = await _epnApiClient.SearchProductsAsync(
                        query: keywordSetting.Keyword,
                        limit: channel.ParseCount,
                        offset: channel.ParsedCount,
                        orderBy: "orders_count",
                        orderDirection: "desc"
                    );
                    if (response.Contains("Ошибка") || response.Contains("Неизвестная"))
                    {
                        Console.WriteLine(response);
                        return;
                    }

                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<EpnApiResponse>(response);

                        if (apiResponse.Error == null && apiResponse.Results.ContainsKey("search_request"))
                        {
                            var searchResult = apiResponse.Results["search_request"];
                            var offers = searchResult.Offers;

                            int addedProductsCount = 0;
                            var productsToAdd = new List<Product>();

                            foreach (var offer in offers)
                            {
                                if (IsProductValid(offer, channel))
                                {
                                    // Создаем список изображений
                                    var images = new List<string>();

                                    // Добавляем основное изображение (picture)
                                    if (!string.IsNullOrEmpty(offer.Picture))
                                    {
                                        images.Add(offer.Picture);
                                    }

                                    // Добавляем все изображения из all_images, если они есть
                                    if (offer.AllImages != null && offer.AllImages.Any())
                                    {
                                        images.AddRange(offer.AllImages);
                                    }

                                    // Создаем новый товар
                                    var newProduct = new Product
                                    {
                                        ProductId = offer.Id.ToString(),
                                        Name = offer.Name,
                                        Price = offer.Price,
                                        DiscountedPrice = offer.SalePrice,
                                        Images = images, // Сохраняем все изображения
                                        ChannelId = channel.Id,
                                        Url = offer.Url,
                                        Keyword = keywordSetting.Keyword // Сохраняем ключевое слово
                                    };
                                    productsToAdd.Add(newProduct);
                                    addedProductsCount++;
                                }
                            }
                            dbContext.Products.AddRange(productsToAdd);
                            await dbContext.SaveChangesAsync();

                            // Обновляем ParsedCount
                            channel.ParsedCount += channel.ParseCount;

                            // Сохраняем статистику по ключевому слову
                            await UpdateKeywordStat(channel.Id, keywordSetting.Keyword, addedProductsCount);

                            // Сохраняем изменения
                            await dbContext.SaveChangesAsync();

                            Console.WriteLine($"Парсинг завершен для ключевого слова '{keywordSetting.Keyword}'. Добавлено {addedProductsCount} товаров.");
                        }
                        else
                        {
                            Console.WriteLine($"Ошибка при парсинге для ключевого слова '{keywordSetting.Keyword}': {apiResponse.Error}");
                        }
                    }
                    catch (JsonSerializationException ex)
                    {
                        Console.WriteLine($"Ошибка десериализации для ключевого слова '{keywordSetting.Keyword}': {ex.Message}");
                        Console.WriteLine($"JSON-ответ: {response}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при удалении {ex.Message}");
                    }
                }
            }
            finally 
            {
                _parseSemaphore.Release();
            }
        }

        private bool IsProductValid(ProductOffer offer, Channel channel)
        {
            // Проверка на диапазон цен и другие критерии
            return offer.Price >= channel.MinPrice && offer.Price <= channel.MaxPrice;
        }

        private async Task UpdateKeywordStat(int channelId, string keyword, int addedProductsCount)
        {
            using (var dbContext = new AppDbContext(_dbContextOptions))
            {
                // Ищем запись в статистике по ключевому слову
                var keywordStat = await dbContext.KeywordStats
                .FirstOrDefaultAsync(k => k.ChannelId == channelId && k.Keyword == keyword);

                if (keywordStat == null)
                {
                    // Если записи нет, создаем новую
                    keywordStat = new KeywordStat
                    {
                        ChannelId = channelId,
                        Keyword = keyword,
                        Count = addedProductsCount,
                        LastUpdated = DateTime.UtcNow
                    };
                    dbContext.KeywordStats.Add(keywordStat);
                }
                else
                {
                    // Если запись есть, обновляем количество и дату
                    keywordStat.Count += addedProductsCount;
                    keywordStat.LastUpdated = DateTime.UtcNow;
                    dbContext.KeywordStats.Update(keywordStat);
                }
            }
        }
    }
}
