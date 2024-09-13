using Microsoft.Extensions.Hosting;

namespace Ollabotica;

/// <summary>
/// This class integrates BotManager with IHostedService lifecycle.
/// </summary>
public class BotHostedService : IHostedService
{
    private readonly IBotManager _botManager;

    public BotHostedService(IBotManager botManager)
    {
        _botManager = botManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _botManager.StartBotsAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _botManager.StopBotsAsync();
    }
}