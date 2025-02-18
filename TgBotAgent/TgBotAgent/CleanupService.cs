using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgBotAgent.DB;

namespace TgBotAgent
{
    public class CleanupService : BackgroundService
    {
        private readonly ILogger<CleanupService> _logger;
        private readonly ApplicationDbContext _dbContext;

        public CleanupService(ILogger<CleanupService> logger, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Сервис очистки запущен.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Удаляем сообщения старше 7 дней
                    var cutoffDate = DateTime.UtcNow.AddDays(-7);
                    var oldMessages = _dbContext.Messages
                        .Where(m => m.Timestamp < cutoffDate)
                        .ToList();

                    if (oldMessages.Any())
                    {
                        _dbContext.Messages.RemoveRange(oldMessages);
                        await _dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Удалено {oldMessages.Count} сообщений старше 7 дней.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при удалении старых сообщений.");
                }

                // Ожидаем 1 час перед следующей проверкой
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("Сервис очистки остановлен.");
        }
    }
}
