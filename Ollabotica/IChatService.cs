using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Requests;

namespace Ollabotica;

public interface IChatService
{
    Task SendChatActionAsync(ChatMessage message, string action);

    Task SendTextMessageAsync(ChatMessage message);

    Task SendTextMessageAsync(ChatMessage message, string text);

    void Init<T>(T chatClient) where T : class;

    string BotId { get; }
}