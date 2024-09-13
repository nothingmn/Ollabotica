using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Ollabotica.InputProcessors;

public class PromptFillingInputProcessor : IMessageInputProcessor
{
    public Task<bool> Handle(Message message, StringBuilder prompt, OllamaSharp.Chat ollamaChat, TelegramBotClient telegramClient, bool isAdmin)
    {
        // Fill in the prompt information, system instructions, and user context
        prompt.AppendLine("System Instructions:");
        prompt.AppendLine("You are helpful and knowledgeable. Respond in a friendly and professional tone, providing accurate and concise information.");
        prompt.AppendLine("User Information:");
        prompt.AppendLine($"- Name: {message.Chat.FirstName} {message.Chat.LastName}");
        prompt.AppendLine($"- Username: {message.Chat.Username}");
        prompt.AppendLine($"- Chat Title: {message.Chat.Title}");

        if (message.Location is not null)
        {
            prompt.AppendLine($"- Location: {message.Location.Latitude}, {message.Location.Longitude}");
        }

        prompt.AppendLine($"- Current Date: {DateTimeOffset.Now}");
        prompt.AppendLine("- Preferences: Prefers concise, detailed responses, with a casual tone.");
        prompt.AppendLine("");
        prompt.AppendLine("Additional Parameters:");
        prompt.AppendLine("- Maximum response length: 200 words");
        prompt.AppendLine("- Output format: Step-by-step guide");
        prompt.AppendLine("- Response style: Detailed technical instruction");

        return Task.FromResult(true);
    }
}