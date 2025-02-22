

namespace TgBotAgent.Models
{
    public class MessageRecord
    {
        public int Id { get; set; }
        public long FromUserId { get; set; }
        public string FromUsername { get; set; }
        public long ToUserId { get; set; }
        public string ToUsername { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
