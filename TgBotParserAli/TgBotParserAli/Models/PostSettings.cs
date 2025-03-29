using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBotParserAli.Models
{
    public class PostSettings
    {
        public int Id { get; set; }
        public int ChannelId { get; set; } // Связь с каналом
        public string TitleTemplate { get; set; } // Шаблон для названия
        public string PriceTemplate { get; set; } // Шаблон для цены
        public string CaptionTemplate { get; set; } // Шаблон для подписи
        public string UrlTemplate { get; set; } // Шаблон для ссылки
        public string Order { get; set; } // Порядок шаблонов (например, "Price,Title,Caption")
        public Channel Channel { get; set; } // Навигационное свойство
    }
}
