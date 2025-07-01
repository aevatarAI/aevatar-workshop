using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.Workshop.GAgent;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using Aevatar.GAgents.AIGAgent.Agent;

namespace Aevatar.Workshop.Tests;

[GenerateSerializer]
public class TestToolAIGAgentState : AIGAgentStateBase
{
    [Id(0)] public List<string> ProcessedTasks { get; set; } = [];
    [Id(1)] public Dictionary<string, string> TaskResults { get; set; } = new();
}

[GenerateSerializer]
public class TestToolAIGAgentStateLogEvent : StateLogEventBase<TestToolAIGAgentStateLogEvent>;

[GenerateSerializer]
public class TestTaskProcessedLogEvent : TestToolAIGAgentStateLogEvent
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public string Task { get; set; } = string.Empty;
    [Id(2)] public string Result { get; set; } = string.Empty;
}

public interface ITestToolAIGAgent : IAIGAgent, IGAgent
{
    Task<string> ProcessComplexInstructionAsync(string instruction);
}

[GAgent("testToolAI", "test")]
public class TestToolAIGAgent : ToolAIGAgentBase<TestToolAIGAgentState, TestToolAIGAgentStateLogEvent>, ITestToolAIGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test ToolAI GAgent that can intelligently process complex instructions using math and time conversion tools");
    }

    /// <summary>
    /// 处理复杂指令，自动识别需要使用的工具
    /// </summary>
    public async Task<string> ProcessComplexInstructionAsync(string instruction)
    {
        Logger.LogInformation("Processing complex instruction: {Instruction}", instruction);

        var taskId = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        // 记录任务开始
        RaiseEvent(new TestTaskProcessedLogEvent
        {
            TaskId = taskId,
            Task = instruction,
            Result = "Processing..."
        });
        await ConfirmEvents();

        // 直接使用原始指令，让基类处理工具调用逻辑
        var result = await ProcessComplexTaskWithToolsAsync(instruction);

        // 记录任务完成
        RaiseEvent(new TestTaskProcessedLogEvent
        {
            TaskId = taskId,
            Task = instruction,
            Result = result
        });
        await ConfirmEvents();

        Logger.LogInformation("Complex instruction completed. Task ID: {TaskId}", taskId);
        return result;
    }

    [EventHandler]
    public async Task HandleTestRequestAsync(GreetingEvent eventData)
    {
        if (!string.IsNullOrWhiteSpace(eventData.Greeting))
        {
            Logger.LogInformation("Received test request: {Request}", eventData.Greeting);
            var result = await ProcessComplexInstructionAsync(eventData.Greeting);
            
            // 发布结果
            await PublishAsync(new RecordEvent
            {
                Message = $"Test result: {result}"
            });
        }
    }



    /// <summary>
    /// 增强的事件创建方法，为测试场景优化
    /// </summary>
    protected override async Task<EventBase> CreateEventForGAgentAsync(GAgentInfo gagentInfo, string task)
    {
        // 对于测试场景，我们可以更智能地创建事件
        switch (gagentInfo.Alias.ToLower())
        {
            case "math":
                return new MathCalculateEvent { Expression = task };
                
            case "timeconverter":
                // 尝试解析时间转换请求
                if (task.Contains("convert", StringComparison.OrdinalIgnoreCase))
                {
                    return new TimeConvertEvent { TimeInput = task };
                }
                else
                {
                    // 使用GreetingEvent作为通用载体
                    return new GreetingEvent { Greeting = task };
                }
                
            default:
                return await base.CreateEventForGAgentAsync(gagentInfo, task);
        }
    }

    protected override void AIGAgentTransitionState(TestToolAIGAgentState state, StateLogEventBase<TestToolAIGAgentStateLogEvent> @event)
    {
        switch (@event)
        {
            case TestTaskProcessedLogEvent processed:
                state.ProcessedTasks.Add($"{processed.TaskId}: {processed.Task}");
                state.TaskResults[processed.TaskId] = processed.Result;
                break;
        }
    }
} 