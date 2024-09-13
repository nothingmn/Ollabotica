namespace Ollabotica;

/// <summary>
/// Interface for each bot’s lifecycle management.
/// </summary>
public interface IBotService
{
    Task StartAsync(BotConfiguration botConfig);

    Task StopAsync();
}