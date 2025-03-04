using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System.Text;

namespace ParserAli
{
    internal class Program
    {
            static async Task Main(string[] args)
            {
                var apiKey = "e1edb8f19acccfc7a70d1541d8fba30f";
                var userHash = "s5tk7whzmfsqn4k70am2iov1z1nfqi9s";

                var epnClient = new EpnApiClient(apiKey, userHash);

                // Указываем дату, относительно которой будем проверять товары
                var targetDate = new DateTime(2025, 02, 04); // Например, 1 октября 2023 года

                // Лимит товаров на один запрос
                var limit = 10;
                // Смещение для пагинации
                var offset = 0;
                // Флаг для остановки цикла
                var found = false;

                // Цикл будет выполняться, пока не найдем подходящий товар
                while (!found)
                {
                    // Создаем запрос для поиска товаров
                    var searchRequest = new EpnApiRequest
                    {
                        UserApiKey = apiKey,
                        UserHash = userHash,
                        Requests = new Dictionary<string, EpnApiActionRequest>
                {
                    {
                        "search_request",
                        new EpnApiActionRequest
                        {
                            Action = "search",
                            Query = "телефон",
                            Limit = limit,
                            Offset = offset,
                            Lang = "ru",
                            Currency = "RUR",
                            OrderBy = "added_at", // Сортировка по дате добавления
                            OrderDirection = "desc" // Сначала новые товары
                        }
                    }
                }
                    };

                    // Отправляем запрос на поиск
                    var searchResponse = await epnClient.SendRequestAsync(searchRequest);

                    // Обрабатываем ответ
                    if (searchResponse.Error != null)
                    {
                        Console.WriteLine("Ошибка при поиске: " + searchResponse.Error);
                        return;
                    }

                    // Извлекаем результаты поиска
                    var searchResults = searchResponse.Results["search_request"] as JObject;
                    var offers = searchResults["offers"] as JArray;

                    if (offers == null || offers.Count == 0)
                    {
                        Console.WriteLine("Товары не найдены.");
                        return;
                    }

                    Console.WriteLine($"Проверено товаров: {offset + offers.Count}");

                    // Фильтруем товары по дате добавления
                    foreach (var offer in offers)
                    {
                        var addedAtStr = offer["added_at"]?.ToString(); // Дата добавления товара
                        if (DateTime.TryParse(addedAtStr, out var addedAt))
                        {
                            if (addedAt >= targetDate)
                            {
                                // Найден подходящий товар
                                Console.WriteLine($"\nНайден подходящий товар:");
                                Console.WriteLine($"ID: {offer["id"]}, Название: {offer["name"]}, Цена: {offer["price"]} {offer["currency"]}, Дата добавления: {addedAt:yyyy-MM-dd}");
                                found = true; // Останавливаем цикл
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"Товар {offer["id"]} добавлен {addedAt:yyyy-MM-dd} (не подходит)");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Товар {offer["id"]} имеет некорректную дату добавления: {addedAtStr}");
                        }
                    }

                    // Увеличиваем смещение для следующего запроса
                    offset += limit;
                }
            }
        }

        public class EpnApiClient
    {
        private const string ApiUrl = "http://api.epn.bz/json";
        private readonly string _apiKey;
        private readonly string _userHash;

        public EpnApiClient(string apiKey, string userHash)
        {
            _apiKey = apiKey;
            _userHash = userHash;
        }

        public async Task<EpnApiResponse> SendRequestAsync(EpnApiRequest request)
        {
            using var httpClient = new HttpClient();
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(ApiUrl, content);
            response.EnsureSuccessStatusCode(); // Проверка на успешный запрос

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<EpnApiResponse>(responseJson);
        }
    }

    // Модели для запросов и ответов
    public class EpnApiRequest
    {
        [JsonProperty("user_api_key")]
        public string UserApiKey { get; set; }

        [JsonProperty("user_hash")]
        public string UserHash { get; set; }

        [JsonProperty("api_version")]
        public string ApiVersion { get; set; } = "2";

        [JsonProperty("requests")]
        public Dictionary<string, EpnApiActionRequest> Requests { get; set; }
    }

    public class EpnApiActionRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; } = "en";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; } = "USD";

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; } = 10;

        [JsonProperty("offset")]
        public int Offset { get; set; } = 0;

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("price_min")]
        public double PriceMin { get; set; } = 0.0;

        [JsonProperty("price_max")]
        public double PriceMax { get; set; } = 1000000.0;

        [JsonProperty("orderby")]
        public string OrderBy { get; set; }

        [JsonProperty("order_direction")]
        public string OrderDirection { get; set; }
    }

    public class EpnApiResponse
    {
        [JsonProperty("identified_as")]
        public string IdentifiedAs { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("results")]
        public Dictionary<string, object> Results { get; set; }
    }
}
