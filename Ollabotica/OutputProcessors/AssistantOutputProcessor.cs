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
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Ollabotica.OutputProcessors.Assistant;
using Microsoft.Extensions.Options;
using Ollabotica.OutputProcessors.Assistant.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;


namespace Ollabotica.InputProcessors;

public class AssistantOutputProcessor : IMessageOutputProcessor
{
    public static string AssistantTerminator { get { return "¦"; } }
    private readonly ILogger<AssistantOutputProcessor> _log;
    private readonly IServiceProvider _serviceProvider;

    public AssistantOutputProcessor(ILogger<AssistantOutputProcessor> log, IServiceProvider serviceProvider)
    {
        _log = log;
        this._serviceProvider = serviceProvider;
    }

    private string text = "";

    public async Task<bool> Handle(ChatMessage message, OllamaSharp.Chat ollamaChat, IChatService chat, bool isAdmin, string ollamaOutputText, BotConfiguration botConfiguration)
    {
        await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
        _log.LogInformation("Received message:{messageText}, ollama responded with: {ollamaOutputText}", message.IncomingText, ollamaOutputText);

        if (ollamaOutputText.Equals(AssistantTerminator))
        {
            await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());
            var sections = Parse(text);
            var options = new JsonSerializerOptions
            {
                Converters = { new TaskBaseConverter() },
                PropertyNameCaseInsensitive = true
            };

            if (sections.Count > 0)
            {
                foreach (var section in sections.Where(s => s.IsMarkdown == false))
                {
                    message.OutgoingText = section.MarkdownOrText;
                    await chat.SendTextMessageAsync(message);
                }
                foreach (var section in sections.Where(s => s.IsMarkdown))
                {
                    _log.LogInformation($"\n\n{section.MarkdownOrText}\n\n");
                    try
                    {
                        var task = JsonSerializer.Deserialize<TaskBase>(section.MarkdownOrText, options);
                        if (task != null)
                        {
                            var action = _serviceProvider.GetKeyedService<ITaskAction>(task.GetType().FullName);
                            if(action != null)
                            {
                                string actionResult = null;
                                if (task.GetType().Equals(typeof(CreateTimer)))
                                {
                                    actionResult = await action.CreateTimer(task as CreateTimer, botConfiguration);
                                }
                                if (!string.IsNullOrWhiteSpace(actionResult))
                                {
                                    await chat.SendChatActionAsync(message, ChatAction.Typing.ToString());

                                    _log.LogInformation($"\n\n{actionResult}\n\n");
                                    message.OutgoingText = actionResult;
                                    await chat.SendTextMessageAsync(message);

                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, "Error during task and action execution.");
                    }


                }
            }
            text = "";
        }
        else
        {
            text += ollamaOutputText;
        }

        return false;
    }
    public List<ResponseSection> Parse(string input)
    {
        var sections = new List<ResponseSection>();

        // Regex to handle markdown blocks with or without language specifiers, matching ` ``` ` and ` ```json `
        var markdownRegex = new Regex(@"```(\w+)?(.*?)```", RegexOptions.Singleline);

        // Regex to detect JSON-like structures (even without markdown wrapping)
        var jsonLikeRegex = new Regex(@"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}", RegexOptions.Singleline);

        int lastIndex = 0;

        // Process markdown blocks first
        foreach (Match match in markdownRegex.Matches(input))
        {
            // Text before the markdown block (non-markdown text)
            if (match.Index > lastIndex)
            {
                string nonMarkdownText = input.Substring(lastIndex, match.Index - lastIndex);
                ProcessNonMarkdownText(nonMarkdownText, sections, jsonLikeRegex);
            }

            // Markdown block itself
            string markdownBlock = match.Groups[2].Value;
            sections.Add(new ResponseSection { IsMarkdown = true, MarkdownOrText = markdownBlock.Trim() });

            // Update lastIndex to the end of this match
            lastIndex = match.Index + match.Length;
        }

        // Add any remaining non-markdown text after the last markdown block
        if (lastIndex < input.Length)
        {
            string remainingText = input.Substring(lastIndex);
            ProcessNonMarkdownText(remainingText, sections, jsonLikeRegex);
        }

        return sections;
    }

    private void ProcessNonMarkdownText(string text, List<ResponseSection> sections, Regex jsonLikeRegex)
    {
        int lastJsonIndex = 0;

        // Search for JSON-like structures within the non-markdown text
        foreach (Match jsonMatch in jsonLikeRegex.Matches(text))
        {
            // Add any text before the JSON block
            if (jsonMatch.Index > lastJsonIndex)
            {
                string nonJsonText = text.Substring(lastJsonIndex, jsonMatch.Index - lastJsonIndex);
                if (!string.IsNullOrWhiteSpace(nonJsonText))
                {
                    sections.Add(new ResponseSection { IsMarkdown = false, MarkdownOrText = nonJsonText.Trim() });
                }
            }

            // Add the JSON-like block itself
            string jsonBlock = jsonMatch.Value;
            sections.Add(new ResponseSection { IsMarkdown = false, MarkdownOrText = jsonBlock.Trim() });

            // Update lastJsonIndex to the end of this match
            lastJsonIndex = jsonMatch.Index + jsonMatch.Length;
        }

        // Add any remaining non-JSON text after the last JSON block
        if (lastJsonIndex < text.Length)
        {
            string remainingText = text.Substring(lastJsonIndex);
            if (!string.IsNullOrWhiteSpace(remainingText))
            {
                sections.Add(new ResponseSection { IsMarkdown = false, MarkdownOrText = remainingText.Trim() });
            }
        }
    }
}

