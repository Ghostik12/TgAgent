using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using TgBotParserAli.Models;

namespace TgBotParserAli
{
    public class EpnApiClient
    {
        private const string ApiUrl = "http://api.epn.bz/json";
        private readonly string _apiKey;
        private readonly string _userHash;
        private const string CheckProductUrl = "https://app.epn.bz/affiliate/checkLink";
        private string _clientId;

        public EpnApiClient(string apiKey, string userHash, string clientId)
        {
            _apiKey = apiKey;
            _userHash = userHash;
            _clientId = clientId;
        }

        public async Task<string> SearchProductsAsync(string query, int limit, int offset = 0, string orderBy = "added_at", string orderDirection = "desc")
        {
            try
            {
                using var httpClient = new HttpClient();
                var request = new
                {
                    user_api_key = _apiKey,
                    user_hash = _userHash,
                    api_version = "2",
                    lang = "ru",
                    requests = new
                    {
                        search_request = new
                        {
                            action = "search",
                            query,
                            limit,
                            offset,
                            currency = "RUR",
                            lang = "ru",
                            orderby = orderBy,
                            order_direction = orderDirection
                        }
                    },
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(ApiUrl, content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                return result; // Успешный запрос, ошибок нет
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"Ошибка HTTP-запроса: {ex.Message}";
                Console.WriteLine(errorMessage); // Логируем ошибку
                return errorMessage;
            }
            catch (JsonSerializationException ex)
            {
                string errorMessage = $"Ошибка десериализации JSON: {ex.Message}";
                Console.WriteLine(errorMessage); // Логируем ошибку
                return errorMessage;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Неизвестная ошибка: {ex.Message}";
                Console.WriteLine(errorMessage); // Логируем ошибку
                return errorMessage;
            }
        }
        public async Task<ProductResponse> CheckProductAsync(string link, string accessToken, string language = "ru")
        {
            var _httpClient = new HttpClient();
            var requestUri = $"{CheckProductUrl}?link={Uri.EscapeDataString(link)}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("X-ACCESS-TOKEN", accessToken);
            request.Headers.Add("ACCEPT-LANGUAGE", language);

            var response = await _httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access Token истек. Пробуем обновить токены...");
                return null;
            }
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ProductResponse>(jsonResponse);
        }

        public async Task<TokensResponse> RefreshTokensAsync(string refreshToken)
        {
            var _httpClient = new HttpClient();
            var requestUri = "https://oauth2.epn.bz/token/refresh?v=2";
            var body = new
            {
                grant_type = "refresh_token",
                refresh_token = refreshToken,
                client_id = _clientId
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("X-API-VERSION", "2");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TokensResponse>(jsonResponse);
        }
    }
    public class RequestData
    {
        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("otherField")]
        public string OtherField { get; set; }
    }

    // Классы для десериализации JSON-ответа
    public class ProductResponse
    {
        [JsonProperty("data")]
        public ProductData Data { get; set; }

        [JsonProperty("result")]
        public bool Result { get; set; }

        [JsonProperty("request")]
        public RequestData Request { get; set; }
    }

    public class ProductData
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("attributes")]
        public ProductAttributes Attributes { get; set; }
    }

    public class ProductAttributes
    {
        [JsonProperty("redirectUrl")]
        public string RedirectUrl { get; set; }

        [JsonProperty("offerName")]
        public string OfferName { get; set; }

        [JsonProperty("cashbackPercent")]
        public string CashbackPercent { get; set; }

        [JsonProperty("isHotsale")]
        public bool? IsHotsale { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("productImage")]
        public string ProductImage { get; set; }

        [JsonProperty("logoSmall")]
        public string LogoSmall { get; set; }

        [JsonProperty("affiliateType")]
        public int AffiliateType { get; set; }

        [JsonProperty("maxRate")]
        public string MaxRate { get; set; }

        [JsonProperty("rates")]
        public object Rates { get; set; } // Может быть объектом или массивом, уточните структуру

        [JsonProperty("ratesDesc")]
        public string RatesDesc { get; set; }

        [JsonProperty("hasDynamics")]
        public bool HasDynamics { get; set; }

        [JsonProperty("cashbackAvailable")]
        public bool CashbackAvailable { get; set; }
    }

    public class TokensResponse
    {
        [JsonProperty("data")]
        public TokensData Data { get; set; }

        [JsonProperty("result")]
        public bool Result { get; set; }

        [JsonProperty("request")]
        public object Request { get; set; }
    }

    public class TokensData
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("attributes")]
        public TokensAttributes Attributes { get; set; }
    }

    public class TokensAttributes
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("isAuth")]
        public bool IsAuth { get; set; }
    }
}
