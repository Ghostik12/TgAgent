﻿using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient("8179155928:AAHDnmyz0F_p3PTV5dqbtJKRGj4HiZeXcOI"));

            // Регистрируем основной сервис бота и фоновые задачи
            services.AddHostedService<Bot>();
            services.AddHostedService<DailyResetService>();

            // Регистрируем контекст базы данных
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql("Host=localhost;Database=epnbot1;Username=postgres;Password=12345Ob@"));
            services.AddScoped<TextMessageController>();
            services.AddScoped<ParseJob>();
            services.AddScoped<PostJob>();

            // Регистрируем ePN API клиент
            services.AddSingleton<EpnApiClient>(provider => new EpnApiClient("e1edb8f19acccfc7a70d1541d8fba30f", "s5tk7whzmfsqn4k70am2iov1z1nfqi9s"));

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
