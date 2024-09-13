using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ollabotica;

/// <summary>
/// This class will hold the configuration properties for each bot.
/// </summary>
public class BotConfiguration
{
    public string Name { get; set; }
    public string TelegramToken { get; set; }
    public string OllamaUrl { get; set; }
    public string OllamaToken { get; set; }
    public string DefaultModel { get; set; }
    public string NewChatPrompt { get; set; }

    // These will be loaded as strings, then parsed into lists of longs
    public string AllowedChatIdsRaw { get; set; }

    public string AdminChatIdsRaw { get; set; }

    public List<long> AllowedChatIds
    {
        get
        {
            return AllowedChatIdsRaw?.Split(',')
                .Select(id => long.Parse(id.Trim()))
                .ToList() ?? new List<long>();
        }
    }

    public List<long> AdminChatIds
    {
        get
        {
            return AdminChatIdsRaw?.Split(',')
                .Select(id => long.Parse(id.Trim()))
                .ToList() ?? new List<long>();
        }
    }
}