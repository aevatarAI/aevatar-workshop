using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Microsoft.Extensions.Logging;

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
public class ExampleToolAIGAgent : ToolAIGAgent<ExampleToolAIGAgentState, ExampleToolAIGAgentStateLogEvent>, IExampleToolAIGAgent
{
    public ExampleToolAIGAgent(IGAgentFactory gAgentFactory, IClusterClient clusterClient) 
        : base(gAgentFactory, clusterClient)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is an example ToolAIGAgent that can call other GAgents to complete complex tasks.");
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

        // 使用工具增强的 LLM 处理任务
        var prompt = $"""
            You are an intelligent AI agent that can coordinate with other agents to complete complex tasks.
            
            Current task: {task}
            
            Please analyze this task and determine what steps are needed. You can use the following tools:
            - research: For gathering information and research
            - write: For creating content and reports  
            - record: For logging important information
            - call_gagent: For calling any specific GAgent
            
            Break down the task and execute it step by step using the available tools.
            Provide a comprehensive response with the final result.
            """;

        var result = await ChatWithToolsAsync(prompt);

        // 记录任务完成
        RaiseEvent(new TaskCompletedStateLogEvent { Task = task, Result = result });
        await ConfirmEvents();

        // 记录到系统日志
        await CallGAgentAsync("recorder", "demo", new RecordEvent 
        { 
            Message = $"ExampleToolAIGAgent completed task: {task}" 
        });

        Logger.LogInformation("Complex task completed: {Task}", task);
        return result;
    }

    [EventHandler]
    public async Task HandleTaskRequestAsync(GreetingEvent greetingEvent)
    {
        Logger.LogInformation("Received task request: {Greeting}", greetingEvent.Greeting);
        
        var result = await ProcessComplexTaskAsync(greetingEvent.Greeting);
        
        // 发布结果事件
        await PublishAsync(new RecordEvent { Message = $"Task result: {result}" });
    }

    /// <summary>
    /// 注册自定义工具
    /// </summary>
    protected override async Task RegisterCustomToolsAsync()
    {
        // 这里可以注册额外的自定义工具
        // 例如：Kernel.Plugins.AddFromObject(new CustomToolPlugin(), "custom_tools");
        
        Logger.LogInformation("Custom tools registered for ExampleToolAIGAgent");
        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取自定义工具描述
    /// </summary>
    protected override Task<string> GetCustomToolsDescriptionAsync()
    {
        return Task.FromResult("""
            Custom Tools:
            - complex_analysis: Perform complex multi-step analysis
            - coordinate_agents: Coordinate multiple agents for a task
            """);
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