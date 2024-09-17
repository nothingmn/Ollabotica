using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ollabotica.ChatServices;
using OllamaSharp;
using Slack.NetStandard.AsyncEnumerable;
using Slack.NetStandard.Interaction;
using Slack.NetStandard.Messages.Blocks;
using Slack.NetStandard.Messages.Elements.RichText;
using Slack.NetStandard.Socket;
using SlackAPI;
using SlackAPI.WebSocketMessages;
using Telegram.Bot.Types.Enums;

namespace Ollabotica.BotServices;

/// <summary>
/// This class will handle a single bot's Slack and Ollama connections.
/// </summary>
public class SlackBotService : IBotService
{
    private BotConfiguration _config;
    private SocketModeClient _slackClient;
    private OllamaApiClient _ollamaClient;
    private readonly ILogger<SlackBotService> _logger;
    private readonly MessageInputRouter _messageInputRouter;
    private readonly MessageOutputRouter _messageOutputRouter;
    private readonly SlackChatService _slackChatService;
    private ClientWebSocket _clientWebSocket;
    private Chat _ollamaChat;
    private CancellationTokenSource _cts;

    // Inject all required dependencies via constructor
    public SlackBotService(ILogger<SlackBotService> logger, MessageInputRouter messageInputRouter, MessageOutputRouter messageOutputRouter, SlackChatService chatService)
    {
        _logger = logger;
        _messageInputRouter = messageInputRouter;
        _messageOutputRouter = messageOutputRouter;
        _slackChatService = chatService;
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync(BotConfiguration botConfig)
    {
        _config = botConfig;
        _ollamaClient = new OllamaApiClient(botConfig.OllamaUrl, botConfig.DefaultModel);
        _ollamaClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {botConfig.OllamaToken}");
        _ollamaChat = new Chat(_ollamaClient, "");

        _clientWebSocket = new ClientWebSocket();
        var _slackClient = new SocketModeClient();

        await _slackClient.ConnectAsync(botConfig.ChatAuthToken);
        _slackChatService.Init(_slackClient);

        await foreach (var envelope in _slackClient.EnvelopeAsyncEnumerable(_cts.Token))
        {
            await HandleMessageAsync(envelope);
        }

        _logger.LogInformation($"Bot {_config.Name} started for Slack.");
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "App shutting down", CancellationToken.None);
        _slackClient.Dispose();
        _logger.LogInformation("Bot stopped.");
    }

    private async Task HandleMessageAsync(Envelope slackMessage)
    {
        if (!slackMessage.Type.Equals("events_api"))
            return; // Ignore bot messages

        //(Slack.NetStandard.EventsApi.EventCallback)slackMessage.Payload).Event
        var payload = (slackMessage.Payload as Slack.NetStandard.EventsApi.EventCallback);
        if (payload is null)
            return;

        var message = (Slack.NetStandard.Messages.Message)payload.Event;

        if (message is null)
            return;

        _logger.LogInformation($"Received Slack slackMessage: {message.Text} from user {message.User} in {message.Channel.NameNormalized}");

        bool isAdmin = _config.AdminChatIds.Contains(message.User);

        var m = new ChatMessage()
        {
            MessageId = slackMessage.EnvelopeId,
            IncomingText = message.Text,
            ChatId = slackMessage.EnvelopeId,
            UserIdentity = $"{message.User}"
        };

        if (_config.AllowedChatIds.Contains(message.User))
        {
            if (m.IncomingText != null)
            {
                _logger.LogInformation(
                    $"Received chat slackMessage from: {m.UserIdentity} for {_slackChatService.BotId}: {m.IncomingText}");

                try
                {
                    // Route the slackMessage through the input processors
                    var shouldContinue = await _messageInputRouter.Route(m, _ollamaChat, _slackChatService, isAdmin, _config);

                    if (shouldContinue)
                    {
                        var p = "";
                        if (string.IsNullOrWhiteSpace(p)) p = m.IncomingText;
                        // Send the prompt to Ollama and gather response
                        await foreach (var answerToken in _ollamaChat.Send(p))
                        {
                            await _slackChatService.SendChatActionAsync(m, "Typing");
                            m.OutgoingText += p;
                            await _messageOutputRouter.Route(m, _ollamaChat, _slackChatService, isAdmin, answerToken, _config);
                        }
                        await _messageOutputRouter.Route(m, _ollamaChat, _slackChatService, isAdmin, "\n", _config);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error processing slackMessage {m.ChatId}");
                    if (isAdmin)
                    {
                        m.OutgoingText = e.ToString();
                        await _slackChatService.SendTextMessageAsync(m);
                    }
                }
            }
            else
            {
                m.OutgoingText = "I can only process text messages.";
                await _slackChatService.SendTextMessageAsync(m);
            }
        }
        else
        {
            _logger.LogWarning($"Received slackMessage from unauthorized chat: {message.User}");
        }
    }
}