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

[Trigger(Trigger = "listmodels", Description = "List all available models.")]
[Trigger(Trigger = "usemodel", Description = "Change the current chat to use a different model. /usemodel <name>")]
public class ModelManagerInputProcessor : IMessageInputProcessor
{
    private readonly ILogger<ModelManagerInputProcessor> _log;

    public ModelManagerInputProcessor(ILogger<ModelManagerInputProcessor> log)
    {
        _log = log;
    }

    public async Task<bool> Handle(Message message, StringBuilder prompt, OllamaSharp.Chat ollamaChat, TelegramBotClient telegramClient, bool isAdmin, BotConfiguration botConfiguration)
    {
        if (message.Text.StartsWith("/listmodels", StringComparison.InvariantCultureIgnoreCase))
        {
            await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            var models = await ollamaChat.Client.ListLocalModels();
            if (!models.Any())
            {
                await telegramClient.SendTextMessageAsync(message.Chat.Id, "No models found.");
            }
            else
            {
                var modelList = new StringBuilder();
                modelList.AppendLine("Available models:");
                foreach (var m in models)
                {
                    modelList.AppendLine($"  {m.Name} ({m.Details.ParameterSize})");
                }
                await telegramClient.SendTextMessageAsync(message.Chat.Id, modelList.ToString());
            }

            return false;
        }
        if (message.Text.StartsWith("/usemodel", StringComparison.InvariantCultureIgnoreCase))
        {
            await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            var name = message.Text.Substring("/usemodel".Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Please provide a name for the model to use.");
                _log.LogInformation("Trying to use a model with no name specified.");
                return false;
            }

            var models = await ollamaChat.Client.ListLocalModels();
            var existingModel = models.FirstOrDefault(m => m.Name == name);
            if (existingModel is null)
            {
                await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Model {name} not found.");
                return false;
            }
            else
            {
                ollamaChat.Model = existingModel.Name;
                ollamaChat.Client.SelectedModel = existingModel.Name;
                await telegramClient.SendTextMessageAsync(message.Chat.Id, $"Model {name} selected.");
            }

            return false;
        }

        return true;
    }
}