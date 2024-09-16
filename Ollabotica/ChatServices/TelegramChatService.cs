using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Ollabotica.ChatServices;

public class TelegramChatService : IChatService
{
    private TelegramBotClient telegramClient = null;

    public void Init<T>(T chatClient) where T : class
    {
        telegramClient = chatClient as TelegramBotClient;
    }

    public long BotId
    {
        get
        {
            return telegramClient.BotId;
        }
    }

    public async Task SendChatActionAsync(long chatId, string action)
    {
        var a = System.Enum.Parse<Telegram.Bot.Types.Enums.ChatAction>(action);
        await telegramClient.SendChatActionAsync(chatId, a);
    }

    public async Task SendTextMessageAsync(long chatId, string text)
    {
        await telegramClient.SendTextMessageAsync(chatId, text);
    }
}