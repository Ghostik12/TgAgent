using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Telegram.Bot;
using TgBotParserAli.DB;
using TgBotParserAli.Models;

namespace TgBotParserAli.Quartz
{
    public class Scheduler
    {
        private readonly Dictionary<int, Timer> _parseTimers = new Dictionary<int, Timer>();
        private readonly Dictionary<int, Timer> _postTimers = new Dictionary<int, Timer>();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EpnApiClient _epnApiClient;
        private readonly VkLinkShortener _vkLinkShortener;
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;

        // Очереди для задач парсинга и постинга
        private ConcurrentQueue<(Channel channel, KeywordSetting keywordSetting)> _parseQueue = new ConcurrentQueue<(Channel, KeywordSetting)>();
        private ConcurrentQueue<(Channel channel, KeywordSetting keywordSetting)> _postQueue = new ConcurrentQueue<(Channel, KeywordSetting)>();

        public Scheduler(IServiceScopeFactory scopeFactory, EpnApiClient epnApiClient, VkLinkShortener vkLinkShortener, DbContextOptions<AppDbContext> dbContextOptions)
        {
            _scopeFactory = scopeFactory;
            _epnApiClient = epnApiClient;
            _vkLinkShortener = vkLinkShortener;
            _dbContextOptions = dbContextOptions;
        }

