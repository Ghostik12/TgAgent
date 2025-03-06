using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System.Text;

namespace ParserAli
{
    class Program
    {
        private const string ApiUrl = "http://api.epn.bz/json";
        private const string ApiKey = "e1edb8f19acccfc7a70d1541d8fba30f"; // Замените на ваш API ключ
        private const string UserHash = "s5tk7whzmfsqn4k70am2iov1z1nfqi9s"; // Замените на ваш user hash

        static async Task Main(string[] args)
        {
            string productId = "1005004323522689"; // ID товара для проверки

            try
            {
                var productInfo = await GetProductInfoAsync(productId);
                Console.WriteLine("Ответ от API:");
                Console.WriteLine(productInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        public static async Task<string> GetProductInfoAsync(string productId)
        {
            using var httpClient = new HttpClient();
            var request = new
            {
                user_api_key = ApiKey,
                user_hash = UserHash,
                api_version = "2",
                lang = "ru",
                requests = new
                {
                    product_info = new
                    {
                        action = "offer_info",
                        id = productId,
                        lang = "ru",
                        currency = "RUR,USD"
                    }
                },
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(ApiUrl, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
