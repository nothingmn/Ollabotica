using System.Text;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using SlackAPI;
using SlackAPI.WebSocketMessages;

namespace Ollabotica;

/// <summary>
/// This class will handle a single bot's Slack and Ollama connections.
/// </summary>
public class SlackBotService : IBotService
{
    private BotConfiguration _config;
    private SlackSocketClient _slackClient;
    private OllamaApiClient _ollamaClient;
    private readonly ILogger<SlackBotService> _logger;
    private readonly MessageInputRouter _messageInputRouter;
    private readonly MessageOutputRouter _messageOutputRouter;
    private OllamaSharp.Chat _ollamaChat;
    private CancellationTokenSource _cts;

    // Inject all required dependencies via constructor
    public SlackBotService(ILogger<SlackBotService> logger, MessageInputRouter messageInputRouter, MessageOutputRouter messageOutputRouter)
    {
        _logger = logger;
        _messageInputRouter = messageInputRouter;
        _messageOutputRouter = messageOutputRouter;
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync(BotConfiguration botConfig)
    {
        _config = botConfig;
        _ollamaClient = new OllamaApiClient(botConfig.OllamaUrl, botConfig.DefaultModel);
        _ollamaClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {botConfig.OllamaToken}");
        _ollamaChat = new OllamaSharp.Chat(_ollamaClient, "");

        // Connect to Slack via Socket API
        _slackClient = new SlackSocketClient(botConfig.ChatAuthToken); // Replace with your actual Slack token

        _slackClient.OnMessageReceived += HandleMessageAsync;
        _slackClient.Connect((connected) =>
        {
            _logger.LogInformation($"Bot {_config.Name} connected to Slack.");
        }, () =>
        {
            _logger.LogInformation($"Bot {_config.Name} disconnected from Slack.");
        });

        _logger.LogInformation($"Bot {_config.Name} started for Slack.");
    }

    public Task StopAsync()
    {
        _cts.Cancel();
        _slackClient.CloseSocket();
        _logger.LogInformation("Bot stopped.");
        return Task.CompletedTask;
    }

    private async void HandleMessageAsync(NewMessage message)
    {
        if (message.subtype == "bot_message")
            return; // Ignore bot messages

        _logger.LogInformation($"Received Slack message: {message.text} from user {message.user}");

        bool isAdmin = _config.AdminChatIds.Contains(long.Parse(message.user)); // Assuming user IDs are long, adjust if needed

        if (_config.AllowedChatIds.Contains(long.Parse(message.user)))
        {
            var prompt = new StringBuilder();

            try
            {
                // Route the message through the input processors
                //var shouldContinue = await _messageInputRouter.Route(message, prompt, _ollamaChat, _slackClient, isAdmin, _config);

                //if (shouldContinue)
                //{
                //    var p = prompt.ToString();
                //    if (string.IsNullOrWhiteSpace(p)) p = message.text;

                //    // Send the prompt to Ollama and gather response
                //    await foreach (var answerToken in _ollamaChat.Send(p))
                //    {
                //        await SendTypingIndicatorAsync(message.channel); // Slack doesn't have a "typing" action like Telegram
                //        await _messageOutputRouter.Route(message, prompt, _ollamaChat, _slackClient, isAdmin, answerToken, _config);
                //    }
                //    await _messageOutputRouter.Route(message, prompt, _ollamaChat, _slackClient, isAdmin, "\n", _config);
                //}
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error processing Slack message {message.ts}");
                if (isAdmin)
                {
                    await SendMessageAsync(message.channel, e.ToString());
                }
            }
        }
        else
        {
            _logger.LogWarning($"Received message from unauthorized user: {message.user}");
        }
    }

    private async Task SendMessageAsync(string channel, string text)
    {
        //await _slackClient.SendMessageAsync((response) =>
        //{
        //    if (response.ok)
        //    {
        //        _logger.LogInformation($"Message sent to channel {channel}: {text}");
        //    }
        //    else
        //    {
        //        _logger.LogError($"Failed to send message to Slack channel {channel}: {response.error}");
        //    }
        //}, channel, text);
    }

    private async Task SendTypingIndicatorAsync(string channel)
    {
        //await _slackClient.IndicateTyping(channel);
    }
}