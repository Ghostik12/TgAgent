using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TgBotParserAli.DB;

namespace TgBotParserAli
{
    public class DailyResetService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DailyResetService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Вычисляем время до следующего обнуления (00:00 следующего дня)
                var now = DateTime.UtcNow;
                var nextMidnight = now.Date.AddDays(1);
                var delay = nextMidnight - now;

                // Ждем до следующего обнуления
                await Task.Delay(delay, stoppingToken);

                // Обнуляем PostedToday для всех каналов
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var channels = await dbContext.Channels.ToListAsync(stoppingToken);

                    foreach (var channel in channels)
                    {
                        channel.PostedToday = 0;
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    Console.WriteLine("Обнулено количество постов за день для всех каналов.");
                }
            }
        }
    }
}
