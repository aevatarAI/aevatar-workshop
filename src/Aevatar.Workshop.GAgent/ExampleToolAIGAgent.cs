using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class ExampleToolAIGAgentState : AIGAgentStateBase
{
    [Id(0)] public List<string> TaskHistory { get; set; } = [];
    [Id(1)] public string CurrentTask { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ExampleToolAIGAgentStateLogEvent : StateLogEventBase<ExampleToolAIGAgentStateLogEvent>;

[GenerateSerializer]
public class NewTaskStateLogEvent : ExampleToolAIGAgentStateLogEvent
{
    [Id(0)] public string Task { get; set; } = string.Empty;
}

[GenerateSerializer]
public class TaskCompletedStateLogEvent : ExampleToolAIGAgentStateLogEvent
{
    [Id(0)] public string Task { get; set; } = string.Empty;
    [Id(1)] public string Result { get; set; } = string.Empty;
}

public interface IExampleToolAIGAgent : IAIGAgent, IGAgent
{
    Task<string> ProcessComplexTaskAsync(string task);
}

[GAgent("exampleToolAI", "demo")]
public class ExampleToolAIGAgent : ToolAIGAgentBase<ExampleToolAIGAgentState, ExampleToolAIGAgentStateLogEvent>,
    IExampleToolAIGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "This is an example ToolAIGAgent that can call other GAgents to complete complex tasks.");
    }

    /// <summary>
    /// 处理复杂任务，可能需要调用多个其他 GAgent
    /// </summary>
    public async Task<string> ProcessComplexTaskAsync(string task)
    {
        Logger.LogInformation("Processing complex task: {Task}", task);

        // 记录新任务
        RaiseEvent(new NewTaskStateLogEvent { Task = task });
        await ConfirmEvents();

                // 使用基类的工具增强处理方法
        var responseContent = await ProcessComplexTaskWithToolsAsync(task);

        // 记录任务完成
        RaiseEvent(new TaskCompletedStateLogEvent { Task = task, Result = responseContent });
        await ConfirmEvents();

        // 记录到系统日志
        try
        {
            await PublishAsync(new RecordEvent
            {
                Message = $"ExampleToolAIGAgent completed task: {task}"
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to record to system log");
        }

        Logger.LogInformation("Complex task completed: {Task}", task);
        return responseContent;
    }

    [EventHandler]
    public async Task HandleTaskRequestAsync(GreetingEvent greetingEvent)
    {
        Logger.LogInformation("Received task request: {Greeting}", greetingEvent.Greeting);

        var result = await ProcessComplexTaskAsync(greetingEvent.Greeting);

        // 发布结果事件
        await PublishAsync(new RecordEvent { Message = $"Task result: {result}" });
    }

    protected override void AIGAgentTransitionState(ExampleToolAIGAgentState state,
        StateLogEventBase<ExampleToolAIGAgentStateLogEvent> @event)
    {
        switch (@event)
        {
            case NewTaskStateLogEvent newTaskEvent:
                state.CurrentTask = newTaskEvent.Task;
                break;
            case TaskCompletedStateLogEvent completedEvent:
                state.TaskHistory.Add($"{completedEvent.Task} -> {completedEvent.Result}");
                state.CurrentTask = string.Empty;
                break;
        }
    }
}