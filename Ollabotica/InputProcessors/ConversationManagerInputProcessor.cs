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
using File = System.IO.File;

namespace Ollabotica.InputProcessors;

[Trigger(Trigger = "listchats", Description = "List all saved chats.")]
[Trigger(Trigger = "deletechat", Description = "Delete a saved chat. /deletechat <name>")]
[Trigger(Trigger = "savechat", Description = "Save the current chat. /savechat <name>")]
[Trigger(Trigger = "loadchat", Description = "Load a chat based on its name.  /loadchat <name>")]
public class ConversationManagerInputProcessor : IMessageInputProcessor
{
    private readonly ILogger<ConversationManagerInputProcessor> _log;

    public ConversationManagerInputProcessor(ILogger<ConversationManagerInputProcessor> log)
    {
        _log = log;
    }

    public async Task<bool> Handle(ChatMessage message, OllamaSharp.Chat ollamaChat, IChatService chat, bool isAdmin, BotConfiguration botConfiguration)
    {
        // Logic to start a new conversation by resetting OllamaSharp context
        if (message.IncomingText.StartsWith("/listchats", StringComparison.InvariantCultureIgnoreCase))
        {
            var chatFolder = System.IO.Path.Combine(botConfiguration.ChatsFolder.FullName, ChatAction.Typing.ToString().ToString());
            if (!System.IO.Directory.Exists(chatFolder)) System.IO.Directory.CreateDirectory(chatFolder);
            var chatFiles = System.IO.Directory.GetFiles(chatFolder, "*.json");

            var chats = string.Join("\n  ", chatFiles.Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToList());

            if (string.IsNullOrWhiteSpace(chats))
            {
                chats = "No chats found.\nUse:\n\n/savechat <name>\n\nto save your chats with the bot.";
            }
            await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());

            await chat.SendTextMessageAsync(message, $"Chats available to you include:\n{chats}");
            return false;
        }

        if (message.IncomingText.StartsWith("/deletechat", StringComparison.InvariantCultureIgnoreCase))
        {
            var name = message.IncomingText.Substring("/deletechat".Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
                await chat.SendTextMessageAsync(message, $"Please provide a name for the chat.");
                _log.LogInformation("Trying to delete the chat, no name provided for the chat.");
                return false;
            }
            var chatFolder = System.IO.Path.Combine(botConfiguration.ChatsFolder.FullName, ChatAction.Typing.ToString().ToString());
            if (!System.IO.Directory.Exists(chatFolder)) System.IO.Directory.CreateDirectory(chatFolder);
            var chatFile = System.IO.Path.Combine(chatFolder, $"{name}.json");
            _log.LogInformation($"Chat file will be deleted:{chatFile}");

            await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
            if (System.IO.File.Exists(chatFile))
            {
                System.IO.File.Delete(chatFile);
                await chat.SendTextMessageAsync(message, $"Chat was deleted.");
            }
            else
            {
                await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
                await chat.SendTextMessageAsync(message, $"Chat was not found.");
            }

            return false;
        }

        if (message.IncomingText.StartsWith("/savechat", StringComparison.InvariantCultureIgnoreCase))
        {
            var name = message.IncomingText.Substring("/savechat".Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
                await chat.SendTextMessageAsync(message, $"Please provide a name for the chat.");
                _log.LogInformation("Trying to save the chat, no name provided for the chat.");
                return false;
            }
            var chats = System.Text.Json.JsonSerializer.Serialize(ollamaChat.Messages);
            var chatFolder = System.IO.Path.Combine(botConfiguration.ChatsFolder.FullName, ChatAction.Typing.ToString().ToString());
            if (!System.IO.Directory.Exists(chatFolder)) System.IO.Directory.CreateDirectory(chatFolder);
            var chatFile = System.IO.Path.Combine(chatFolder, $"{name}.json");
            _log.LogInformation($"Chat file will be saved:{chatFile}");
            System.IO.File.WriteAllText(chatFile, chats);

            // Resetting the conversation
            await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
            await chat.SendTextMessageAsync(message, $"Chat was saved.");
            return false;
        }

        if (message.IncomingText.StartsWith("/loadchat", StringComparison.InvariantCultureIgnoreCase))
        {
            var name = message.IncomingText.Substring("/loadchat".Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
                await chat.SendTextMessageAsync(message, $"Please provide a name for the chat.");
                _log.LogInformation("Trying to load the chat, no name provided for the chat.");
                return false;
            }

            var chatFolder = System.IO.Path.Combine(botConfiguration.ChatsFolder.FullName, ChatAction.Typing.ToString().ToString());
            if (!System.IO.Directory.Exists(chatFolder)) System.IO.Directory.CreateDirectory(chatFolder);
            var chatFile = System.IO.Path.Combine(chatFolder, $"{name}.json");

            if (!File.Exists(chatFile))
            {
                _log.LogInformation($"Chat file not found:{chatFile}");
                await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
                await chat.SendTextMessageAsync(message, $"Chat was not found, try /listchats");
                return false;
            }

            _log.LogInformation($"Chat file will be loaded:{chatFile}");

            var contents = System.IO.File.ReadAllText(chatFile);
            var chats = System.Text.Json.JsonSerializer.Deserialize<List<OllamaSharp.Models.Chat.Message>>(contents);
            ollamaChat.SetMessages(chats);

            // Resetting the conversation
            await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
            await chat.SendTextMessageAsync(message, $"Chat was loaded ({chats.Count}).");
            return false;
        }
        return true;
    }
}