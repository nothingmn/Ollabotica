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

    public async Task<bool> Handle(ChatMessage message, OllamaSharp.Chat ollamaChat, IChatService chat, bool isAdmin, BotConfiguration botConfiguration)
    {
        if (message.IncomingText.StartsWith("/listmodels", StringComparison.InvariantCultureIgnoreCase))
        {
            await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());

            var models = await ollamaChat.Client.ListLocalModels();
            if (!models.Any())
            {
                await chat.SendTextMessageAsync(message, "No models found.");
            }
            else
            {
                var modelList = new StringBuilder();
                modelList.AppendLine("Available models:");
                foreach (var m in models)
                {
                    modelList.AppendLine($"  {m.Name} ({m.Details.ParameterSize})");
                }
                await chat.SendTextMessageAsync(message, modelList.ToString());
            }

            return false;
        }
        if (message.IncomingText.StartsWith("/usemodel", StringComparison.InvariantCultureIgnoreCase))
        {
            await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());

            var name = message.IncomingText.Substring("/usemodel".Length).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
                await chat.SendTextMessageAsync(message, $"Please provide a name for the model to use.");
                _log.LogInformation("Trying to use a model with no name specified.");
                return false;
            }

            var models = await ollamaChat.Client.ListLocalModels();
            var existingModel = models.FirstOrDefault(m => m.Name == name);
            if (existingModel is null)
            {
                await chat.SendTextMessageAsync(message, $"Model {name} not found.");
                return false;
            }
            else
            {
                ollamaChat.Model = existingModel.Name;
                ollamaChat.Client.SelectedModel = existingModel.Name;
                await chat.SendTextMessageAsync(message, $"Model {name} selected.");
            }

            return false;
        }

        return true;
    }
}