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

        // 构建增强的提示词，明确告知LLM可用的工具
        var enhancedPrompt = $"""
            You are an intelligent assistant with access to specialized tools. 
            
            Available tools:
            1. **math.tools** - Mathematical calculation agent that can evaluate expressions and perform complex calculations
               - Use this for any mathematical operations, calculations, or numeric expressions
               - Example: "calculate 25 * 4 + 10", "what is the square root of 144"
            
            2. **timeconverter.tools** - Time conversion agent that can convert between time zones and perform time calculations
               - Use this for timezone conversions, time differences, or getting current time in different zones
               - Example: "convert 3pm EST to PST", "what time is it in Tokyo"
            
            Current task: {instruction}
            
            Analyze this task and determine if you need to use any of the specialized tools.
            If the task involves mathematical calculations, use the math tool.
            If the task involves time zones or time-related operations, use the timeconverter tool.
            You can use multiple tools if needed.
            
            Important: When calling tools, provide the task description as a simple, clear instruction.
            """;

        // 使用基类的工具增强处理方法
        var result = await ProcessComplexTaskWithToolsAsync(enhancedPrompt);

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
    /// 自定义工具描述构建，专门针对测试场景
    /// </summary>
    protected override string BuildToolDescription()
    {
        return """
            Available specialized tools:
            
            🧮 math.tools - Mathematical calculation agent
               Capabilities: Basic arithmetic, advanced functions (sqrt, sin, cos, log), expression evaluation
               Usage: Send mathematical expressions or calculation requests
            
            🕐 timeconverter.tools - Time conversion agent  
               Capabilities: Timezone conversion, time difference calculation, current time queries
               Usage: Send time-related queries with timezone information
            
            Examples:
            - "Calculate the compound interest on $1000 at 5% for 3 years"
            - "What time will it be in Tokyo when it's 3pm in New York?"
            - "Convert 2:30 PM PST to EST and then calculate how many hours until midnight"
            """;
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