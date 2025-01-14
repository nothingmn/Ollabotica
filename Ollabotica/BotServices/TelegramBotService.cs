using Microsoft.Extensions.Logging;
using Ollabotica.ChatServices;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Ollabotica.BotServices;

public class TelegramBotService : IBotService {
    private BotConfiguration _config;
    private TelegramBotClient _telegramClient;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly ILLMClient _lLMClient;
    private CancellationTokenSource _cts;

    private IChatService _telegramChatService;

    public TelegramBotService(ILogger<TelegramBotService> logger, ILLMClient lLMClient) {
        _logger = logger;
        this._lLMClient = lLMClient;
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync(BotConfiguration botConfig) {
        _config = botConfig;
        _telegramClient = new TelegramBotClient(botConfig.ChatAuthToken);
        _telegramClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, cancellationToken: _cts.Token);

        _telegramChatService = new TelegramChatService();
        _telegramChatService.Init(_telegramClient);

        await _lLMClient.Init(botConfig);

        _logger.LogInformation($"Bot {_config.Name} started for ChatAuthToken: {_config.ChatAuthToken} for Telgram BotId:{_telegramClient.BotId}");
    }

    public Task StopAsync() {
        _cts.Cancel();
        _logger.LogInformation("Bot stopped.");
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken) {
        if (update.Type == UpdateType.Message && update.Message != null) {
            var message = update.Message;
            await _telegramClient.SendChatActionAsync(message.Chat.Id.ToString(), ChatAction.Typing, cancellationToken: cancellationToken);

            bool isAdmin = _config.AdminChatIdsAsLong.Contains(message.Chat.Id);

            if (_config.AllowedChatIdsAsLong.Contains(message.Chat.Id)) {
                var m = new ChatMessage() {
                    MessageId = message.Chat.Id.ToString(),
                    IncomingText = message.Text,
                    ChatId = message.Chat.Id.ToString(),
                    UserIdentity = $"{message.Chat.FirstName} {message.Chat.LastName}",
                    Received = _config.Now,
                    Mentioned = message.Text.Contains(_config.Name, StringComparison.InvariantCultureIgnoreCase)
                };

                //does it have any text
                bool shouldSendToLLM = !string.IsNullOrWhiteSpace(m.IncomingText);
                //has text, and are we limiting by mentions only?  if so, only send if mentioned
                if (shouldSendToLLM && _config.MentionsOnly) {
                    shouldSendToLLM = m.Mentioned;
                }

                if (shouldSendToLLM) {
                    await _lLMClient.Send(m, _telegramChatService, isAdmin, cancellationToken);
                }
            } else {
                _logger.LogWarning($"Received message from unauthorized chat: {message.Chat.Id}");
            }
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken) {
        _logger.LogError(exception, $"An error occurred during bot operation for bot {client.BotId}");
        return Task.CompletedTask;
    }
}