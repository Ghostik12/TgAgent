using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using TgBotYandexMar.DB;
using TgBotYandexMar.Models;
using Microsoft.EntityFrameworkCore;
using TgBotYandexMar.Services;

namespace TgBotYandexMar.Timers
{
    public class KeywordTimer
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer _parseTimer;
        private Timer _postTimer;

        public KeywordTimer(IServiceProvider serviceProvider, string keyword, TimeSpan parseFrequency, TimeSpan postFrequency)
        {
            _serviceProvider = serviceProvider;
            Keyword = keyword;
            ParseFrequency = parseFrequency;
            PostFrequency = postFrequency;

            // Запускаем таймеры
            _parseTimer = new Timer(ParseCallback, null, TimeSpan.Zero, parseFrequency);
            _postTimer = new Timer(PostCallback, null, TimeSpan.Zero, postFrequency);
        }

        public string Keyword { get; }
        public TimeSpan ParseFrequency { get; }
        public TimeSpan PostFrequency { get; }

        private async void ParseCallback(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var yandexMarketApiService = scope.ServiceProvider.GetRequiredService<YandexMarketApiService>();

                // Получаем канал и настройки для этого ключевого слова
                var channel = await dbContext.Channels
                    .Include(c => c.KeywordSettings)
                    .FirstOrDefaultAsync(c => c.KeywordSettings.Any(k => k.Keyword == Keyword));

                if (channel == null)
                {
                    return;
                }

                // Получаем статистику для текущего ключевого слова
                var keywordSetting = channel.KeywordSettings.FirstOrDefault(k => k.Keyword == Keyword);
                if (keywordSetting == null)
                {
                    return;
                }

                var keywordStat = await dbContext.KeywordStats
                    .FirstOrDefaultAsync(ks => ks.KeywordSettingId == keywordSetting.Id);

                if (keywordStat == null)
                {
                    keywordStat = new KeywordStat
                    {
                        KeywordSettingId = keywordSetting.Id,
                        ParsedCount = 0,
                        LastParsedAt = DateTime.UtcNow
                    };
                    dbContext.KeywordStats.Add(keywordStat);
                }

                // Проверяем, достигнуто ли максимальное количество постов в день
                if (keywordStat.ParsedCount >= channel.MaxPostsPerDay)
                {
                    // Останавливаем только таймер парсинга
                    _parseTimer?.Change(Timeout.Infinite, 0);
                    return;
                }

                // Парсим товары
                var products = await yandexMarketApiService.SearchProductsAsync(
                    Keyword,
                    channel.ApiKey,
                    exactMatch: channel.UseExactMatch
                );

                // Сохраняем товары в базу данных
                foreach (var product in products)
                {
                    if (keywordStat.ParsedCount >= channel.MaxPostsPerDay)
                    {
                        break; // Прекращаем добавление, если достигнут лимит
                    }

                    dbContext.Products.Add(new Product
                    {
                        Name = product.Name,
                        MinPrice = product.MinPrice, // Сохраняем минимальную цену
                        MaxPrice = product.MaxPrice, // Сохраняем максимальную цену
                        AvgPrice = product.AvgPrice, // Сохраняем среднюю цену
                        Url = product.Url,
                        Rating = product.Rating,
                        OpinionCount = product.OpinionCount,
                        Photos = product.Photos,
                        ChannelId = channel.Id,
                        Keyword = Keyword,
                        IsPosted = false
                    });

                    keywordStat.ParsedCount++;
                }

                keywordStat.LastParsedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }

