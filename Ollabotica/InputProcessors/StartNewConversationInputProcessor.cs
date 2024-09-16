using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace Ollabotica.InputProcessors;

[Trigger(Trigger = "start", Description = "Clears the message history and starts a new conversation with the model.", IsAdmin = false)]
[Trigger(Trigger = "new", Description = "Clears the message history and starts a new conversation with the model.", IsAdmin = false)]
public class StartNewConversationInputProcessor : IMessageInputProcessor
{
    public async Task<bool> Handle(Message message, OllamaSharp.Chat ollamaChat, IChatService chat, bool isAdmin, BotConfiguration botConfiguration)
    {
        // Logic to start a new conversation by resetting OllamaSharp context
        if (message.Text.Equals("/new", StringComparison.InvariantCultureIgnoreCase) || message.Text.Equals("/newchat", StringComparison.InvariantCultureIgnoreCase) || message.Text.Equals("/start", StringComparison.InvariantCultureIgnoreCase) || message.Text.Equals("/clear", StringComparison.InvariantCultureIgnoreCase))
        {
            // Resetting the conversation
            ollamaChat.SetMessages(new List<OllamaSharp.Models.Chat.Message>());
            await chat.SendChatActionAsync(message.Chat.Id, ChatAction.Typing.ToString());
            await chat.SendTextMessageAsync(message.Chat.Id, $"Chat was cleared.");
            if (!string.IsNullOrWhiteSpace(botConfiguration.NewChatPrompt))
            {
                await chat.SendChatActionAsync(message.Chat.Id, ChatAction.Typing.ToString());
                await foreach (var answerToken in ollamaChat.Send(botConfiguration.NewChatPrompt))
                {
                    await chat.SendChatActionAsync(message.Chat.Id, ChatAction.Typing.ToString());
                    await chat.SendTextMessageAsync(message.Chat.Id, answerToken);
                }
            }

            return false;
        }

        return true;
    }
}