using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Ollabotica.ChatServices;
using Slack.NetStandard.AsyncEnumerable;
using Slack.NetStandard.Socket;

namespace Ollabotica.BotServices;

public class SlackBotService : IBotService {
    private BotConfiguration _config;
    private SocketModeClient _slackClient;
    private readonly ILogger<SlackBotService> _logger;
    private readonly SlackChatService _slackChatService;
    private ClientWebSocket _clientWebSocket;
    private readonly ILLMClient _lLMClient;

    private CancellationTokenSource _cts;

    public SlackBotService(ILogger<SlackBotService> logger, ILLMClient lLMClient, SlackChatService chatService) {
        _logger = logger;
        _lLMClient = lLMClient;
        _slackChatService = chatService;
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync(BotConfiguration botConfig) {
        _config = botConfig;

        await _lLMClient.Init(botConfig);

        _clientWebSocket = new ClientWebSocket();
        var _slackClient = new SocketModeClient();

        await _slackClient.ConnectAsync(botConfig.ChatAuthToken);
        _slackChatService.Init(_slackClient);

        await foreach (var envelope in _slackClient.EnvelopeAsyncEnumerable(_cts.Token)) {
            await HandleMessageAsync(envelope);
        }

        _logger.LogInformation($"Bot {_config.Name} started for Slack.");
    }

    public async Task StopAsync() {
        _cts.Cancel();
        await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "App shutting down", CancellationToken.None);
        _slackClient.Dispose();
        _logger.LogInformation("Bot stopped.");
    }

    private async Task HandleMessageAsync(Envelope slackMessage) {
        if (!slackMessage.Type.Equals("events_api"))
            return; // Ignore bot messages

        var payload = (slackMessage.Payload as Slack.NetStandard.EventsApi.EventCallback);
        if (payload is null)
            return;

        var message = (Slack.NetStandard.Messages.Message)payload.Event;

        if (message is null)
            return;

        _logger.LogInformation($"Received Slack slackMessage: {message.Text} from user {message.User} in {message.Channel.NameNormalized}");

        bool isAdmin = _config.AdminChatIds.Contains(message.User);

        var m = new ChatMessage() {
            MessageId = slackMessage.EnvelopeId,
            IncomingText = message.Text,
            ChatId = slackMessage.EnvelopeId,
            UserIdentity = $"{message.User}",
            Received = _config.Now
        };

        if (_config.AllowedChatIds.Contains(message.User)) {
            if (!string.IsNullOrWhiteSpace(m.IncomingText)) {
                _logger.LogInformation(
                    $"Received chat slackMessage from: {m.UserIdentity} for {_slackChatService.BotId}: {m.IncomingText}");

                try {
                    await _lLMClient.Send(m, _slackChatService, isAdmin, _cts.Token);
                } catch (Exception e) {
                    _logger.LogError(e, $"Error processing slackMessage {m.ChatId}");
                    if (isAdmin) {
                        m.OutgoingText = e.ToString();
                        await _slackChatService.SendTextMessageAsync(m);
                    }
                }
            } else {
                m.OutgoingText = "I can only process text messages.";
                await _slackChatService.SendTextMessageAsync(m);
            }
        } else {
            _logger.LogWarning($"Received slackMessage from unauthorized chat: {message.User}");
        }
    }
}