        private async void PostCallback(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var yandexMarketApiService = scope.ServiceProvider.GetRequiredService<YandexMarketApiService>();
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

                // Получаем канал и настройки для этого ключевого слова
                var channel = await dbContext.Channels
                    .Include(c => c.KeywordSettings)
                    .Include(c => c.PostSettings)
                    .FirstOrDefaultAsync(c => c.KeywordSettings.Any(k => k.Keyword == Keyword));

                if (channel == null)
                {
                    return;
                }
                // Проверяем, есть ли OAuth-токен
                if (string.IsNullOrEmpty(channel.OAuthToken) || channel.OAuthToken == "пусто")
                {
                    await botClient.SendTextMessageAsync(channel.ChatId, "OAuth-токен отсутствует. Пожалуйста, авторизуйтесь.");
                    return;
                }

                // Получаем статистику для текущего канала
                var channelStat = await dbContext.ChannelStats
                    .FirstOrDefaultAsync(cs => cs.ChannelId == channel.Id);

                if (channelStat == null)
                {
                    channelStat = new ChannekStat
                    {
                        ChannelId = channel.Id,
                        PostedCount = 0,
                        FailedCount = 0,
                        LastUpdatedAt = DateTime.UtcNow
                    };
                    dbContext.ChannelStats.Add(channelStat);
                }

                // Получаем неопубликованные товары
                var products = await dbContext.Products
                    .Where(p => p.ChannelId == channel.Id && p.Keyword == Keyword && !p.IsPosted)
                    .Take(channel.MaxPostsPerDay - channelStat.PostedCount) // Учитываем лимит постов
                    .ToListAsync();

                foreach (var product in products)
                {
                    try
                    {
                        // Собираем пост
                        var (postText, mediaUrls) = BuildPost(product, channel.PostSettings);

                        // Получаем токен erid
                        var eridToken = await yandexMarketApiService.GetEridTokenAsync(product.Url, channel.ApiKey, channel.Clid, postText, mediaUrls, product);

                        // Создаем партнерскую ссылку
                        var partnerUrl = await yandexMarketApiService.CreatePartnerLinkAsync(product.Url, channel, eridToken);

                        // Формируем текст сообщения
                        var postMessage = BuildPostMessage(product, channel.PostSettings, partnerUrl, channel.PriceType);

                        // Отправляем изображение с текстом
                        if (product.Photos != null && product.Photos.Any())
                        {
                            // Используем первое изображение из списка
                            var photoUrl = product.Photos.First();

                            await botClient.SendPhotoAsync(
                                chatId: channel.ChatId,
                                photo: photoUrl,
                                caption: postMessage,
                                parseMode: ParseMode.Html
                            );
                        }
                        else
                        {
                            // Если изображений нет, отправляем только текст
                            await botClient.SendTextMessageAsync(
                                chatId: channel.ChatId,
                                text: postMessage,
                                parseMode: ParseMode.Html
                            );
                        }

                        // Помечаем товар как опубликованный
                        product.IsPosted = true;
                        product.PostedAt = DateTime.UtcNow;

                        // Увеличиваем счетчик опубликованных постов
                        channelStat.PostedCount++;
                    }
                    catch (Exception ex)
                    {
                        // Уведомляем администратора об ошибке
                        await botClient.SendTextMessageAsync(channel.ChatId, $"Ошибка при постинге: {ex.Message}");
                        // Увеличиваем счетчик неудачных попыток
                        channelStat.FailedCount++;
                        Console.WriteLine($"Ошибка при постинге: {ex.Message}");
                    }
                }

                channelStat.LastUpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }
        private (string Text, List<string> MediaUrls) BuildPost(Product product, PostSettings postSettings)
        {
            var messageParts = new List<string>();
            var mediaUrls = new List<string>();

            // Добавляем текст поста
            if (postSettings == null || string.IsNullOrEmpty(postSettings.Order))
            {
                // Стандартный формат
                messageParts.Add($"<b>{product.Name}</b>");
                messageParts.Add($"Цена: {product.AvgPrice} RUB");
                messageParts.Add($"Рейтинг: {product.Rating} ⭐");
                messageParts.Add($"Отзывов: {product.OpinionCount}");
            }
            else
            {
                // Формируем текст в соответствии с настройками
                var orderParts = postSettings.Order.Split(',');

                foreach (var part in orderParts)
                {
                    switch (part)
                    {
                        case "Price":
                            messageParts.Add($"Цена: {product.AvgPrice} RUB");
                            break;
                        case "Title":
                            messageParts.Add($"<b>{product.Name}</b>");
                            break;
                        case "Caption":
                            messageParts.Add(postSettings.CaptionTemplate ?? "Описание отсутствует");
                            break;
                    }
                }

                if (postSettings.ShowRating)
                {
                    messageParts.Add($"Рейтинг: {product.Rating} ⭐");
                }

                if (postSettings.ShowOpinionCount)
                {
                    messageParts.Add($"Отзывов: {product.OpinionCount}");
                }
            }

            // Добавляем медиа-файлы
            if (product.Photos != null && product.Photos.Any())
            {
                mediaUrls.AddRange(product.Photos);
            }

            return (string.Join("\n", messageParts), mediaUrls);
        }

        private string BuildPostMessage(Product product, PostSettings postSettings, string partnerUrl, string priceType)
        {
            var messageParts = new List<string>();

            // Выбираем цену в зависимости от настроек
            string price = priceType switch
            {
                "min" => product.MinPrice,
                "max" => product.MaxPrice,
                _ => product.AvgPrice // По умолчанию используем среднюю цену
            };

            if (postSettings == null)
            {
                // Если настройки сборки поста не заданы, используем стандартный формат
                messageParts.Add($"<b>{product.Name}</b>");
                messageParts.Add($"Цена: {price} RUB");
                messageParts.Add($"Рейтинг: {product.Rating} ⭐");
                messageParts.Add($"Отзывов: {product.OpinionCount}");
                messageParts.Add($"Ссылка: {partnerUrl}"); // Добавляем партнерскую ссылку
            }
            else
            {
                // Формируем сообщение в соответствии с настройками
                var orderParts = postSettings.Order.Split(',');

                foreach (var part in orderParts)
                {
                    switch (part)
                    {
                        case "Price":
                            messageParts.Add($"Цена: {price} RUB");
                            break;
                        case "Title":
                            messageParts.Add($"<b>{product.Name}</b>");
                            break;
                        case "Caption":
                            messageParts.Add(postSettings.CaptionTemplate);
                            break;
                    }
                }

                if (postSettings.ShowRating)
                {
                    messageParts.Add($"Рейтинг: {product.Rating} ⭐");
                }

                if (postSettings.ShowOpinionCount)
                {
                    messageParts.Add($"Отзывов: {product.OpinionCount}");
                }

                // Добавляем партнерскую ссылку
                messageParts.Add($"Ссылка: {partnerUrl}");
            }

            return string.Join("\n", messageParts);
        }

        public void Stop()
        {
            _parseTimer?.Change(Timeout.Infinite, 0);
            _postTimer?.Change(Timeout.Infinite, 0);
        }

        public void Restart()
        {
            _parseTimer.Change(TimeSpan.Zero, ParseFrequency);
            _postTimer.Change(TimeSpan.Zero, PostFrequency);
        }
    }
}
