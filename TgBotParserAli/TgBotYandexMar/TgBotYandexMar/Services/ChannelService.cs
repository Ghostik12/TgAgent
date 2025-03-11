using Microsoft.Extensions.DependencyInjection;
using TgBotYandexMar.DB;
using TgBotYandexMar.Models;
using TgBotYandexMar.Timers;

namespace TgBotYandexMar.Services
{
    public class ChannelService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, KeywordTimer> _keywordTimers = new();

        public ChannelService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task AddChannel(Channel channel)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                //dbContext.Channels.Add(channel);
                //await dbContext.SaveChangesAsync();

                // Запускаем таймеры для каждого ключевого слова
                foreach (var keywordSetting in channel.KeywordSettings)
                {
                    StartKeywordTimer(keywordSetting.Keyword, keywordSetting.ParseFrequency, keywordSetting.PostFrequency);
                }
            }
        }

        public void StartKeywordTimer(string keyword, TimeSpan parseFrequency, TimeSpan postFrequency)
        {
            if (_keywordTimers.ContainsKey(keyword))
            {
                // Останавливаем существующий таймер
                _keywordTimers[keyword].Stop();
            }

            // Создаем новый таймер
            var keywordTimer = new KeywordTimer(_serviceProvider, keyword, parseFrequency, postFrequency);
            _keywordTimers[keyword] = keywordTimer;
        }

        public void StopKeywordTimer(string keyword)
        {
            if (_keywordTimers.ContainsKey(keyword))
            {
                _keywordTimers[keyword].Stop();
                _keywordTimers.Remove(keyword);
            }
        }

        // Новый метод для остановки всех таймеров
        public void StopAllTimers()
        {
            foreach (var timer in _keywordTimers.Values)
            {
                timer.Stop();
            }
            _keywordTimers.Clear();
        }

        public void RestartKeywordTimers()
        {
            foreach (var timer in _keywordTimers.Values)
            {
                timer.Restart();
            }
        }
    }
}
