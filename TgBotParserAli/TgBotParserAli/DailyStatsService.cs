using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Telegram.Bot;
using TgBotParserAli.DB;

namespace TgBotParserAli
{
    public class DailyStatsService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;

        public DailyStatsService(ITelegramBotClient botClient, IServiceScopeFactory scopeFactory)
        {
            _botClient = botClient;
            _scopeFactory = scopeFactory;
        }

        public void Start()
        {
            // Запускаем таймер на отправку статистики каждый день в 00:00
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1); // Следующий запуск в полночь
            var delay = nextRun - now;

            _timer = new Timer(SendDailyStats, null, delay, TimeSpan.FromDays(1));
        }

        private async void SendDailyStats(object state)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var channels = await dbContext.Channels
                    .Include(c => c.KeywordStats)
                    .ToListAsync();

                var statsMessage = new StringBuilder();

                foreach (var channel in channels)
                {
                    statsMessage.AppendLine($"Спарсено товара {channel.Name}:");

                    foreach (var keywordStat in channel.KeywordStats)
                    {
                        statsMessage.AppendLine($"- {keywordStat.Keyword}: {keywordStat.Count}");
                    }

                    statsMessage.AppendLine();
                }

                // Отправляем статистику всем админам
                var admins = await dbContext.Admin.ToListAsync();
                foreach (var admin in admins)
                {
                    await _botClient.SendTextMessageAsync(admin.ChatId, statsMessage.ToString());
                }

                // Сбрасываем счетчики
                foreach (var channel in channels)
                {
                    foreach (var keywordStat in channel.KeywordStats)
                    {
                        keywordStat.Count = 0;
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, 0);
        }
    }
}
