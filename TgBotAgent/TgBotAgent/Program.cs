using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using Telegram.Bot;
using TgBotAgent.Controller;
using TgBotAgent.DB;
using TgBotAgent.Models;
using Topshelf;

namespace TgBotAgent
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Загружаем конфигурацию из appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("jsconfig.json", optional: false, reloadOnChange: true)
                .Build();

            HostFactory.Run(x =>
            {
                x.Service<TelegramBotService>(s =>
                {
                    s.ConstructUsing(name => new TelegramBotService(configuration));
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem(); // Запуск от имени системы
                x.SetDescription("Telegram Bot Service"); // Описание службы
                x.SetDisplayName("TelegramBotService");   // Отображаемое имя
                x.SetServiceName("TelegramBotService");  // Имя службы
                x.StartAutomatically(); // Автоматический запуск при старте системы
            });
        }
    }

    public class TelegramBotService
    {
        private IHost _host;
        private readonly IConfiguration _configuration;

        public TelegramBotService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Start()
        {
            Console.WriteLine("Запуск службы...");

            // Создаем и запускаем хост
            _host = new HostBuilder()
                .ConfigureServices((hostContext, services) => ConfigureServices(services, _configuration))
                .UseConsoleLifetime()
                .Build();

            _host.StartAsync().Wait(); // Запускаем хост синхронно
            Console.WriteLine("Служба успешно запущена.");
        }

        public void Stop()
        {
            Console.WriteLine("Остановка службы...");
            _host.StopAsync().Wait(); // Останавливаем хост синхронно
            Console.WriteLine("Служба остановлена.");
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);
            services.AddScoped<ITelegramBotClient>(provider =>
                new TelegramBotClient(configuration["BotConfiguration:BotToken"]));
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddHostedService<Bot>();
            services.AddHostedService<CleanupService>();

            services.AddScoped<TextMessageController>();
            services.AddScoped<VoiceMessageController>();
        }
    }
}
