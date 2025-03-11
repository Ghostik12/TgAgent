using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBotYandexMar.Models
{
    public class ChannekStat
    {
        public int Id { get; set; }
        public int PostedCount { get; set; } // Количество опубликованных постов
        public int FailedCount { get; set; } // Количество неудачных попыток постинга
        public DateTime LastUpdatedAt { get; set; } // Время последнего обновления статистики
        public int ChannelId { get; set; } // Внешний ключ для Channel
        public Channel Channel { get; set; } // Навигационное свойство
    }
}
