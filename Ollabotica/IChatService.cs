using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Requests;

namespace Ollabotica;

public interface IChatService
{
    Task SendChatActionAsync(long chatId, string action);

    Task SendTextMessageAsync(long chatId, string text);

    void Init<T>(T chatClient) where T : class;

    long BotId { get; }
}