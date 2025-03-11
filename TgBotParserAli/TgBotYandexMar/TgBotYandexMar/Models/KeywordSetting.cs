namespace TgBotYandexMar.Models
{
    public class KeywordSetting
    {
        public int Id { get; set; }
        public string Keyword { get; set; }
        public TimeSpan ParseFrequency { get; set; }
        public TimeSpan PostFrequency { get; set; }
        public int ChannelId { get; set; }
        public Channel Channel { get; set; }
    }
}