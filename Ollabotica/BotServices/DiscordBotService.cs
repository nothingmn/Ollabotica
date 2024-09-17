using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Discord;
using Discord.WebSocket;
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
public class DiscordBotService : IBotService
{
    private BotConfiguration _config;
    private OllamaApiClient _ollamaClient;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly MessageInputRouter _messageInputRouter;
    private readonly MessageOutputRouter _messageOutputRouter;
    private readonly DiscordChatService _chatService;
    private Chat _ollamaChat;
    private CancellationTokenSource _cts;
    private DiscordSocketClient _client;

    // Inject all required dependencies via constructor
    public DiscordBotService(ILogger<DiscordBotService> logger, MessageInputRouter messageInputRouter, MessageOutputRouter messageOutputRouter, DiscordChatService chatService)
    {
        _logger = logger;
        _messageInputRouter = messageInputRouter;
        _messageOutputRouter = messageOutputRouter;
        _chatService = chatService;
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync(BotConfiguration botConfig)
    {
        _config = botConfig;
        _ollamaClient = new OllamaApiClient(botConfig.OllamaUrl, botConfig.DefaultModel);
        _ollamaClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {botConfig.OllamaToken}");
        _ollamaChat = new Chat(_ollamaClient, "");
        // Create a new instance of DiscordSocketClient
        _client = new DiscordSocketClient();

        // Set the token and login
        _client.Log += Log;
        _client.MessageReceived += MessageReceived;

        var token = botConfig.ChatAuthToken;
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _logger.LogInformation($"Bot {_config.Name} started for Slack.");
    }

    private async Task MessageReceived(SocketMessage message)
    {
        // Ensure the message is from a user and mentions the bot
        if (message.Author.IsBot) return; // Ignore messages from bots
        var userMessage = message as SocketUserMessage;
        if (userMessage == null) return;

        bool isAdmin = _config.AdminChatIds.Contains(_client.CurrentUser.Id.ToString());
        bool isAllowed = _config.AllowedChatIds.Contains(_client.CurrentUser.Id.ToString());

        var mentioned = userMessage.MentionedUsers;

        _logger.LogInformation($"Received: {message.Content} from user {_client.CurrentUser.Username} in {message.Channel.Name}");
        if (!isAllowed) return;

        // Check if the message is a DM (private message)
        var dm = (message.Channel is IDMChannel);
        var isMentioned = mentioned.Any(user => user.Id == _client.CurrentUser.Id);

        //exit if we are NOT mentioned in a non DM
        if (!dm && !isMentioned) return;

        var m = new ChatMessage()
        {
            Channel = message.Channel,
            IncomingText = message.Content,
            UserIdentity = $"{_client.CurrentUser.GlobalName}"
        };

        if (isAllowed)
        {
            if (m.IncomingText != null)
            {
                _logger.LogInformation($"Received chat slackMessage from: {m.UserIdentity} for {message.Channel.Name}: {m.IncomingText}");

                try
                {
                    // Route the slackMessage through the input processors
                    var shouldContinue = await _messageInputRouter.Route(m, _ollamaChat, _chatService, isAdmin, _config);

                    if (shouldContinue)
                    {
                        var p = "";
                        if (string.IsNullOrWhiteSpace(p)) p = m.IncomingText;
                        // Send the prompt to Ollama and gather response
                        await foreach (var answerToken in _ollamaChat.Send(p))
                        {
                            await _chatService.SendChatActionAsync(m, "Typing");
                            m.OutgoingText += p;
                            await _messageOutputRouter.Route(m, _ollamaChat, _chatService, isAdmin, answerToken, _config);
                        }
                        await _messageOutputRouter.Route(m, _ollamaChat, _chatService, isAdmin, "\n", _config);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error processing slackMessage {m.ChatId}");
                    if (isAdmin)
                    {
                        m.OutgoingText = e.ToString();
                        await _chatService.SendTextMessageAsync(m);
                    }
                }
            }
            else
            {
                m.OutgoingText = "I can only process text messages.";
                await _chatService.SendTextMessageAsync(m);
            }
        }
        else
        {
            _logger.LogWarning($"Received slackMessage from unauthorized chat: {_client.CurrentUser.Id} {_client.CurrentUser.GlobalName}");
        }
    }

    private Task Log(LogMessage msg)
    {
        // Map Discord's LogSeverity to ILogger's log levels
        switch (msg.Severity)
        {
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

    public async Task StopAsync()
    {
        _cts.Cancel();
        _logger.LogInformation("Bot stopped.");
    }
}