        public void StartAllTimers()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var channels = dbContext.Channels.ToList();
                foreach (var channel in channels)
                {
                    if (channel == null || !channel.IsActive)
                    {
                        continue; // Пропускаем неактивные каналы
                    }
                    ScheduleJobsForChannel(channel);
                }
            }
        }

        public void ScheduleJobsForChannel(Channel channel)
        {
            if (channel == null || !channel.IsActive)
            {
                return; // Не выполняем задачи для неактивных каналов
            }

            // Останавливаем старые таймеры, если они есть
            RemoveTimers(channel.Id);
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var keywordSettings = dbContext.KeywordSettings
                    .Where(k => k.ChannelId == channel.Id)
                    .ToList();
                // Создаем таймеры для каждого ключевого слова
                foreach (var keywordSetting in keywordSettings)
                {
                    // Таймер для парсинга
                    var parseTimer = new Timer(
                        callback: async _ => await ParseChannelProducts(channel, keywordSetting),
                        state: null,
                        dueTime: TimeSpan.Zero, // Запуск сразу после создания
                        period: keywordSetting.ParseFrequency // Интервал между запусками
                    );

                    // Таймер для постинга
                    var postTimer = new Timer(
                        callback: async _ => await PostChannelProducts(channel, keywordSetting),
                        state: null,
                        dueTime: TimeSpan.Zero, // Запуск сразу после создания
                        period: keywordSetting.PostFrequency // Интервал между запусками
                    );

                    // Сохраняем таймеры
                    _parseTimers[keywordSetting.Id] = parseTimer;
                    _postTimers[keywordSetting.Id] = postTimer;
                }
            }
        }

        private async Task ParseChannelProducts(Channel channel, KeywordSetting keywordSetting)
        {
            if (keywordSetting.IsParsing)
            {
                // Если парсинг уже выполняется, добавляем задачу в очередь
                _parseQueue.Enqueue((channel, keywordSetting));
                return;
            }

            keywordSetting.IsParsing = true;

            // Задержка перед выполнением парсинга
            await Task.Delay(keywordSetting.ParseFrequency);

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var parseJob = new ParseJob(dbContext, _epnApiClient, _dbContextOptions);

                if (channel != null)
                {
                    Console.WriteLine($"Парсинг для канала {channel.Name} (ключевое слово: {keywordSetting.Keyword})");
                    await parseJob.Execute(channel, keywordSetting);
                }
            }

            keywordSetting.IsParsing = false;

            // Проверяем очередь и запускаем следующую задачу, если есть
            if (_parseQueue.TryDequeue(out var nextParseTask))
            {
                await ParseChannelProducts(nextParseTask.channel, nextParseTask.keywordSetting);
            }
        }

        private async Task PostChannelProducts(Channel channel, KeywordSetting keywordSetting)
        {
            if (keywordSetting.IsPosting)
            {
                // Если постинг уже выполняется, добавляем задачу в очередь
                _postQueue.Enqueue((channel, keywordSetting));
                return;
            }

            keywordSetting.IsPosting = true;

            // Задержка перед выполнением постинга
            await Task.Delay(keywordSetting.PostFrequency);

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var postJob = new PostJob(dbContext, botClient, _vkLinkShortener, _dbContextOptions);

                if (channel != null)
                {
                    Console.WriteLine($"Постинг для канала {channel.Name} (ключевое слово: {keywordSetting.Keyword})");
                    await postJob.Execute(channel, keywordSetting);
                }
            }

            keywordSetting.IsPosting = false;

            // Проверяем очередь и запускаем следующую задачу, если есть
            if (_postQueue.TryDequeue(out var nextPostTask))
            {
                await PostChannelProducts(nextPostTask.channel, nextPostTask.keywordSetting);
            }
        }

        public void UpdateTimersForKeyword(KeywordSetting keywordSetting)
        {
            // Останавливаем старые таймеры для этого ключевого слова
            if (_parseTimers.TryGetValue(keywordSetting.Id, out var parseTimer))
            {
                parseTimer.Dispose();
                _parseTimers.Remove(keywordSetting.Id);
            }

            if (_postTimers.TryGetValue(keywordSetting.Id, out var postTimer))
            {
                postTimer.Dispose();
                _postTimers.Remove(keywordSetting.Id);
            }

            // Создаем новые таймеры с обновленными настройками
            var parseTimerNew = new Timer(
                callback: async _ => await ParseChannelProducts(keywordSetting.Channel, keywordSetting),
                state: null,
                dueTime: TimeSpan.Zero,
                period: keywordSetting.ParseFrequency
            );

            var postTimerNew = new Timer(
                callback: async _ => await PostChannelProducts(keywordSetting.Channel, keywordSetting),
                state: null,
                dueTime: TimeSpan.Zero,
                period: keywordSetting.PostFrequency
            );

            // Сохраняем новые таймеры
            _parseTimers[keywordSetting.Id] = parseTimerNew;
            _postTimers[keywordSetting.Id] = postTimerNew;
        }

        public void RemoveTimers(int channelId)
        {
            Console.WriteLine($"Останавливаем таймеры и задачи для канала {channelId}...");

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Получаем все KeywordSetting для канала
                var keywordSettings = dbContext.KeywordSettings
                    .Where(k => k.ChannelId == channelId)
                    .ToList();

                // Останавливаем таймеры для каждого KeywordSetting
                foreach (var keywordSetting in keywordSettings)
                {
                    if (_parseTimers.TryGetValue(keywordSetting.Id, out var parseTimer))
                    {
                        parseTimer.Dispose();
                        _parseTimers.Remove(keywordSetting.Id);
                        Console.WriteLine($"Таймер парсинга для ключевого слова {keywordSetting.Keyword} остановлен.");
                    }

                    if (_postTimers.TryGetValue(keywordSetting.Id, out var postTimer))
                    {
                        postTimer.Dispose();
                        _postTimers.Remove(keywordSetting.Id);
                        Console.WriteLine($"Таймер постинга для ключевого слова {keywordSetting.Keyword} остановлен.");
                    }
                }
            }

            // Удаляем задачи из очередей
            _parseQueue = new ConcurrentQueue<(Channel channel, KeywordSetting keywordSetting)>(_parseQueue.Where(x => x.channel.Id != channelId));
            _postQueue = new ConcurrentQueue<(Channel channel, KeywordSetting keywordSetting)>(_postQueue.Where(x => x.channel.Id != channelId));
            Console.WriteLine($"Задачи для канала {channelId} удалены из очередей.");
        }
    }
}
