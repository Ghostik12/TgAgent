using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgBotParserAli.Controllers;
using TgBotParserAli.Quartz;

namespace TgBotParserAli
{
    public class Bot : BackgroundService
    {
        private ITelegramBotClient _telegramClient;
        private TextMessageController _textMessageController;
        private Scheduler _scheduler;


        public Bot(ITelegramBotClient telegramClient, TextMessageController textMessageController, Scheduler scheduler)
        {
            _telegramClient = telegramClient;
            _textMessageController = textMessageController;
            _scheduler = scheduler;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _telegramClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions() { AllowedUpdates = { } }, cancellationToken: cancellationToken);

            Console.WriteLine("Бот запущен");

            // Запуск планировщика (восстановление таймеров)
            _scheduler.StartAllTimers();
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                await _textMessageController.BotClient_OnCallbackQuery(update.CallbackQuery);
                return;
            }

            if (update.Type == UpdateType.Message)
            {
                switch (update.Message!.Type)
                {
                    case MessageType.Text:
                        if (update.Message.Text == "/start" || update.Message.Text == "/menu")
                        {
                            await _textMessageController.CheckUserOrAdmin(update);
                        }
                        else if (update.Message.Text.StartsWith("/admin"))
                        {
                            //await _textMessageController.HandleAdminCommand(update.Message.Chat.Id, update.Message.Text);
                        }
                        else
                        {
                            await _textMessageController.Handle(update, cancellationToken);
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException}]\n{apiRequestException}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);

            Console.WriteLine("Ожидаем 10 секунд перед повторным подключением");
            Thread.Sleep(10000);

            return Task.CompletedTask;
        }
    }
}
