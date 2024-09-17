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

    public async Task SendTextMessageAsync(ChatMessage message, string text)
    {
        message.OutgoingText = text;
        await this.SendTextMessageAsync(message);
    }

    public void Init<T>(T chatClient) where T : class
    {
        telegramClient = chatClient as TelegramBotClient;
    }

    public string BotId
    {
        get
        {
            return telegramClient.BotId.ToString();
        }
    }

    public async Task SendChatActionAsync(ChatMessage message, string action)
    {
        var a = System.Enum.Parse<Telegram.Bot.Types.Enums.ChatAction>(action);
        await telegramClient.SendChatActionAsync(message.ChatId, a);
    }

    public async Task SendTextMessageAsync(ChatMessage message)
    {
        await telegramClient.SendTextMessageAsync(message.ChatId, message.OutgoingText);
    }
}