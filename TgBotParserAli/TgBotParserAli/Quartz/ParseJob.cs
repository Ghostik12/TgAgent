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
        private TokenService _tokenService;

        public ParseJob(AppDbContext appDbContext, EpnApiClient epnApiClient, DbContextOptions<AppDbContext> dbContextOptions, TokenService tokenService)
        {
            _dbContext = appDbContext;
            _epnApiClient = epnApiClient;
            _dbContextOptions = dbContextOptions;
            _tokenService = tokenService;
        }

        public async Task Execute(int channelId, KeywordSetting keywordSetting)
        {
            await _parseSemaphore.WaitAsync();
            try
            {
                using (var dbContext = new AppDbContext(_dbContextOptions))
                {
                    var channel = await dbContext.Channels.FirstOrDefaultAsync(c => c.Id == channelId);
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
                                    // Получаем актуальный access_token
                                    var accessToken = await _tokenService.GetAccessTokenAsync();
                                    if (accessToken == null) { Console.WriteLine("Проблема с токеном"); return; }
                                    // Проверяем товар через API
                                    var productLink = $"https://aliexpress.ru/item/{offer.Id}.html";
                                    var productInfo = await _epnApiClient.CheckProductAsync(productLink, accessToken);
                                    if (productInfo == null)
                                    {
                                        var refreshToken = await _dbContext.Tokens.FirstOrDefaultAsync();
                                        var newTokens = await _epnApiClient.RefreshTokensAsync(refreshToken.RefreshToken);
                                        refreshToken.RefreshToken = newTokens.Data.Attributes.RefreshToken;
                                        refreshToken.AccessToken = newTokens.Data.Attributes.AccessToken;
                                        refreshToken.ExpiresAt = DateTime.UtcNow.AddDays(1);
                                        _dbContext.Tokens.Update(refreshToken);
                                        await _dbContext.SaveChangesAsync();
                                        productInfo = await _epnApiClient.CheckProductAsync(productLink, newTokens.Data.Attributes.AccessToken);
                                    }

                                    if (productInfo.Result && !string.IsNullOrEmpty(productInfo.Data.Attributes.ProductName) && !string.IsNullOrEmpty(productInfo.Data.Attributes.ProductImage))
                                    {
                                        var existingProduct = await dbContext.Products
                                            .FirstOrDefaultAsync(p => p.Url == offer.Url);

                                        if (existingProduct != null)
                                        {
                                            continue; // Пропускаем товар, если он уже существует
                                        }
                                        // Создаем список изображений
                                        var images = new List<string>();

                                        // Добавляем все изображения из all_images, если они есть
                                        if (offer.AllImages != null && offer.AllImages.Any())
                                        {
                                            images.AddRange(offer.AllImages);
                                        }
                                        else
                                        {
                                            // Добавляем основное изображение (picture)
                                            if (!string.IsNullOrEmpty(offer.Picture))
                                            {
                                                images.Add(offer.Picture);
                                            }
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
                                        dbContext.Products.Add(newProduct);
                                        addedProductsCount++;
                                    }
                                }
                            }

                            // Обновляем ParsedCount
                            channel.ParsedCount += channel.ParseCount;
                            dbContext.Channels.Update(channel);

                            // Сохраняем изменения
                            await dbContext.SaveChangesAsync();

                            // Сохраняем статистику по ключевому слову
                            await UpdateKeywordStat(channel.Id, keywordSetting.Keyword, addedProductsCount);

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
            try
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

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString() + "123");
            }
        }
    }
}
