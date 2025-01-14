using Microsoft.Extensions.Logging;
using Ollabotica.ChatServices;
using OllamaSharp;
using System.Threading;
using Telegram.Bot.Types.Enums;
using static OllamaSharp.OllamaApiClient;

namespace Ollabotica.OllamaClient;

public class OllamaWebUILLMClient : ILLMClient {
    private readonly ILogger<OllamaWebUILLMClient> _log;
    private readonly MessageInputRouter _messageInputRouter;
    private readonly MessageOutputRouter _messageOutputRouter;
    private OllamaApiClient _ollamaClient;
    private OllamaSharp.Chat _ollamaChat;
    private BotConfiguration _botConfig;

    public OllamaWebUILLMClient(ILogger<OllamaWebUILLMClient> log, MessageInputRouter messageInputRouter, MessageOutputRouter messageOutputRouter) {
        this._log = log;
        this._messageInputRouter = messageInputRouter;
        this._messageOutputRouter = messageOutputRouter;
    }

    public async Task Init(BotConfiguration botConfig) {
        _botConfig = botConfig;
        _ollamaClient = new OllamaApiClient(botConfig.OllamaUrl, botConfig.DefaultModel);
        _ollamaClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {botConfig.OllamaToken}");
        _ollamaChat = new OllamaSharp.Chat(_ollamaClient, "");
    }

    public async Task Send(ChatMessage message, IChatService chatService, bool isAdmin, CancellationToken cancellationToken) {
        _log.LogInformation($"Received chat message from: {message.MessageId} for {message.ChatId}: {message.IncomingText}");
        try {
            // Route the message through the input processors
            var shouldContinue = await _messageInputRouter.Route(message, _ollamaChat, chatService, isAdmin, _botConfig);

            if (shouldContinue) {
                var user_input = message.IncomingText;
                _log.LogInformation("LLM Message: {user_input}", user_input);
                // Send the prompt to Ollama and gather response
                await foreach (var answerToken in _ollamaChat.SendAsync(user_input, cancellationToken)) {
                    await chatService.SendChatActionAsync(message, ChatAction.Typing.ToString());
                    await _messageOutputRouter.Route(message, _ollamaChat, chatService, isAdmin, answerToken, _botConfig);
                }
                await _messageOutputRouter.Route(message, _ollamaChat, chatService, isAdmin, "\n", _botConfig);
            }
        } catch (Exception e) {
            _log.LogError(e, $"Error processing message {message.MessageId}");
            if (isAdmin) {
                await chatService.SendTextMessageAsync(message);
            }
        }
    }
}