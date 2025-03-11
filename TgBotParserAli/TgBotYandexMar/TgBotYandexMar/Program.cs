using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using TgBotYandexMar.Controller;
using TgBotYandexMar.DB;
using TgBotYandexMar.Services;
using TgBotYandexMar.Timers;

namespace TgBotYandexMar
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;

            var host = new HostBuilder()
                .ConfigureServices((hostContext, services) => ConfigureServices(services))
                .UseConsoleLifetime()
                .Build();

            Console.WriteLine("Services launch");

            await host.RunAsync();
            Console.WriteLine("Services stop");
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            // Регистрируем Telegram-бота
            services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient("8179155928:AAHDnmyz0F_p3PTV5dqbtJKRGj4HiZeXcOI"));

            // Регистрируем основной сервис бота и фоновые задачи
            services.AddHostedService<Bot>();

            // Регистрируем контекст базы данных
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql("Host=localhost;Database=yanbot;Username=postgres;Password=12345Ob@"));
            services.AddScoped<TextMessageController>();
            services.AddScoped<KeywordTimer>();

            services.AddHttpClient<YandexMarketApiService>();
            services.AddSingleton<ChannelService>();
            services.AddHostedService<ParsingService>();
            services.AddHostedService<PostingService>();

            //services.AddSingleton<DailyStatsService>();
        }
    }
}
