using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace ParserAli
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static string _clientId = "5CBhjY0PmwqG4Esc9LylHONvSJuUT7ig"; // Ваш client_id
        private static string _secretId = "cokH4jtYXzvFsg28PGh3pbM7nOLVrCw5";
        private static string _refreshToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoicmVmcmVzaF90b2tlbiIsImV4cCI6MTc0MzMzOTgxNSwidG9rZW4iOiIzOGU5ZDBkZjMzNzc3MmUzZWVkNTlmODJkNjFjMTM1NzQ2Y2NlNjM3IiwidXNlcl9pZCI6Njk0Mzc5LCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNoZWNrX2lwIjpmYWxzZSwic2NvcGUiOiJ1c2VyX2lzc3VlZF90b2tlbiJ9.wHQ2g12n0pCdVm10uV_XvepIMxwqiJhsDUManKUfdOsv9XdcXTtF3sNUaduv3rcZFyxks7KOoAJ8m_c5VqFFUA";
        private static string _accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoiYWNjZXNzX3Rva2VuIiwiZXhwIjoxNzQyMTMwMjE1LCJ1c2VyX2lkIjo2OTQzNzksInVzZXJfcm9sZSI6InVzZXIiLCJjbGllbnRfcGxhdGZvcm0iOiJ3ZWIiLCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNsaWVudF9pZCI6IlE2Z0tEdWkxRnQ1SWhIWWtjQ3MzbVdHanBiVkVvQVA5IiwiY2hlY2tfaXAiOmZhbHNlLCJ0b2tlbiI6ImI0ZjMwMmZiYzdmMjljNzIyNjQ4NWQ1MmNiODY1ZGU5ZDU1MGIwZjQiLCJzY29wZSI6InVzZXJfaXNzdWVkX3Rva2VuIn0.kP3DeEhD0dh1DuIU5Yn0rciPcnhpLdr4YQlnsI0EuCyFXGXY3MYudXC1xmtpmG3ETbwBqrLsDhEUvbv5JqThyw";
        static async Task Main(string[] args)
        {
            string link = $"https://aliexpress.ru/item/.html"; // Замените на реальную ссылку на товар
            string language = "ru"; // Язык (например, "ru" или "en")

            var ssidToken = await GetSsidToken();
            await GetAccessRefresh(ssidToken);
        }

        private static async Task GetAccessRefresh(string ssidToken)
        {
            var requestUri = $"https://oauth2.epn.bz/token?v=2";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("Content_Type", "application/json");
            var body = new
            {
                ssid_token = ssidToken,
                client_id = _clientId,
                client_secret = _secretId,
                grant_type = "client_credential",
                check_ip = false
            };
            var content1 = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            request.Content = content1;
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var responseInfo = JsonConvert.DeserializeObject<TokensResponse>(jsonResponse);
                Console.WriteLine($"Access = {responseInfo.Data.Attributes.AccessToken}");
                Console.WriteLine($"Refresh = {responseInfo.Data.Attributes.RefreshToken}");
            }
            else
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var responseInfo = JsonConvert.DeserializeObject<TokensResponse>(jsonResponse);
            }
        }

        static async Task<string> GetSsidToken()
        {
            string ssidToken = null;
            bool captchaRequired = true;

            while (captchaRequired)
            {
                var requestUri = $"https://oauth2.epn.bz/ssid?v=2&client_id={_clientId}";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseInfo = JsonConvert.DeserializeObject<TokensResponse>(jsonResponse);
                    ssidToken = responseInfo.Data.Attributes.SsidToken;
                    captchaRequired = false;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // 429
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var error = JObject.Parse(jsonResponse);
                    var captchaData = error["errors"][0]["captcha"];

                    string captchaType = captchaData["type"].ToString();
                    string captchaPhraseKey = captchaData["captcha_phrase_key"].ToString();

                    if (captchaType == "image")
                    {
                        // Капча в виде изображения
                        string captchaImageBase64 = captchaData["captcha"]["image"].ToString();

                        // Удаляем префикс "data:image/jpeg;base64,"
                        if (captchaImageBase64.StartsWith("data:image/jpeg;base64,"))
                        {
                            captchaImageBase64 = captchaImageBase64.Replace("data:image/jpeg;base64,", "");
                        }

                        // Декодируем Base64 в изображение
                        byte[] imageBytes = Convert.FromBase64String(captchaImageBase64);
                        string imagePath = "captcha.png";
                        File.WriteAllBytes(imagePath, imageBytes);

                        Console.WriteLine("Капча сохранена в файл 'captcha.png'. Откройте файл и введите текст с изображения:");
                        string captchaSolution = Console.ReadLine();

                        // Повторяем запрос с решением капчи
                        requestUri = $"{requestUri}&captcha={captchaSolution}&captcha_phrase_key={captchaPhraseKey}";
                    }
                    else if (captchaType == "reCaptcha")
                    {
                        // reCAPTCHA
                        Console.WriteLine("Требуется ввод reCAPTCHA. Пожалуйста, решите капчу и введите токен:");
                        string captchaSolution = Console.ReadLine();

                        // Повторяем запрос с решением капчи
                        requestUri = $"{requestUri}&captcha={captchaSolution}&captcha_phrase_key={captchaPhraseKey}";
                    }
                }
                else
                {
                    Console.WriteLine("Ошибка при получении SSID токена.");
                    break;
                }
            }

            return ssidToken;
        }

        static async Task CheckProductAsync(string link, string accessToken, string language)
        {
            var requestUri = $"https://app.epn.bz/affiliate/checkLink?link={Uri.EscapeDataString(link)}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("X-ACCESS-TOKEN", accessToken);
            request.Headers.Add("ACCEPT-LANGUAGE", language);

            try
            {
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var productInfo = JsonConvert.DeserializeObject<ProductResponse>(jsonResponse);

                    if (productInfo.Result)
                    {
                        Console.WriteLine("\nИнформация о товаре:");
                        Console.WriteLine($"Название: {productInfo.Data.Attributes.ProductName}");
                        Console.WriteLine($"Процент кэшбэка: {productInfo.Data.Attributes.CashbackPercent}%");
                        Console.WriteLine($"Ссылка для перехода: {productInfo.Data.Attributes.RedirectUrl}");
                        Console.WriteLine($"Изображение товара: {productInfo.Data.Attributes.ProductImage}");
                        Console.WriteLine($"Логотип магазина: {productInfo.Data.Attributes.LogoSmall}");
                        Console.WriteLine($"Тип аффилиата: {productInfo.Data.Attributes.AffiliateType}");
                        Console.WriteLine($"Хотсейл: {productInfo.Data.Attributes.IsHotsale}");
                        Console.WriteLine($"Максимальная ставка: {productInfo.Data.Attributes.MaxRate}");
                        Console.WriteLine($"Описание ставок: {productInfo.Data.Attributes.RatesDesc}");
                        Console.WriteLine($"Динамика: {productInfo.Data.Attributes.HasDynamics}");
                        Console.WriteLine($"Кэшбэк доступен: {productInfo.Data.Attributes.CashbackAvailable}");
                    }
                    else
                    {
                        Console.WriteLine("Товар не найден или произошла ошибка.");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("Access Token истек. Пробуем обновить токены...");

                    // Обновляем токены
                    var newTokens = await RefreshTokensAsync(_refreshToken);
                    if (newTokens != null)
                    {
                        // Обновляем access_token и refresh_token
                        _accessToken = newTokens.Data.Attributes.AccessToken;
                        _refreshToken = newTokens.Data.Attributes.RefreshToken;

                        Console.WriteLine("Токены успешно обновлены.");
                        Console.WriteLine($"Новый Access Token: {_accessToken}");
                        Console.WriteLine($"Новый Refresh Token: {_refreshToken}");

                        // Повторяем запрос с новым access_token
                        await CheckProductAsync(link, _accessToken, language);
                    }
                    else
                    {
                        Console.WriteLine("Не удалось обновить токены.");
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}");
                    Console.WriteLine(errorResponse);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }

        static async Task<TokensResponse> RefreshTokensAsync(string refreshToken)
        {
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

            try
            {
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<TokensResponse>(jsonResponse);
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка при обновлении токенов: {response.StatusCode}");
                    Console.WriteLine(errorResponse);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
                return null;
            }
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
        [JsonProperty("ssid_token")]
        public string SsidToken { get; set; }
    }
}
