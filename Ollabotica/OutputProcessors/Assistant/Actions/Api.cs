using Microsoft.Extensions.Logging;
using Slack.NetStandard.Messages.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Ollabotica.OutputProcessors.Assistant.Actions;
public class Api
{
    private readonly ILogger<Api> logger;
    HttpClient client = new HttpClient();
    string url = string.Empty;

    public Api(ILogger<Api> logger)
    {
        this.logger = logger;
    }

    public Task Init(string apiUrl)
    {
        url = apiUrl;
        return Task.CompletedTask;

    }
    public async Task<string> CreateTimer(CreateTimer createTimer) {
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(createTimer), Encoding.UTF8, "application/json");
        // Make the POST request
        HttpResponseMessage response = await client.PostAsync(url, content);

        // Check the response status code
        if (response.IsSuccessStatusCode)
        {
            // Read the response body as a string (if needed)
            string responseBody = await response.Content.ReadAsStringAsync();
            logger.LogInformation($"Response: {responseBody}");
            return responseBody;
        }
        else
        {
            logger.LogError($"Request failed. Status code: {response.StatusCode}");
        }
        return null;
    }
}
