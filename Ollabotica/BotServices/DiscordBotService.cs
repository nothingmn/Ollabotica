using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Ollabotica.ChatServices;

namespace Ollabotica.BotServices;

public class DiscordBotService : IBotService {
    private BotConfiguration _config;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly DiscordChatService _chatService;
    private CancellationTokenSource _cts;
    private DiscordSocketClient _client;
    private readonly ILLMClient _lLMClient;

    public DiscordBotService(ILogger<DiscordBotService> logger, ILLMClient lLMClient, DiscordChatService chatService) {
        _logger = logger;
        _chatService = chatService;
        _cts = new CancellationTokenSource();
        _lLMClient = lLMClient;
    }

    public async Task StartAsync(BotConfiguration botConfig) {
        _config = botConfig;
        await _lLMClient.Init(botConfig);
        _client = new DiscordSocketClient();

        _client.Log += Log;
        _client.MessageReceived += MessageReceived;

        var token = botConfig.ChatAuthToken;
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _logger.LogInformation($"Bot {_config.Name} started for Slack.");
    }

    private async Task MessageReceived(SocketMessage message) {
        // Ensure the message is from a user and mentions the bot
        if (message.Author.IsBot)
            return; // Ignore messages from bots
        var userMessage = message as SocketUserMessage;
        if (userMessage == null)
            return;

        bool isAdmin = _config.AdminChatIds.Contains(_client.CurrentUser.Id.ToString());
        bool isAllowed = _config.AllowedChatIds.Contains(_client.CurrentUser.Id.ToString());

        var mentioned = userMessage.MentionedUsers;

        _logger.LogInformation($"Received: {message.Content} from user {_client.CurrentUser.Username} in {message.Channel.Name}");
        if (!isAllowed)
            return;

        // Check if the message is a DM (private message)
        var dm = (message.Channel is IDMChannel);
        var isMentioned = mentioned.Any(user => user.Id == _client.CurrentUser.Id);

        //exit if we are NOT mentioned in a non DM
        if (!dm && !isMentioned)
            return;

        var m = new ChatMessage() {
            Channel = message.Channel,
            IncomingText = message.Content,
            UserIdentity = $"{_client.CurrentUser.GlobalName}",
            Received = _config.Now
        };

        if (isAllowed) {
            if (!string.IsNullOrWhiteSpace(m.IncomingText)) {
                _logger.LogInformation($"Received chat slackMessage from: {m.UserIdentity} for {message.Channel.Name}: {m.IncomingText}");

                try {
                    await _lLMClient.Send(m, _chatService, isAdmin, _cts.Token);
                } catch (Exception e) {
                    _logger.LogError(e, $"Error processing slackMessage {m.ChatId}");
                    if (isAdmin) {
                        m.OutgoingText = e.ToString();
                        await _chatService.SendTextMessageAsync(m);
                    }
                }
            } else {
                m.OutgoingText = "I can only process text messages.";
                await _chatService.SendTextMessageAsync(m);
            }
        } else {
            _logger.LogWarning($"Received slackMessage from unauthorized chat: {_client.CurrentUser.Id} {_client.CurrentUser.GlobalName}");
        }
    }

    private Task Log(LogMessage msg) {
        // Map Discord's LogSeverity to ILogger's log levels
        switch (msg.Severity) {
            case LogSeverity.Critical:
                _logger.LogCritical(msg.Exception, "[{Source}] {Message}", msg.Source, msg.Message);
                break;

            case LogSeverity.Error:
                _logger.LogError(msg.Exception, "[{Source}] {Message}", msg.Source, msg.Message);
                break;

            case LogSeverity.Warning:
                _logger.LogWarning("[{Source}] {Message}", msg.Source, msg.Message);
                break;

            case LogSeverity.Info:
                _logger.LogInformation("[{Source}] {Message}", msg.Source, msg.Message);
                break;

            case LogSeverity.Verbose:
                _logger.LogDebug("[{Source}] {Message}", msg.Source, msg.Message);
                break;

            case LogSeverity.Debug:
                _logger.LogTrace("[{Source}] {Message}", msg.Source, msg.Message);
                break;

            default:
                _logger.LogInformation("[{Source}] {Message}", msg.Source, msg.Message);
                break;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync() {
        _cts.Cancel();
        _logger.LogInformation("Bot stopped.");
    }
}