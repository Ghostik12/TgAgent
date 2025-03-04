using Newtonsoft.Json;
using System;
using System.Globalization;
using TgBotParserAli.DB;
using TgBotParserAli.Models;

namespace TgBotParserAli.Quartz
{
    public class ParseJob
    {
        private readonly EpnApiClient _epnApiClient;
        private readonly AppDbContext _dbContext;

        public ParseJob(EpnApiClient epnApiClient, AppDbContext dbContext)
        {
            _epnApiClient = epnApiClient;
            _dbContext = dbContext;
        }

        public async Task Execute(Channel channel)
        {
            Console.WriteLine($"Парсинг товаров для канала {channel.Name}...");

            // Парсим товары через ePN API с учетом настроек канала
            var response = await _epnApiClient.SearchProductsAsync(
                query: channel.Keywords,
                limit: channel.ParseCount,
                offset: channel.ParsedCount, // Используем ParsedCount как offset
                orderBy: "added_at",       // Сортировка по дате добавления
                orderDirection: "desc"    // Сначала новые товары
            );

            try
            {
                var apiResponse = JsonConvert.DeserializeObject<EpnApiResponse>(response);

                if (apiResponse.Error == null && apiResponse.Results.ContainsKey("search_request"))
                {
                    var searchResult = apiResponse.Results["search_request"];
                    var offers = searchResult.Offers;

                    int addedProductsCount = 0; // Счетчик добавленных товаров

                    foreach (var offer in offers)
                    {
                        // Проверяем, подходит ли товар по всем критериям
                        if (IsProductValid(offer, channel))
                        {
                            // Создаем список изображений
                            var images = new List<string>();

                            // Добавляем главную картинку
                            images.Add(offer.Picture);

                            // Если есть дополнительные картинки, берем первые две
                            if (offer.AllImages != null && offer.AllImages.Any())
                            {
                                // Берем максимум две дополнительные картинки
                                var additionalImages = offer.AllImages.Take(2).ToList();
                                images.AddRange(additionalImages);
                            }

                            var newProduct = new Product
                            {
                                ProductId = offer.Id.ToString(),
                                Name = offer.Name,
                                Price = offer.Price,
                                DiscountedPrice = offer.SalePrice,
                                Images = images,
                                ChannelId = channel.Id,
                                Url = offer.Url
                            };
                            _dbContext.Products.Add(newProduct);
                            addedProductsCount++;
                        }
                    }
                    // Обновляем ParsedCount на значение ParseCount
                    channel.ParsedCount += channel.ParseCount;
                    await _dbContext.SaveChangesAsync();
                    Console.WriteLine($"Парсинг завершен. Добавлено {addedProductsCount} товаров.");
                }
                else
                {
                    Console.WriteLine($"Ошибка при парсинге: {apiResponse.Error}");
                }
            }
            catch (JsonSerializationException ex)
            {
                Console.WriteLine($"Ошибка десериализации: {ex.Message}");
                Console.WriteLine($"JSON-ответ: {response}");
            }
        }

        private bool IsProductValid(ProductOffer offer, Channel channel)
        {
            // Проверка на наличие товара в базе данных
            if (_dbContext.Products.Any(p => p.ProductId == offer.Id.ToString()))
            {
                return false; // Товар уже существует в базе данных
            }

            // Проверка на актуальность (добавлен ли товар за последние 30 дней)
            if (offer.AddedAt != null)
            {
                var addedDate = DateTime.Parse(offer.AddedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                if ((DateTime.Now - addedDate) > TimeSpan.FromDays(30))
                {
                    return false; // Товар не актуален
                }
            }

            // Проверка на диапазон цен
            if (offer.Price < channel.MinPrice || offer.Price > channel.MaxPrice)
            {
                return false; // Товар не подходит по цене
            }

            // Проверка на наличие изображений
            if (string.IsNullOrEmpty(offer.Picture))
            {
                return false; // Товар не подходит, если нет изображения
            }

            // Проверка на ключевые слова
            if (!string.IsNullOrEmpty(channel.Keywords))
            {
                var keywords = channel.Keywords.Split(',');
                if (!keywords.Any(k => offer.Name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    return false; // Товар не подходит по ключевым словам
                }
            }

            return true; // Товар подходит
        }
    }
}
