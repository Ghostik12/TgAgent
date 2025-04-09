using Microsoft.Extensions.Hosting;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using TgBotAgent.Controller;
using Telegram.Bot.Types;

namespace TgBotAgent.Models
{
    public class Bot : BackgroundService
    {
        private ITelegramBotClient _telegramClient;
        private TextMessageController _textMessageController;
        private VoiceMessageController _voiceMessageController;


        public Bot(ITelegramBotClient telegramClient, TextMessageController textMessageController, VoiceMessageController voiceMessageController)
        {
            _telegramClient = telegramClient;
            _voiceMessageController = voiceMessageController;
            _textMessageController = textMessageController;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _telegramClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions() { AllowedUpdates = { } }, cancellationToken: stoppingToken);

            Console.WriteLine("Бот запущен");
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
                    case MessageType.Voice:
                        _voiceMessageController.HandlerVois(update.Message.From.Id, update);
                        return;
                    case MessageType.Text:
                        if (update.Message.Text == "/start")
                        {
                            await _textMessageController.CheckUserOrAdmin(update);
                        }
                        else if (update.Message.Text.StartsWith("/admin"))
                        {
                            await _textMessageController.HandleAdminCommand(update.Message.Chat.Id, update.Message.Text);
                        }
                        else
                        {
                            await _textMessageController.HandleUserMessage(update.Message.Chat.Id, update.Message);
                        }
                        return;
                    case MessageType.Document:
                        _voiceMessageController.HandlerDocument(update.Message.From.Id, update);
                        return;
                    case MessageType.Photo:
                        _textMessageController.HandleUserMessage(update.Message.From.Id, update.Message);
                        return;
                    case MessageType.Video:
                        _voiceMessageController.HandlerVideo(update.Message.From.Id, update);
                        return;
                    case MessageType.Animation:
                        _voiceMessageController.HandlerAnimation(update.Message.From.Id, update);
                        return;
                    case MessageType.Audio:
                        _voiceMessageController.HandlerAudio(update.Message.From.Id, update);
                        return;
                    case MessageType.Contact:
                        _voiceMessageController.HandlerContact(update.Message.From.Id, update);
                        return;
                    case MessageType.Sticker:
                        _voiceMessageController.HandlerSticker(update.Message.From.Id, update);
                        return;
                    case MessageType.Location:
                        _voiceMessageController.HandlerLocation(update.Message.From.Id, update);
                        return;
                    case MessageType.VideoChatStarted:
                        _voiceMessageController.HandlerVideoNote(update.Message.From.Id, update);
                        return;
                    default:
                        //await _defaultMessage.Handle(update.Message, cancellationToken);
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
