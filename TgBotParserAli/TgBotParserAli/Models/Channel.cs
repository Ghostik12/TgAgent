

namespace TgBotParserAli.Models
{
    public class Channel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ChatId { get; set; }
        public int ParseCount { get; set; } // Количество товаров для парсинга за раз
        public int MaxPostsPerDay { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string ReferralLink { get; set; }
        public string Keywords { get; set; } // Ключевые слова для парсинга
        public bool IsActive { get; set; }
        public int ParsedCount { get; set; }
        public int PostedToday { get; set; }
        public int FailedPosts { get; set; }
        public List<Product> Products { get; set; } = new List<Product>(); // Список товаров
                                                                           // Связь с KeywordStat
        public List<KeywordStat> KeywordStats { get; set; } = new List<KeywordStat>();
        public List<KeywordSetting> KeywordSettings { get; set; } = new List<KeywordSetting>();
    }
}
