using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ollabotica.InputProcessors;
using OllamaSharp;
using System.Net.Http.Headers;
using Telegram.Bot;

namespace Ollabotica;

/// <summary>
/// The entry point to configure and run the IHostedService.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        //var botManager = host.Services.GetRequiredService<IBotManager>();
        //await botManager.StartBotsAsync();
        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                var env = context.HostingEnvironment;
                var files = Directory.GetFiles(env.ContentRootPath, "appsettings*.*");

                foreach (var file in files)
                {
                    config.AddJsonFile(file, optional: false, reloadOnChange: true);
                }
                //config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                var botConfigurations = context.Configuration.GetSection("Bots").Get<List<BotConfiguration>>();

                // Register configurations
                foreach (var botConfig in botConfigurations)
                {
                    services.AddSingleton(botConfig);  // Register each BotConfiguration as a singleton
                }

                services.AddSingleton<IEnumerable<BotConfiguration>>(botConfigurations);
                services.AddSingleton<IBotManager, BotManager>();
                // Register BotService as transient to create a new instance for each BotConfiguration
                services.AddTransient<IBotService, BotService>();

                // Register factories for TelegramBotClient and OllamaClient based on each BotConfiguration
                services.AddTransient(provider =>
                {
                    var botConfig = provider.GetRequiredService<BotConfiguration>();
                    return new TelegramBotClient(botConfig.TelegramToken);
                });

                // Register logging
                services.AddLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });

                services.AddSingleton<MessageInputRouter>();
                services.AddSingleton<MessageOutputRouter>();
                //services.AddTransient<IMessageInputProcessor, PromptFillingInputProcessor>();
                services.AddTransient<IMessageInputProcessor, StartNewConversationInputProcessor>();
                services.AddTransient<IMessageInputProcessor, DiagnosticsInputProcessor>();
                //services.AddTransient<IMessageInputProcessor, EchoUserTextInputProcessor>();
                services.AddTransient<IMessageOutputProcessor, BasicChatOutputProcessor>();

                services.AddHostedService<BotHostedService>();
            });
}