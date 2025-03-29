using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgBotParserAli.DB;
using Microsoft.EntityFrameworkCore;
using TgBotParserAli.Models;

namespace TgBotParserAli
{
    public class TokenService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly AppDbContext _dbContext;

        public TokenService(HttpClient httpClient, AppDbContext dbContext, string clientId, string clientSecret)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // Получаем токен из базы данных
            var token = await _dbContext.Tokens.FirstOrDefaultAsync();
            if (token == null)
            {
                // Если токена нет, получаем новый
                var ssidToken = await GetSsidTokenAsync();
                var newTokens = await GetTokensAsync(ssidToken);

                // Сохраняем токены в базу данных
                token = new Token
                {
                    AccessToken = newTokens.Data.Attributes.AccessToken,
                    RefreshToken = newTokens.Data.Attributes.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddDays(1) // Пример: токен действует 1 час
                };
                _dbContext.Tokens.Add(token);
                await _dbContext.SaveChangesAsync();
                return token.AccessToken;
            }
            else if (token.ExpiresAt <= DateTime.UtcNow)
            {
                // Если токен истек, обновляем его
                var newToken = await RefreshTokensAsync();
                if (newToken == null) { return null; }
                return newToken.Data.Attributes.AccessToken;
            }

            return token.AccessToken;
        }

        private async Task<string> GetSsidTokenAsync()
        {
            var response = await _httpClient.GetAsync($"https://oauth2.epn.bz/ssid?v=2&client_id={_clientId}");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var ssidResponse = JsonConvert.DeserializeObject<SsidResponse>(jsonResponse);

            return ssidResponse.Data.Attributes.SsidToken;
        }

        private async Task<TokensResponse> GetTokensAsync(string ssidToken)
        {
            var requestBody = new
            {
                ssid_token = ssidToken,
                client_id = _clientId,
                client_secret = _clientSecret,
                grant_type = "client_credential",
                check_ip = false
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://oauth2.epn.bz/token?v=2", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TokensResponse>(jsonResponse);
        }

        public async Task<TokensResponse> RefreshTokensAsync()
        {
            var token = await _dbContext.Tokens.FirstOrDefaultAsync();
            if (token == null)
            {
                throw new InvalidOperationException("Токен не найден в базе данных.");
            }

            var requestBody = new
            {
                grant_type = "refresh_token",
                refresh_token = token.RefreshToken,
                client_id = _clientId
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.epn.bz/token/refresh?v=2");
            request.Headers.Add("X-API-VERSION", "2");
            request.Content = content;
            var response = await _httpClient.SendAsync(request);
            //var response = await _httpClient.PostAsync("https://oauth2.epn.bz/token/refresh?v=2", content);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var newTokens = JsonConvert.DeserializeObject<TokensResponse>(jsonResponse);

                // Обновляем токены в базе данных
                token.AccessToken = newTokens.Data.Attributes.AccessToken;
                token.RefreshToken = newTokens.Data.Attributes.RefreshToken;
                token.ExpiresAt = DateTime.UtcNow.AddDays(1); // Пример: токен действует 1 день
                _dbContext.Tokens.Update(token);

                await _dbContext.SaveChangesAsync();
                return newTokens;
            }
            return null;
        }
    }

    public class SsidResponse()
    {
        [JsonProperty("data")]
        public Data Data { get; set; }
        [JsonProperty("result")]
        public string Result { get; set; }
        [JsonProperty("request")]
        public string Request {  get; set; }
    }

    public class Data
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("attributes")]
        public Attributes Attributes { get; set; }
    }

    public class Attributes
    {
        [JsonProperty("ssid_token")]
        public string SsidToken { get; set; }
    }
}
