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

public class BasicChatOutputProcessor : IMessageOutputProcessor
{
    private readonly ILogger<BasicChatOutputProcessor> _log;

    public BasicChatOutputProcessor(ILogger<BasicChatOutputProcessor> log)
    {
        _log = log;
    }

    private string text = "";

    public async Task<bool> Handle(Message message, OllamaSharp.Chat ollamaChat, IChatService chat, bool isAdmin, string ollamaOutputText, BotConfiguration botConfiguration)
    {
        _log.LogInformation("Received message:{messageText}, ollama responded with: {ollamaOutputText}", message.Text, ollamaOutputText);

        text += ollamaOutputText;
        if (text.Contains("\n"))
        {
            foreach (var line in text.Split("\n"))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                await chat.SendChatActionAsync(message.Chat.Id, ChatAction.Typing.ToString());
                await chat.SendTextMessageAsync(message.Chat.Id, line);
            }
            text = "";
        }

        return false;
    }
}