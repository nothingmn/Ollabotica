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

namespace Ollabotica.InputProcessors;

[Trigger(Trigger = "debug", Description = "Dump diagnostic information.", IsAdmin = true)]
public class DiagnosticsInputProcessor : IMessageInputProcessor
{
    public async Task<bool> Handle(Message message, StringBuilder prompt, OllamaSharp.Chat ollamaChat, TelegramBotClient telegramClient, bool isAdmin, BotConfiguration botConfiguration)
    {
        if (!isAdmin) return true;

        // Logic to start a new conversation by resetting OllamaSharp context
        if (message.Text.Equals("/debug", StringComparison.InvariantCultureIgnoreCase))
        {
            await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
            await telegramClient.SendTextMessageAsync(message.Chat.Id,
                $"Diagnostics:\n\nTelegram:\n" +
                $"    ChatId: {message.Chat.Id}\n" +
                $"    Chat FirstName: {message.Chat.FirstName}\n" +
                $"    Chat LastName: {message.Chat.LastName}\n" +
                $"    Chat Title: {message.Chat.Title}\n" +
                $"    Chat Username: {message.Chat.Username}\n" +
                $"    Chat IsForum: {message.Chat.IsForum}\n" +
                $"    Chat Type: {message.Chat.Type}\n" +
                $"    Chat HashCode: {message.Chat.GetHashCode()}\n" +
                $"    BotId: {telegramClient.BotId}\n" +
                $"    Chat HashCode: {telegramClient.GetHashCode()}\n" +
                "\nOllama:\n" +
                $"    Version: {(await ollamaChat.Client.GetVersion())?.ToString()}\n" +
                $"    Client HashCode: {ollamaChat.Client.GetHashCode()}\n" +
                $"    Client SelectedModel: {ollamaChat.Client.SelectedModel}\n" +
                $"    Model: {ollamaChat.Model}\n" +
                $"    Messages HashCode: {ollamaChat.Messages.GetHashCode()}\n" +
                $"    Messages Count: {ollamaChat.Messages.Count}\n" +
                $"    Options: {System.Text.Json.JsonSerializer.Serialize(ollamaChat.Options)}\n" +
                "\nConfig:\n" +
                $"    HashCode: {botConfiguration.GetHashCode()}\n" +
                $"    Name: {botConfiguration.Name}\n" +
                $"    Chat Folder: {botConfiguration.ChatsFolder.FullName}\n" +
                $"    Telegram Token: {botConfiguration.TelegramToken}\n" +
                $"    Allowed ChatIds: {botConfiguration.AllowedChatIdsRaw}\n" +
                $"    Admin ChatIds: {botConfiguration.AdminChatIdsRaw}\n" +
                $"    Default Model: {botConfiguration.DefaultModel}\n" +
                $"    Ollama Url: {botConfiguration.OllamaUrl}\n" +
                $"    Ollama Token: {botConfiguration.OllamaToken}\n" +
                $"    New Chat Prompt: {botConfiguration.NewChatPrompt}\n" +
                ""
                );
            return false;
        }

        return true;
    }
}