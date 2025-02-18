using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using Telegram.Bot;
using TgBotAgent.Controller;
using TgBotAgent.DB;
using TgBotAgent.Models;

namespace TgBotAgent
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

            Console.WriteLine("Servives launch");

            await host.RunAsync();
            Console.WriteLine("Services stop");
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ITelegramBotClient>(provide => new TelegramBotClient(""));
            services.AddHostedService<Bot>();
            services.AddHostedService<CleanupService>();
            services.AddDbContext<ApplicationDbContext>();

            services.AddScoped<TextMessageController>();
        }
    }
}
