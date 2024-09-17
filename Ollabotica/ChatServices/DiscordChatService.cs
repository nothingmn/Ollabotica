using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Ollabotica.ChatServices;

public class DiscordChatService : IChatService
{
    public async Task SendTextMessageAsync(ChatMessage message, string text)
    {
        message.OutgoingText = text;
        await this.SendTextMessageAsync(message);
    }

    public void Init<T>(T chatClient) where T : class
    {
    }

    public string BotId
    {
        get
        {
            return "0";
        }
    }

    public async Task SendChatActionAsync(ChatMessage message, string action)
    {
        var discordChannel = message.Channel as ISocketMessageChannel;
        if (discordChannel != null)
        {
            var a = System.Enum.Parse<Telegram.Bot.Types.Enums.ChatAction>(action);
            if (a is ChatAction.Typing) await discordChannel.TriggerTypingAsync(RequestOptions.Default);
        }
        //await telegramClient.SendChatActionAsync(message.ChatId, a);
    }

    public async Task SendTextMessageAsync(ChatMessage message)
    {
        var discordChannel = message.Channel as ISocketMessageChannel;
        if (discordChannel != null)
        {
            await discordChannel.SendMessageAsync(message.OutgoingText);
        }
    }
}