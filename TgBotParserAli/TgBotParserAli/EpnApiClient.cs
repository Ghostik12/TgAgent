﻿using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System.Text;
using TgBotParserAli.Models;

namespace TgBotParserAli
{
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
    }
}
