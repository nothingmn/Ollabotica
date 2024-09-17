using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Ollabotica;

public interface IMessageInputProcessor
{
    Task<bool> Handle(ChatMessage message, OllamaSharp.Chat ollamaChat, IChatService chat, bool isAdmin, BotConfiguration botConfiguration);
}

public interface IMessageOutputProcessor
{
    Task<bool> Handle(ChatMessage message, OllamaSharp.Chat ollamaChat, IChatService chat, bool isAdmin, string ollamaOutputText, BotConfiguration botConfiguration);
}