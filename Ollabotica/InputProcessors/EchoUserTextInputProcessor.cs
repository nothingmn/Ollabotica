using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace Ollabotica.InputProcessors;

public class EchoUserTextInputProcessor : IMessageInputProcessor
{
    private readonly ILogger<EchoUserTextInputProcessor> _log;

    public EchoUserTextInputProcessor(ILogger<EchoUserTextInputProcessor> log)
    {
        _log = log;
    }

    public async Task<bool> Handle(Message message, StringBuilder prompt, OllamaSharp.Chat ollamaChat, TelegramBotClient telegramClient, bool isAdmin)
    {
        _log.LogInformation("Received message:{messageText}", message.Text);

        await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
        await telegramClient.SendTextMessageAsync(message.Chat.Id, $"You said:\n\"{message.Text}\"");
        return false;
    }
}