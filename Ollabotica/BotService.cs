using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Ollabotica;

/// <summary>
/// This class will handle a single bot's Telegram and Ollama connections.
/// </summary>
public class BotService : IBotService
{
    private BotConfiguration _config;
    private TelegramBotClient _telegramClient;
    private OllamaApiClient _ollamaClient;
    private readonly ILogger<BotService> _logger;
    private readonly MessageInputRouter _messageInputRouter;
    private readonly MessageOutputRouter _messageOutputRouter;
    private OllamaSharp.Chat _ollamaChat;
    private CancellationTokenSource _cts;

    // Inject all required dependencies via constructor
    public BotService(ILogger<BotService> logger, MessageInputRouter messageInputRouter, MessageOutputRouter messageOutputRouter)
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
        _telegramClient = new TelegramBotClient(botConfig.TelegramToken);
        _ollamaChat = new OllamaSharp.Chat(_ollamaClient, "");
        _telegramClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, cancellationToken: _cts.Token);
        _logger.LogInformation(
            $"Bot {_config.Name} started for TelegramToken: {_config.TelegramToken} for Telgram BotId:{_telegramClient.BotId}");
    }

    public Task StopAsync()
    {
        _cts.Cancel();
        _logger.LogInformation("Bot stopped.");
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message != null)
        {
            var message = update.Message;
            await _telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            bool isAdmin = _config.AdminChatIds.Contains(message.Chat.Id);

            if (_config.AllowedChatIds.Contains(message.Chat.Id))
            {
                if (message.Text != null)
                {
                    _logger.LogInformation(
                        $"Received chat message from: {message.Chat.Id} for {_telegramClient.BotId}: {message.Text}");

                    var text = "";
                    var prompt = new StringBuilder();

                    try
                    {
                        // Route the message through the input processors
                        var shouldContinue =
                            await _messageInputRouter.Route(message, prompt, _ollamaChat, _telegramClient, isAdmin);

                        if (shouldContinue)
                        {
                            var p = prompt.ToString();
                            if (string.IsNullOrWhiteSpace(p)) p = message.Text;
                            // Send the prompt to Ollama and gather response
                            await foreach (var answerToken in _ollamaChat.Send(p))
                            {
                                text += answerToken;
                                await _telegramClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                            }

                            await _messageOutputRouter.Route(message, prompt, _ollamaChat, _telegramClient, isAdmin, text);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Error processing message {message.MessageId}");
                        if (isAdmin)
                        {
                            await _telegramClient.SendTextMessageAsync(message.Chat.Id, e.ToString(), cancellationToken: cancellationToken);
                        }
                    }
                }
                else
                {
                    await _telegramClient.SendTextMessageAsync(message.Chat.Id, "I can only process text messages.", cancellationToken: cancellationToken);
                }
            }
            else
            {
                _logger.LogWarning($"Received message from unauthorized chat: {message.Chat.Id}");
            }
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, $"An error occurred during bot operation for bot {client.BotId}");
        return Task.CompletedTask;
    }
}