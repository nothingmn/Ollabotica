using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ollabotica;

public enum ServiceTypes
{
    Telegram,
    Slack,
    Discord
}

/// <summary>
/// This class will hold the configuration properties for each bot.
/// </summary>
public class BotConfiguration
{
    public string Name { get; set; }
    public ServiceTypes ServiceType { get; set; } = ServiceTypes.Telegram;
    public string ChatAuthToken { get; set; }
    public string OllamaUrl { get; set; }
    public string OllamaToken { get; set; }
    public string DefaultModel { get; set; }
    public string NewChatPrompt { get; set; }
    public System.IO.DirectoryInfo ChatsFolder { get; set; }

    // These will be loaded as strings, then parsed into lists of longs
    public string AllowedChatIdsRaw { get; set; }

    public string AdminChatIdsRaw { get; set; }

    public List<long> AllowedChatIdsAsLong
    {
        get
        {
            return AllowedChatIdsRaw?.Split(',')
                .Select(id => long.Parse(id.Trim()))
                .ToList() ?? new List<long>();
        }
    }

    public List<long> AdminChatIdsAsLong
    {
        get
        {
            return AdminChatIdsRaw?.Split(',')
                .Select(id => long.Parse(id.Trim()))
                .ToList() ?? new List<long>();
        }
    }

    public List<string> AllowedChatIds
    {
        get
        {
            return AllowedChatIdsRaw?.Split(',')
                .Select(id => id.Trim())
                .ToList() ?? new List<string>();
        }
    }

    public List<string> AdminChatIds
    {
        get
        {
            return AdminChatIdsRaw?.Split(',')
                .Select(id => id.Trim())
                .ToList() ?? new List<string>();
        }
    }
}