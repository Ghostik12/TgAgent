using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBotParserAli.Models
{
    public class KeywordStat
    {
        public int Id { get; set; }
        public int ChannelId { get; set; } // Связь с каналом
        public Channel Channel { get; set; }
        public string Keyword { get; set; } // Ключевое слово
        public int Count { get; set; } // Количество товаров по этому слову
        public DateTime LastUpdated { get; set; } // Дата последнего обновления
    }
}
