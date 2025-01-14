namespace Ollabotica;

public interface ILLMClient {

    Task Init(BotConfiguration botConfig);

    Task Send(ChatMessage message, IChatService chatService, bool isAdmin, CancellationToken cancellationToken);
}