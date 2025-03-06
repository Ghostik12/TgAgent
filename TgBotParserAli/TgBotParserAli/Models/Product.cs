

namespace TgBotParserAli.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductId { get; set; } // ID товара на AliExpress
        public string Name { get; set; } // Название товара
        public decimal Price { get; set; } // Цена
        public decimal DiscountedPrice { get; set; } // Цена по скидке
        public List<string> Images { get; set; } = new List<string>(); // Список фото
        public bool IsPosted { get; set; } = false; // Опубликован ли товар
        public int ChannelId { get; set; } // Внешний ключ на канал
        public Channel Channel { get; set; } // Навигационное свойство
        public string Url { get; set; } // Ссылка на товар
        public string Keyword { get; set; } // Ключевое слово
    }
}
