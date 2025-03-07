using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System.Text;

namespace ParserAli
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            string link = "https://aliexpress.ru/item/1005002232904103.html"; // Замените на реальную ссылку на товар
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoiYWNjZXNzX3Rva2VuIiwiZXhwIjoxNzQxNDM3MDUxLCJ1c2VyX2lkIjo2OTQzNzksInVzZXJfcm9sZSI6InVzZXIiLCJjbGllbnRfcGxhdGZvcm0iOiJ3ZWIiLCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNsaWVudF9pZCI6IlE2Z0tEdWkxRnQ1SWhIWWtjQ3MzbVdHanBiVkVvQVA5IiwiY2hlY2tfaXAiOmZhbHNlLCJ0b2tlbiI6IjdmNjI0NGJlNzMzMTI4MWIwOWM3ZWMxY2ExMTUwODg1MTFjN2MyNGQiLCJzY29wZSI6InVzZXJfaXNzdWVkX3Rva2VuIn0.8Y9uQGVHL3fc49KYLGYeG97IiIQMqVO1Awvk4ESjsD9AcZlL1J6SC_wf81eChvq72EzdLPoEWLU9xrBTL1eN7A"; // Замените на ваш access token
            string language = "ru"; // Язык (например, "ru" или "en")

            await CheckProductAsync(link, accessToken, language);
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
                    Console.WriteLine("Access Token истек. Необходимо обновить токены.");
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
}
