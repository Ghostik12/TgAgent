using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TgBotParserAli.DB;
using TgBotParserAli.Quartz;

namespace TgBotParserAli
{
    public class ResetPostedTodayTask : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly TimeSpan _resetInterval; // Интервал сброса
        private DateTime _lastResetTime; // Время последнего сброса

        public ResetPostedTodayTask(IServiceProvider services, TimeSpan resetInterval)
        {
            _services = services;
            _resetInterval = resetInterval;
            _lastResetTime = DateTime.UtcNow; // Инициализируем временем запуска
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Сбрасываем счетчик опубликованных постов в полночь
                    if (DateTime.UtcNow - _lastResetTime >= _resetInterval)
                    {
                        var channels = await dbContext.Channels.ToListAsync();
                        foreach (var channel in channels)
                        {
                            channel.PostedToday = 0;
                            dbContext.Channels.Update(channel);

                            // Перезапускаем таймеры парсинга для канала
                            var scheduler = scope.ServiceProvider.GetRequiredService<Scheduler>();
                            scheduler.ScheduleJobsForChannel(channel); // Перезапуск таймеров
                        }

                        await dbContext.SaveChangesAsync();
                        _lastResetTime = DateTime.UtcNow; // Обновляем время последнего сброса
                        Console.WriteLine($"Счетчик опубликованных постов сброшен для всех каналов. Следующий сброс через {_resetInterval.TotalHours} часов.");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Проверяем каждую минуту
            }
        }
    }
}
