using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TgBotYandexMar.Models;

namespace TgBotYandexMar.Services
{
    public class YandexMarketApiService
    {
        private readonly HttpClient _httpClient;
        private int _currentPage = 1; // Текущая страница
        private const int PageSize = 30; // Количество товаров на странице

        public YandexMarketApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Метод для поиска товаров
        public async Task<List<Product>> SearchProductsAsync(string keyword, string apiKey, int geoId = 1, bool exactMatch = false, string fields = "MODEL_MEDIA,MODEL_DEFAULT_OFFER,MODEL_PRICE,MODEL_RATING,OFFER_PHOTO")
        {
            var url = $"https://api.content.market.yandex.ru/v3/affiliate/search?text={Uri.EscapeDataString(keyword)}&geo_id={geoId}&fields={fields}&page={_currentPage}&count={PageSize}";
            if (exactMatch)
            {
                url += "&exact-match=true";
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(apiKey);

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка при запросе к API Яндекс.Маркета: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<YandexMarketSearchResponse>(content);

            var products = result?.Items.Select(item => new Product
            {
                Name = item.Name,
                MinPrice = item.Price?.Min ?? "0", // Минимальная цена
                MaxPrice = item.Price?.Max ?? "0", // Максимальная цена
                AvgPrice = item.Price?.Avg ?? "0", // Средняя цена
                Url = item.Link,
                Rating = item.Rating?.Value ?? 0,
                OpinionCount = item.OpinionCount ?? 0,
                Photos = item.Offer?.Photos?.Select(p => p.Url).ToList() ?? new List<string>()
            }).ToList() ?? new List<Product>();

            // Если товаров нет, сбрасываем страницу на 1
            if (products.Count == 0)
            {
                _currentPage = 1;
            }
            else if(_currentPage == 50)
            {
                _currentPage = 1;
            }
            else
            {
                _currentPage++; // Переходим на следующую страницу
            }

            return products;
        }

        // Метод для создания партнерской ссылки
        public async Task<string> CreatePartnerLinkAsync(string productUrl, Channel channel, string eridToken)
        {
            // Проверяем, есть ли OAuth-токен
            if (string.IsNullOrEmpty(channel.OAuthToken) || channel.OAuthToken == "пусто")
            {
                Console.WriteLine("OAuth-токен отсутствует. Пожалуйста, авторизуйтесь.");
            }

            if (eridToken == null) 
            {
                Console.WriteLine("Erid-токен отсутствует.");
            }

            // Проверяем, истек ли токен
            if (channel.OAuthTokenExpiresAt == null)
            {
                Console.WriteLine("OAuth-токен истек. Пожалуйста, обновите токен.");
            }

            // Создаем партнерскую ссылку
            var url = $"https://api.content.market.yandex.ru/v3/affiliate/partner/link/create?url={Uri.EscapeDataString(productUrl)}&clid={channel.Clid}&erid={eridToken}";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(channel.ApiKey);// Возможно нужно будет добавить в начале OAuth итог $"OAuth {channel.OAuthToken}"

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка при создании партнерской ссылки: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<YandexMarketLinkResponse>(content);

            return result?.Link?.Url ?? productUrl;
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }

        public async Task<string> GetEridTokenAsync(string productUrl, string apiKey, string clid, string postText, List<string> mediaUrls, Product product, string OAuthToken)
        {
            var url = "https://distribution.yandex.net/api/v2/creatives/";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add($"Authorization", $"OAuth {OAuthToken}");
            //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("AOuth", OAuthToken);
            var requestBody = new
            {
                clid = clid,
                form = "text-graphic-block",
                text_data = new[] { postText }, // Текст поста
                media_data = mediaUrls.Select(mediaUrl => new
                {
                    media_url = mediaUrl, // URL изображения товара
                    media_url_file_type = "image",
                    description = $"Яндекс Маркет, партнёрская ссылка. {product.Name}"
                }).ToArray(),
                description = $"Яндекс Маркет, партнёрская ссылка. {product.Name}",
                service = "market",
                type = "cpa",
                urls = new[] { productUrl }, // Ссылка на товар
                okveds = new[] { "62.01" } // Код ОКВЭД
            };
            var content1 = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            request.Content = content1;
            var response = await _httpClient.SendAsync(request);
            //var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var result1 = JsonSerializer.Deserialize<CheckErrorApiYandex>(content);
                Console.WriteLine(result1.Data);
                Console.WriteLine(result1.Error);
                Console.WriteLine($"Ошибка при создании креатива: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<DataResponse>(content);

            return result?.Data.Token;
        }

        private class DataResponse
        {
            [JsonPropertyName("data")]
            public EridResponse Data { get; set; }
        }

        private class EridResponse
        {
            [JsonPropertyName("token")]
            public string Token { get; set; }
        }

        // Классы для десериализации JSON-ответа
        private class YandexMarketSearchResponse
        {
            [JsonPropertyName("items")]
            public List<YandexMarketItem> Items { get; set; }
        }

        private class YandexMarketItem
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("link")]
            public string Link { get; set; }

            [JsonPropertyName("price")]
            public YandexMarketPrice Price { get; set; }

            [JsonPropertyName("rating")]
            public YandexMarketRating Rating { get; set; }

            [JsonPropertyName("opinionCount")]
            public int? OpinionCount { get; set; }

            [JsonPropertyName("offer")]
            public YandexMarketOffer Offer { get; set; }
        }

        private class YandexMarketPrice
        {
            [JsonPropertyName("min")]
            public string Min { get; set; }

            [JsonPropertyName("max")]
            public string Max { get; set; }

            [JsonPropertyName("avg")]
            public string Avg { get; set; }
        }

        private class YandexMarketRating
        {
            [JsonPropertyName("value")]
            public double Value { get; set; }

            [JsonPropertyName("count")]
            public int Count { get; set; }
        }

        private class YandexMarketOffer
        {
            [JsonPropertyName("photos")]
            public List<YandexMarketPhoto> Photos { get; set; }
        }

        private class YandexMarketPhoto
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        private class YandexMarketLinkResponse
        {
            [JsonPropertyName("link")]
            public YandexMarketLink Link { get; set; }
        }

        private class YandexMarketLink
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }
        private class CheckErrorApiYandex
        {
            [JsonPropertyName("result")]
            public string Error { get; set; }
            [JsonPropertyName("data")]
            public string Data {  get; set; }
        }
    }
}
