
namespace TgBotYandexMar.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string MinPrice { get; set; } // Минимальная цена
        public string MaxPrice { get; set; } // Максимальная цена
        public string AvgPrice { get; set; } // Средняя цена
        public string Url { get; set; }
        public double Rating { get; set; }
        public int OpinionCount { get; set; }
        public bool IsPosted { get; set; }
        public DateTime? PostedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int ChannelId { get; set; }
        public Channel Channel { get; set; }
        public string Keyword { get; set; }
        public List<string> Photos { get; internal set; }
    }
}