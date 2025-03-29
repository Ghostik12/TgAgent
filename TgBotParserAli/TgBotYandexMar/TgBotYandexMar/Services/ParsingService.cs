using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TgBotYandexMar.DB;

namespace TgBotYandexMar.Services
{
    public class ParsingService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ChannelService _channelService;
        private Timer _dailyResetTimer;

        public ParsingService(IServiceProvider serviceProvider, ChannelService channelService)
        {
            _serviceProvider = serviceProvider;
            _channelService = channelService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Запускаем парсинг для всех активных каналов
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var channels = dbContext.Channels
                    .Include(c => c.KeywordSettings)
                    .Where(c => c.IsActive)
                    .ToList();

                foreach (var channel in channels)
                {
                    foreach (var keywordSetting in channel.KeywordSettings)
                    {
                        _channelService.StartKeywordTimer(
                            keywordSetting.Keyword,
                            keywordSetting.ParseFrequency,
                            keywordSetting.PostFrequency
                        );
                    }
                }
            }

            // Запускаем таймер для сброса статистики каждый день
            _dailyResetTimer = new Timer(async _ => await ResetDailyStats(), null, TimeSpan.Zero, TimeSpan.FromDays(1));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Останавливаем все таймеры
            _channelService.StopAllTimers();
            return Task.CompletedTask;
        }

        private async Task ResetDailyStats()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Находим все записи статистики, которые нужно сбросить
                var statsToReset = await dbContext.KeywordStats
                    //.Where(ks => ks.LastParsedAt < DateTime.UtcNow.Date)
                    .ToListAsync();

                foreach (var stat in statsToReset)
                {
                    stat.ParsedCount = 0;
                    stat.LastParsedAt = DateTime.UtcNow;
                    dbContext.KeywordStats.Update(stat);
                }

                // Сбрасываем счетчик постинга для всех каналов
                var channelStatsToReset = await dbContext.ChannelStats
                    //.Where(cs => cs.LastUpdatedAt < DateTime.UtcNow.Date)
                    .ToListAsync();

                foreach (var channelStat in channelStatsToReset)
                {
                    channelStat.PostedCount = 0; // Сбрасываем счетчик постинга
                    channelStat.LastUpdatedAt = DateTime.UtcNow;
                    dbContext.ChannelStats.Update(channelStat);
                }
                await dbContext.SaveChangesAsync();

                // Перезапускаем таймеры парсинга
                _channelService.RestartKeywordTimers();
            }
        }
    }
}
