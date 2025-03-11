namespace TgBotYandexMar.Models
{
    public class Channel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ChatId { get; set; }
        public int ParseCount { get; set; }
        public int MaxPostsPerDay { get; set; }
        public string ApiKey { get; set; }
        public string Clid { get; set; } // Новое поле
        public string ClientId { get; set; } // Client ID от приложения
        public string ClientSecret { get; set; } // Client Secret от приложения
        public string RedirectUrl { get; set; } // Redirect URL от приложения
        public string OAuthToken { get; set; } // OAuth-токен
        public DateTime? OAuthTokenExpiresAt { get; set; } // Время истечения токена
        public bool UseExactMatch { get; set; }
        public bool UseLowPrice { get; set; }
        public bool ShowRating { get; set; }
        public bool ShowOpinionCount { get; set; }
        public List<KeywordSetting> KeywordSettings { get; set; } = new();
        public PostSettings PostSettings { get; set; }
        public bool IsActive { get; set; } = true;
        public string PriceType { get; set; } = "avg"; // Тип цены (min, max, avg)
    }
}