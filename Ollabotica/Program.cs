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

        Console.WriteLine(@"
   ___  _ _       _           _   _
  / _ \| | | __ _| |__   ___ | |_(_) ___ __ _
 | | | | | |/ _` | '_ \ / _ \| __| |/ __/ _` |
 | |_| | | | (_| | |_) | (_) | |_| | (_| (_| |
  \___/|_|_|\__,_|_.__/ \___/ \__|_|\___\__,_|
        ~~ Bringing AI to Telegram! ~~
        ");

        Console.WriteLine("\n----------------------------------------------\n");

        // Get admin and non-admin triggers
        var (adminTriggers, nonAdminTriggers) = AttributeScanner.GetAdminAndNonAdminTriggers();

        // Output the admin triggers
        Console.WriteLine("Admin Triggers:");
        foreach (var trigger in adminTriggers)
        {
            Console.WriteLine($"{trigger.Trigger} - {trigger.Description}");
        }

        // Output the non-admin triggers
        Console.WriteLine("\nNon-Admin Triggers:");
        foreach (var trigger in nonAdminTriggers)
        {
            Console.WriteLine($"{trigger.Trigger} - {trigger.Description}");
        }

        Console.WriteLine("\n\n----------------------------------------------\n\n");
        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((context, config) =>
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

                var env = context.HostingEnvironment;

                // Register configurations
                foreach (var botConfig in botConfigurations)
                {
                    botConfig.ChatsFolder = new System.IO.DirectoryInfo(Path.Combine(env.ContentRootPath, "chats", botConfig.Name));
                    if (!botConfig.ChatsFolder.Exists) Directory.CreateDirectory(botConfig.ChatsFolder.FullName);
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
                services.AddTransient<IMessageInputProcessor, ConversationManagerInputProcessor>();
                services.AddTransient<IMessageInputProcessor, StartNewConversationInputProcessor>();
                services.AddTransient<IMessageInputProcessor, DiagnosticsInputProcessor>();
                services.AddTransient<IMessageInputProcessor, ModelManagerInputProcessor>();

                //services.AddTransient<IMessageInputProcessor, EchoUserTextInputProcessor>();
                services.AddTransient<IMessageOutputProcessor, BasicChatOutputProcessor>();

                services.AddHostedService<BotHostedService>();
            });
}

public static class AttributeScanner
{
    public static List<(Type ClassType, TriggerAttribute Trigger)> GetClassesWithTriggerAttribute()
    {
        var typesWithTriggerAttribute = new List<(Type, TriggerAttribute)>();

        // Loop through all assemblies in the current AppDomain
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Loop through all types in the assembly
            foreach (var type in assembly.GetTypes())
            {
                // Check if the type is a class and has the TriggerAttribute
                if (type.IsClass)
                {
                    var attributes = type.GetCustomAttributes(typeof(TriggerAttribute), inherit: true)
                                         .Cast<TriggerAttribute>();

                    foreach (var attribute in attributes)
                    {
                        typesWithTriggerAttribute.Add((type, attribute));
                    }
                }
            }
        }

        return typesWithTriggerAttribute;
    }

    public static Tuple<List<TriggerAttribute>, List<TriggerAttribute>> GetAdminAndNonAdminTriggers()
    {
        // Get all TriggerAttributes
        var allTriggers = GetClassesWithTriggerAttribute()
            .Select(t => t.Trigger)
            .ToList();

        // Use LINQ to filter the triggers by IsAdmin == true and IsAdmin == false
        var adminTriggers = allTriggers
            .Where(trigger => trigger.IsAdmin)
            .ToList();

        var nonAdminTriggers = allTriggers
            .Where(trigger => !trigger.IsAdmin)
            .ToList();

        // Return a tuple containing both lists
        return Tuple.Create(adminTriggers, nonAdminTriggers);
    }
}