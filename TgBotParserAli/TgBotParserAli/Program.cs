using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TgBotParserAli.DB;
using Microsoft.Extensions.Hosting;
using System.Text;
using TgBotParserAli.Controllers;
using TgBotParserAli.Quartz;
using Microsoft.EntityFrameworkCore;

namespace TgBotParserAli
{
    public class Program
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
            services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient("7935930217:AAF_KST07z_gL35RnoNoSdC3lA6H366ST8s"));

            // Регистрируем основной сервис бота и фоновые задачи
            services.AddHostedService<Bot>();
            services.AddHostedService<ResetPostedTodayTask>(provider =>
                new ResetPostedTodayTask(provider, TimeSpan.FromHours(24))); // Интервал сброса - 24 часа);

            // Регистрируем контекст базы данных
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql("Host=localhost;Database=epnbot1;Username=postgres;Password=12345Ob@"));

            // Регистрируем TokenService
            services.AddHttpClient<TokenService>();
            services.AddScoped<TokenService>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var dbContext = provider.GetRequiredService<AppDbContext>();
                return new TokenService(httpClientFactory.CreateClient(), dbContext, "5CBhjY0PmwqG4Esc9LylHONvSJuUT7ig", "cokH4jtYXzvFsg28PGh3pbM7nOLVrCw5");
            });


            services.AddScoped<TextMessageController>();
            services.AddScoped<ParseJob>();
            services.AddScoped<PostJob>();

            // Регистрируем ePN API клиент
            services.AddSingleton<EpnApiClient>(provider => new EpnApiClient("e1edb8f19acccfc7a70d1541d8fba30f", "s5tk7whzmfsqn4k70am2iov1z1nfqi9s", "Q6gKDui1Ft5IhHYkcCs3mWGjpbVEoAP9"));

            // Регистрируем VkLinkShortener
            services.AddSingleton<VkLinkShortener>(provider =>
            {
                var accessToken = "vk1.a.ACDgotT08hvuvzNYKa03FeU25LfA20vYpUliKlxNmyCASqsc0Zg337gGczBSg3O2CM2UjrUci72z5y9n638Gx79SzqQk9XD5OhwXeMFTnAFK7dy57HunRPBqe-qMdogj_jqbriGZdvRS32UwPUa2Lm-cACBrqVDNVfEEFyuKy_0-Ee_6m9bfPvasimweGVfXv85DWiwkTgKAmxODKfKLnQ"; // Замени токен VK
                return new VkLinkShortener(accessToken);
            });

            services.AddSingleton<DailyStatsService>();
            services.AddSingleton<Scheduler>();
        }
    }
}
