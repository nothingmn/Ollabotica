﻿using OllamaSharp;
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
    public async Task<bool> Handle(ChatMessage message, OllamaSharp.Chat ollamaChat, IChatService chat, bool isAdmin, BotConfiguration botConfiguration)
    {
        if (!isAdmin) return true;

        // Logic to start a new conversation by resetting OllamaSharp context
        if (message.IncomingText.Equals("/debug", StringComparison.InvariantCultureIgnoreCase))
        {
            await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
            await chat.SendTextMessageAsync(message,
                $"Diagnostics:\n\nTelegram:\n" +
                $"    ChatId: {message.UserIdentity}\n" +
                $"    Chat UserIdentity: {message.UserIdentity}\n" +
                $"    Chat HashCode: {message.GetHashCode()}\n" +
                $"    BotId: {chat.BotId}\n" +
                $"    Chat HashCode: {chat.GetHashCode()}\n" +
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
                $"    Telegram Token: {botConfiguration.ChatAuthToken}\n" +
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