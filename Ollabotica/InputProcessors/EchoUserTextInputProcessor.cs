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
    private readonly ILogger<BasicChatOutputProcessor> _log;

    public EchoUserTextInputProcessor(ILogger<BasicChatOutputProcessor> log)
    {
        _log = log;
    }

    public async Task<bool> Handle(ChatMessage message, OllamaSharp.Chat ollamaChat, IChatService chat, bool isAdmin, BotConfiguration botConfiguration)
    {
        _log.LogInformation("Received message:{messageText}", message.IncomingText);

        await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
        await chat.SendTextMessageAsync(message, $"You said:\n\"{message.IncomingText}\"");
        return false;
    }
}