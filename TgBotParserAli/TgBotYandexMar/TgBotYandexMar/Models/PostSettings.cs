namespace TgBotYandexMar.Models
{
    public class PostSettings
    {
        public int Id { get; set; }
        public string PriceTemplate { get; set; }
        public string TitleTemplate { get; set; }
        public string CaptionTemplate { get; set; }
        public string Order { get; set; }
        public bool ShowRating { get; set; } // Показывать рейтинг
        public bool ShowOpinionCount { get; set; } // Показывать количество отзывов
        public int ChannelId { get; set; }
        public Channel Channel { get; set; }
    }
}