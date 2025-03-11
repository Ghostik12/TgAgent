using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBotYandexMar.Services
{
    public class PostingService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ChannelService _channelService;

        public PostingService(IServiceProvider serviceProvider, ChannelService channelService)
        {
            _serviceProvider = serviceProvider;
            _channelService = channelService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Постинг уже реализован в KeywordTimer, поэтому здесь ничего не делаем
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Останавливаем все таймеры
            _channelService.StopAllTimers();
            return Task.CompletedTask;
        }
    }
}
