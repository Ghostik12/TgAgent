

namespace TgBotParserAli.Models
{
    public class Channel
    {
        public int Id { get; set; }
        public string Name { get; set; } // Название канала
        public string ChatId { get; set; } // ID канала
        public string Keywords { get; set; } // Ключевые слова для парсинга
        public TimeSpan ParseFrequency { get; set; } // Частота парсинга (например, "6 hours")
        public int ParseCount { get; set; } // Количество товаров для парсинга
        public TimeSpan PostFrequency { get; set; } // Частота постинга (например, "2 hours")
        public int MaxPostsPerDay { get; set; } // Максимальное количество постов в день
        public decimal MinPrice { get; set; } // Минимальная цена товара
        public decimal MaxPrice { get; set; } // Максимальная цена товара
        public string ReferralLink { get; set; } // Реферальная ссылка
        public bool IsActive { get; set; } = true; // По умолчанию канал активен
        public int ParsedCount { get; set; } = 0; // Новое поле
        public List<Product> Products { get; set; } = new List<Product>(); // Список товаров
    }
}
