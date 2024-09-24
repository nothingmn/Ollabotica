using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Ollabotica.ChatServices;
using Ollabotica.InputProcessors;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Ollabotica.BotServices;

/// <summary>
/// This class will handle a single bot's Telegram and Ollama connections.
/// </summary>
public class TelegramBotService : IBotService
{
    private BotConfiguration _config;
    private TelegramBotClient _telegramClient;
    private OllamaApiClient _ollamaClient;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly MessageInputRouter _messageInputRouter;
    private readonly MessageOutputRouter _messageOutputRouter;
    private OllamaSharp.Chat _ollamaChat;
    private CancellationTokenSource _cts;

    private IChatService _telegramChatService;

    // Inject all required dependencies via constructor
    public TelegramBotService(ILogger<TelegramBotService> logger, MessageInputRouter messageInputRouter, MessageOutputRouter messageOutputRouter)
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
        _telegramClient = new TelegramBotClient(botConfig.ChatAuthToken);
        _ollamaChat = new OllamaSharp.Chat(_ollamaClient, "");
        _telegramClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, cancellationToken: _cts.Token);

        _telegramChatService = new TelegramChatService();
        _telegramChatService.Init(_telegramClient);

        _logger.LogInformation(
            $"Bot {_config.Name} started for ChatAuthToken: {_config.ChatAuthToken} for Telgram BotId:{_telegramClient.BotId}");
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
            await _telegramClient.SendChatActionAsync(message.Chat.Id.ToString(), ChatAction.Typing);

            bool isAdmin = _config.AdminChatIdsAsLong.Contains(message.Chat.Id);

            if (_config.AllowedChatIdsAsLong.Contains(message.Chat.Id))
            {
                var m = new ChatMessage()
                {
                    MessageId = message.Chat.Id.ToString(),
                    IncomingText = message.Text,
                    ChatId = message.Chat.Id.ToString(),
                    UserIdentity = $"{message.Chat.FirstName} {message.Chat.LastName}",
                    Received = _config.Now
                };
                if (!string.IsNullOrWhiteSpace(m.IncomingText))
                {
                    _logger.LogInformation(
                        $"Received chat message from: {message.Chat.Id} for {_telegramClient.BotId}: {message.Text}");

                    try
                    {

                        // Route the message through the input processors
                        var shouldContinue = await _messageInputRouter.Route(m, _ollamaChat, _telegramChatService, isAdmin, _config);

                        if (shouldContinue)
                        {
                            var p = $"## VARIABLES:\nDate Time:\n{m.Received}\n";
                            p += "----\n";
                            p += $"## USER INPUT:\n{message.Text}\n";
                            p += "----\n";

                            // Send the prompt to Ollama and gather response
                            await foreach (var answerToken in _ollamaChat.Send(p))
                            {
                                await _telegramClient.SendChatActionAsync(message.Chat.Id.ToString(), ChatAction.Typing);
                                await _messageOutputRouter.Route(m, _ollamaChat, _telegramChatService, isAdmin, answerToken, _config);
                            }
                            await _messageOutputRouter.Route(m, _ollamaChat, _telegramChatService, isAdmin, "\n", _config);
                            await _messageOutputRouter.Route(m, _ollamaChat, _telegramChatService, isAdmin, AssistantOutputProcessor.AssistantTerminator, _config);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Error processing message {message.MessageId}");
                        if (isAdmin)
                        {
                            await _telegramClient.SendTextMessageAsync(message.Chat.Id.ToString(), e.ToString(), cancellationToken: cancellationToken);
                        }
                    }
                }
                else
                {
                    await _telegramClient.SendTextMessageAsync(message.Chat.Id.ToString(), "I can only process text messages.", cancellationToken: cancellationToken);
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