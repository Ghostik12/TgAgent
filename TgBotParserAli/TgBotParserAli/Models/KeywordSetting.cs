using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBotParserAli.Models
{
    public class KeywordSetting
    {
        public int Id { get; set; }
        public int ChannelId { get; set; } // Связь с каналом
        public Channel Channel { get; set; }
        public string Keyword { get; set; } // Ключевое слово
        public TimeSpan ParseFrequency { get; set; } // Частота парсинга
        public TimeSpan PostFrequency { get; set; } // Частота постинга
        public bool IsParsing { get; set; } = false; // Флаг для парсинга
        public bool IsPosting { get; set; } = false; // Флаг для постинга
    }
}
