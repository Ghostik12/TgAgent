namespace TgBotYandexMar.Models
{
    public class KeywordStat
    {
        public int Id { get; set; }
        public int ParsedCount { get; set; } // Количество спарсенных товаров
        public DateTime LastParsedAt { get; set; } // Время последнего парсинга
        public int KeywordSettingId { get; set; } // Внешний ключ для KeywordSetting
        public KeywordSetting KeywordSetting { get; set; } // Навигационное свойство
    }
}