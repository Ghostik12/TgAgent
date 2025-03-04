using TgBotParserAli.DB;
using TgBotParserAli.Models;

namespace TgBotParserAli.Quartz
{
    public class Scheduler
    {
        private readonly Dictionary<int, Timer> _parseTimers = new Dictionary<int, Timer>();
        private readonly Dictionary<int, Timer> _postTimers = new Dictionary<int, Timer>();
        private ParseJob _parseJob;
        private PostJob _postJob;
        private readonly Dictionary<int, (Timer ParseTimer, Timer PostTimer)> _timers = new Dictionary<int, (Timer, Timer)>();
        private AppDbContext _appDbContext;

        public Scheduler(ParseJob parseJob, PostJob postJob, AppDbContext appDbContext)
        {
            _parseJob = parseJob;
            _postJob = postJob;
            _appDbContext = appDbContext;
        }

        // Запуск всех таймеров при старте приложения
        public void StartAllTimers()
        {
            var channels = _appDbContext.Channels.ToList();
            foreach (var channel in channels)
            {
                if (channel == null || !channel.IsActive)
                {
                    return; // Не выполняем задачи для неактивных каналов
                }
                ScheduleJobsForChannel(channel);
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

            // Создаем таймер для парсинга
            var parseTimer = new Timer(
                callback: async _ => await ParseChannelProducts(channel),
                state: null,
                dueTime: TimeSpan.Zero, // Запуск сразу после создания
                period: channel.ParseFrequency // Интервал между запусками
            );

            // Создаем таймер для постинга
            var postTimer = new Timer(
                callback: async _ => await PostChannelProducts(channel),
                state: null,
                dueTime: TimeSpan.Zero, // Запуск сразу после создания
                period: channel.PostFrequency // Интервал между запусками
            );

            // Сохраняем таймеры
            _timers[channel.Id] = (parseTimer, postTimer);
        }

        public void RemoveTimers(int channelId)
        {
            if (_timers.TryGetValue(channelId, out var timers))
            {
                timers.ParseTimer.Dispose(); // Останавливаем таймер парсинга
                timers.PostTimer.Dispose();  // Останавливаем таймер постинга
                _timers.Remove(channelId);   // Удаляем таймеры из словаря

                // Отменяем задачу постинга, если она выполняется
                _postJob.Cancel();
            }
        }

        // Колбэк для парсинга
        private async Task ParseChannelProducts(Channel channel)
        {
            if (channel != null)
            {
                Console.WriteLine($"Парсинг для канала {channel.Name} (ID: {channel.Id})");
                _parseJob.Execute(channel);
            }
        }

        // Колбэк для постинга
        private async Task PostChannelProducts(Channel channel)
        {
            if (channel != null)
            {
                Console.WriteLine($"Постинг для канала {channel.Name} (ID: {channel.Id})");
                _postJob.Execute(channel);
            }
        }
    }
}
