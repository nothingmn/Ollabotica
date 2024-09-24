using Microsoft.AspNetCore.Mvc.ActionConstraints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ollabotica.OutputProcessors.Assistant.Actions;

public interface ITaskAction
{
    Task<string> CreateTimer(CreateTimer task, BotConfiguration botConfiguration);
}
public class CreateTimerAction : ITaskAction
{
    private readonly Api api;

    public CreateTimerAction(Api api)
    {
        this.api = api;
    }
    public async Task<string> CreateTimer(CreateTimer task, BotConfiguration botConfiguration)
    {
        await this.api.Init(botConfiguration.TaskEndPoint);
        return await this.api.CreateTimer(task);
    }
}
