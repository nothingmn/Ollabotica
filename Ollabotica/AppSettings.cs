namespace Ollabotica;

/// <summary>
/// To load configuration settings for multiple bots from appsettings.json.
/// </summary>
public class AppSettings
{
    public List<BotConfiguration> Bots { get; set; } = new List<BotConfiguration>();
}