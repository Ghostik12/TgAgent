using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TgBotParserAli.DB;

namespace TgBotParserAli
{
    public class ResetPostedTodayTask : BackgroundService
    {
        private readonly IServiceProvider _services;

        public ResetPostedTodayTask(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Сбрасываем счетчик опубликованных постов в полночь
                    if (DateTime.UtcNow.Hour == 0 && DateTime.UtcNow.Minute == 0)
                    {
                        var channels = await dbContext.Channels.ToListAsync();
                        foreach (var channel in channels)
                        {
                            channel.PostedToday = 0;
                            dbContext.Channels.Update(channel);
                        }

                        await dbContext.SaveChangesAsync();
                        Console.WriteLine("Счетчик опубликованных постов сброшен для всех каналов.");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Проверяем каждую минуту
            }
        }
    }
}
