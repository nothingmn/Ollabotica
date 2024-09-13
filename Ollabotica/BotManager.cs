using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ollabotica;

/// <summary>
/// This class will implement IBotManager and manage multiple bots.
/// </summary>
public class BotManager : IBotManager
{
    private readonly ILogger<BotManager> _log;
    private readonly IEnumerable<BotConfiguration> _botConfigurations;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<IBotService> _botServices = new List<IBotService>();

    public BotManager(IEnumerable<BotConfiguration> botConfigurations, ILogger<BotManager> log, IServiceProvider serviceProvider)
    {
        _botConfigurations = botConfigurations;
        _log = log;
        _serviceProvider = serviceProvider;
    }

    public async Task StartBotsAsync()
    {
        foreach (var botConfig in _botConfigurations)
        {
            // Resolve BotService from IServiceProvider
            var botService = _serviceProvider.GetRequiredService<IBotService>();
            await botService.StartAsync(botConfig);
            _botServices.Add(botService);
        }
    }

    public Task StopBotsAsync()
    {
        foreach (var botService in _botServices)
        {
            botService.StopAsync();
        }
        return Task.CompletedTask;
    }

    public IEnumerable<BotConfiguration> GetAllBots() => _botConfigurations;
}