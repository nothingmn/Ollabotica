using SlackAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Slack.NetStandard.AsyncEnumerable;
using Slack.NetStandard.Messages.Blocks;
using Slack.NetStandard.Socket;
using SlackAPI.WebSocketMessages;
using Telegram.Bot;

namespace Ollabotica.ChatServices;

public class SlackChatService : IChatService
{
    private readonly ILogger<SlackChatService> _log;
    private SocketModeClient client = null;

    public SlackChatService(ILogger<SlackChatService> log)
    {
        _log = log;
    }

    public void Init<T>(T chatClient) where T : class
    {
        client = chatClient as SocketModeClient;
    }

    public string BotId
    {
        get
        {
            return this.GetHashCode().ToString();
        }
    }

    public async Task SendChatActionAsync(ChatMessage message, string action)
    {
        await client.Send(System.Text.Json.JsonSerializer.Serialize(new Typing() { }));
    }

    public async Task SendTextMessageAsync(ChatMessage message, string text)
    {
        message.OutgoingText = text;
        await this.SendTextMessageAsync(message);
    }

    public async Task SendTextMessageAsync(ChatMessage message)
    {
        _log.LogInformation("Sending message to chatId: {chatId}, message: {text}", message.ChatId, message.OutgoingText);
        var msg = System.Text.Json.JsonSerializer.Serialize(new Acknowledge()
        {
            EnvelopeId = message.ChatId,
            Payload = new Slack.NetStandard.Messages.Message()
            {
                Blocks = new List<IMessageBlock>
                {
                    new Section(message.OutgoingText)
                }
            }
        });
        _log.LogInformation("Message Sent: {msg}", msg);
        try
        {
            await client.Send(msg);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
    }
}