using HtmlAgilityPack;
using Newtonsoft.Json;
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

            // Создаем запрос
            var request = new EpnApiRequest
            {
                UserApiKey = apiKey,
                UserHash = userHash,
                Requests = new Dictionary<string, EpnApiActionRequest>
            {
                {
                    "search_request_1",
                    new EpnApiActionRequest
                    {
                        Action = "search",
                        Query = "phone",
                        Limit = 1000,
                        Offset = 0,
                        Lang = "en",
                        Currency = "USD"
                    }
                }
            }
            };

            // Отправляем запрос
            var response = await epnClient.SendRequestAsync(request);

            // Обрабатываем ответ
            if (response.Error != null)
            {
                Console.WriteLine("Ошибка: " + response.Error);
            }
            else
            {
                Console.WriteLine("Ответ от API:");
                Console.WriteLine(JsonConvert.SerializeObject(response.Results, Formatting.Indented));
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

    // Модель для основного запроса
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

    // Модель для действия (action)
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
    }

    // Модель для ответа
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
