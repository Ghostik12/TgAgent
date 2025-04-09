
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace YandexParser
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            var spisok = await SearchProductsAsync("телефон", "CjbJfVH2fH4GPcEgPjWWQpSb5kMxrq", 1, false, "MODEL_MEDIA,MODEL_DEFAULT_OFFER,MODEL_PRICE,MODEL_RATING,OFFER_PHOTO");
        }

        static public async Task<List<Product>> SearchProductsAsync(string keyword, string apiKey, int geoId = 1, bool exactMatch = false, string fields = "MODEL_MEDIA,MODEL_DEFAULT_OFFER,MODEL_PRICE,MODEL_RATING,OFFER_PHOTO")
        {
            int _currentPage = 1;
            int PageSize = 30;
            var _httpClient = new HttpClient();
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
            else if (_currentPage == 50)
            {
                _currentPage = 1;
            }
            else
            {
                _currentPage++; // Переходим на следующую страницу
            }

            return products;
        }

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string MinPrice { get; set; } // Минимальная цена
            public string MaxPrice { get; set; } // Максимальная цена
            public string AvgPrice { get; set; } // Средняя цена
            public string Url { get; set; }
            public double Rating { get; set; }
            public int OpinionCount { get; set; }
            public bool IsPosted { get; set; }
            public DateTime? PostedAt { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public int ChannelId { get; set; }
            public string Keyword { get; set; }
            public List<string> Photos { get; internal set; }
        }
    }
    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class DataResponse
    {
        [JsonPropertyName("data")]
        public EridResponse Data { get; set; }
    }

    public class EridResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }

    // Классы для десериализации JSON-ответа
    public class YandexMarketSearchResponse
    {
        [JsonPropertyName("items")]
        public List<YandexMarketItem> Items { get; set; }
    }

    public class YandexMarketItem
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

    public class YandexMarketPrice
    {
        [JsonPropertyName("min")]
        public string Min { get; set; }

        [JsonPropertyName("max")]
        public string Max { get; set; }

        [JsonPropertyName("avg")]
        public string Avg { get; set; }
    }

    public class YandexMarketRating
    {
        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class YandexMarketOffer
    {
        [JsonPropertyName("photos")]
        public List<YandexMarketPhoto> Photos { get; set; }
    }

    public class YandexMarketPhoto
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class YandexMarketLinkResponse
    {
        [JsonPropertyName("link")]
        public YandexMarketLink Link { get; set; }
    }

    public class YandexMarketLink
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
    public class CheckErrorApiYandex
    {
        [JsonPropertyName("result")]
        public string Error { get; set; }
        [JsonPropertyName("data")]
        public string Data { get; set; }
    }
}