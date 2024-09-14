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

    public async Task<bool> Handle(Message message, StringBuilder prompt, OllamaSharp.Chat ollamaChat, TelegramBotClient telegramClient, bool isAdmin, BotConfiguration botConfiguration)
    {
        // Logic to start a new conversation by resetting OllamaSharp context
        if (message.Text.StartsWith("/listchats", StringComparison.InvariantCultureIgnoreCase))
        {
            var chatFolder = System.IO.Path.Combine(botConfiguration.ChatsFolder.FullName, message.Chat.Id.ToString());
            if (!System.IO.Directory.Exists(chatFolder)) System.IO.Directory.CreateDirectory(chatFolder);
            var chatFiles = System.IO.Directory.GetFiles(chatFolder, "*.json");

            var chats = string.Join("\n  ", chatFiles.Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToList());

            if (string.IsNullOrWhiteSpace(chats))
            {
                chats = "No chats found.\nUse:\n\n/savechat <name>\n\nto save your chats with the bot.";
            }
            await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Chats available to you include:\n{chats}");
            return false;
        }

        if (message.Text.StartsWith("/deletechat", StringComparison.InvariantCultureIgnoreCase))
        {
            var name = message.Text.Substring("/deletechat".Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Please provide a name for the chat.");
                _log.LogInformation("Trying to delete the chat, no name provided for the chat.");
                return false;
            }
            var chatFolder = System.IO.Path.Combine(botConfiguration.ChatsFolder.FullName, message.Chat.Id.ToString());
            if (!System.IO.Directory.Exists(chatFolder)) System.IO.Directory.CreateDirectory(chatFolder);
            var chatFile = System.IO.Path.Combine(chatFolder, $"{name}.json");
            _log.LogInformation($"Chat file will be deleted:{chatFile}");

            await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
            if (System.IO.File.Exists(chatFile))
            {
                System.IO.File.Delete(chatFile);
                await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Chat was deleted.");
            }
            else
            {
                await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Chat was not found.");
            }

            return false;
        }

        if (message.Text.StartsWith("/savechat", StringComparison.InvariantCultureIgnoreCase))
        {
            var name = message.Text.Substring("/savechat".Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Please provide a name for the chat.");
                _log.LogInformation("Trying to save the chat, no name provided for the chat.");
                return false;
            }
            var chats = System.Text.Json.JsonSerializer.Serialize(ollamaChat.Messages);
            var chatFolder = System.IO.Path.Combine(botConfiguration.ChatsFolder.FullName, message.Chat.Id.ToString());
            if (!System.IO.Directory.Exists(chatFolder)) System.IO.Directory.CreateDirectory(chatFolder);
            var chatFile = System.IO.Path.Combine(chatFolder, $"{name}.json");
            _log.LogInformation($"Chat file will be saved:{chatFile}");
            System.IO.File.WriteAllText(chatFile, chats);

            // Resetting the conversation
            await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
            await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Chat was saved.");
            return false;
        }

        if (message.Text.StartsWith("/loadchat", StringComparison.InvariantCultureIgnoreCase))
        {
            var name = message.Text.Substring("/loadchat".Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Please provide a name for the chat.");
                _log.LogInformation("Trying to load the chat, no name provided for the chat.");
                return false;
            }

            var chatFolder = System.IO.Path.Combine(botConfiguration.ChatsFolder.FullName, message.Chat.Id.ToString());
            if (!System.IO.Directory.Exists(chatFolder)) System.IO.Directory.CreateDirectory(chatFolder);
            var chatFile = System.IO.Path.Combine(chatFolder, $"{name}.json");

            if (!File.Exists(chatFile))
            {
                _log.LogInformation($"Chat file not found:{chatFile}");
                await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Chat was not found, try /listchats");
                return false;
            }

            _log.LogInformation($"Chat file will be loaded:{chatFile}");

            var contents = System.IO.File.ReadAllText(chatFile);
            var chats = System.Text.Json.JsonSerializer.Deserialize<List<OllamaSharp.Models.Chat.Message>>(contents);
            ollamaChat.SetMessages(chats);

            // Resetting the conversation
            await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
            await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Chat was loaded ({chats.Count}).");
            return false;
        }
        return true;
    }
}