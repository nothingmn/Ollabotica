namespace Ollabotica;

/// <summary>
/// Interface for managing bots.
/// </summary>
public interface IBotManager
{
    Task StartBotsAsync();

    Task StopBotsAsync();

    IEnumerable<BotConfiguration> GetAllBots();
}