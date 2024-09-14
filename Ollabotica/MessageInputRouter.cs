﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Ollabotica;

public class MessageInputRouter
{
    private readonly IEnumerable<IMessageInputProcessor> _processors;
    private readonly ILogger<MessageInputRouter> _logger;

    // Inject all available IMessageInputProcessor implementations
    public MessageInputRouter(IEnumerable<IMessageInputProcessor> processors, ILogger<MessageInputRouter> logger)
    {
        _processors = processors;
        _logger = logger;
    }

    // Routes a message to all processors
    public async Task<bool> Route(Message message, StringBuilder prompt, OllamaSharp.Chat ollamaChat, TelegramBotClient telegramClient, bool isAdmin)
    {
        foreach (var processor in _processors)
        {
            try
            {
                if (await processor.Handle(message, prompt, ollamaChat, telegramClient, isAdmin) == false) return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message {message.MessageId} with {processor.GetType().Name}");
            }
        }
        return true;
    }
}