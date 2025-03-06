using VkNet;
using VkNet.Model;

namespace TgBotParserAli
{
    public class VkLinkShortener
    {
        private readonly VkApi _vkApi;

        public VkLinkShortener(string accessToken)
        {
            _vkApi = new VkApi();
            _vkApi.Authorize(new ApiAuthParams
            {
                AccessToken = accessToken
            });
        }

        public async Task<string> ShortenLinkAsync(Uri url)
        {
            try
            {
                var response = await _vkApi.Utils.GetShortLinkAsync(url, false);
                var result = response.ShortUrl.ToString();
                return result; // Возвращаем сокращенную ссылку
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сокращении ссылки: {ex.Message}");
                var result = url.ToString();
                return result; // Возвращаем оригинальную ссылку в случае ошибки
            }
        }
    }
}
