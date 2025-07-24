using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Workshop.GAgent.GAgents.Demo;

[GenerateSerializer]
public class CoordinatorDemoGAgentState : StateBase
{
    [Id(0)] public List<CoordinationTask> CoordinationTasks { get; set; } = new();
    [Id(1)] public Dictionary<string, List<string>> AgentResponses { get; set; } = new();
    [Id(2)] public int TotalCoordinationRequests { get; set; }
}

[GenerateSerializer]
public class CoordinationTask
{
    [Id(0)] public string RequestId { get; set; } = string.Empty;
    [Id(1)] public string TaskName { get; set; } = string.Empty;
    [Id(2)] public List<string> RequiredAgents { get; set; } = new();
    [Id(3)] public Dictionary<string, bool> AgentAcceptance { get; set; } = new();
    [Id(4)] public DateTime CreatedAt { get; set; }
    [Id(5)] public DateTime? CompletedAt { get; set; }
    [Id(6)] public bool IsComplete { get; set; }
}

[GenerateSerializer]
public class CoordinatorDemoStateLogEvent : StateLogEventBase<CoordinatorDemoStateLogEvent>
{
}

public interface ICoordinatorDemoGAgent : IStateGAgent<CoordinatorDemoGAgentState>
{
    
}

[GAgent("coordinator-demo", "workshop")]
public class CoordinatorDemoGAgent : GAgentBase<CoordinatorDemoGAgentState, CoordinatorDemoStateLogEvent>, ICoordinatorDemoGAgent
{
    private readonly ILogger<CoordinatorDemoGAgent> _logger;
    private readonly IGrainFactory _grainFactory;

    public CoordinatorDemoGAgent(ILogger<CoordinatorDemoGAgent> logger, IGrainFactory grainFactory)
    {
        _logger = logger;
        _grainFactory = grainFactory;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("CoordinatorDemoGAgent - 协调多个GAgent协同工作的演示GAgent");
    }

    /// <summary>
    /// 处理协调请求事件
    /// </summary>
    [EventHandler]
    public async Task HandleCoordinationRequestAsync(CoordinationRequestEvent @event)
    {
        _logger.LogInformation("收到协调请求 {RequestId}: {TaskName}",
            @event.RequestId, @event.TaskName);

        var task = new CoordinationTask
        {
            RequestId = @event.RequestId,
            TaskName = @event.TaskName,
            RequiredAgents = @event.RequiredAgents,
            CreatedAt = DateTime.UtcNow
        };

        State.CoordinationTasks.Add(task);
        State.TotalCoordinationRequests++;

        // 记录事件
        await PublishAsync(new EventLoggedEvent
        {
            SourceAgent = this.GetGrainId().ToString(),
            EventType = nameof(CoordinationRequestEvent),
            EventData = $"Coordinating {task.TaskName} with {task.RequiredAgents.Count} agents",
            Success = true
        });

        // 开始协调流程
        await StartCoordinationAsync(@event, task);
    }

    /// <summary>
    /// 处理协调响应事件
    /// </summary>
    [EventHandler]
    public async Task HandleCoordinationResponseAsync(CoordinationResponseEvent @event)
    {
        var task = State.CoordinationTasks.FirstOrDefault(t => t.RequestId == @event.RequestId);
        if (task != null)
        {
            task.AgentAcceptance[@event.RespondingAgent] = @event.Accepted;

            if (!State.AgentResponses.ContainsKey(@event.RequestId))
            {
                State.AgentResponses[@event.RequestId] = new List<string>();
            }
            State.AgentResponses[@event.RequestId].Add(@event.RespondingAgent);

            _logger.LogInformation("收到 {Agent} 对任务 {RequestId} 的响应: {Accepted}",
                @event.RespondingAgent, @event.RequestId, @event.Accepted ? "接受" : "拒绝");

            // 检查是否所有agent都已响应
            if (task.AgentAcceptance.Count == task.RequiredAgents.Count)
            {
                await CompleteCoordinationAsync(task);
            }
        }
    }

    private async Task StartCoordinationAsync(CoordinationRequestEvent request, CoordinationTask task)
    {
        // 模拟向所需的agent发送协调请求
        foreach (var agentName in request.RequiredAgents)
        {
            try
            {
                // 发送通知给各个agent
                await PublishAsync(new NotificationEvent
                {
                    Title = $"协调请求: {task.TaskName}",
                    Message = $"需要您参与任务: {task.TaskName}",
                    Level = NotificationLevel.Info,
                    Source = this.GetGrainId().ToString()
                });

                // 如果是处理任务，触发数据处理
                if (request.Parameters.ContainsKey("ProcessData") && 
                    request.Parameters["ProcessData"] == "true")
                {
                    await PublishAsync(new DataProcessingEvent
                    {
                        DataType = $"Coordination-{task.TaskName}",
                        Data = new Dictionary<string, object>
                        {
                            ["RequestId"] = request.RequestId,
                            ["Agent"] = agentName,
                            ["Parameters"] = request.Parameters
                        },
                        Priority = ProcessingPriority.High
                    });
                }

                // 模拟agent响应（实际应用中，其他agent会发送响应事件）
                await Task.Delay(Random.Shared.Next(100, 500));
                await SimulateAgentResponseAsync(request.RequestId, agentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "协调 {Agent} 时发生错误", agentName);
            }
        }
    }

    private async Task SimulateAgentResponseAsync(string requestId, string agentName)
    {
        // 模拟agent响应（80%概率接受）
        var accepted = Random.Shared.Next(100) < 80;
        
        await PublishAsync(new CoordinationResponseEvent
        {
            RequestId = requestId,
            RespondingAgent = agentName,
            Accepted = accepted,
            Reason = accepted ? null : "Agent is busy"
        });
    }

    private async Task CompleteCoordinationAsync(CoordinationTask task)
    {
        task.CompletedAt = DateTime.UtcNow;
        task.IsComplete = true;

        var acceptedCount = task.AgentAcceptance.Count(kvp => kvp.Value);
        var success = acceptedCount == task.RequiredAgents.Count;

        _logger.LogInformation("协调任务 {RequestId} 完成: {AcceptedCount}/{TotalCount} agents接受",
            task.RequestId, acceptedCount, task.RequiredAgents.Count);

        // 发送完成通知
        await PublishAsync(new NotificationEvent
        {
            Title = $"协调完成: {task.TaskName}",
            Message = $"{acceptedCount}/{task.RequiredAgents.Count} agents参与任务",
            Level = success ? NotificationLevel.Success : NotificationLevel.Warning,
            Source = this.GetGrainId().ToString()
        });

        // 保持任务历史在最近20条
        if (State.CoordinationTasks.Count > 20)
        {
            var oldestTask = State.CoordinationTasks
                .Where(t => t.IsComplete)
                .OrderBy(t => t.CompletedAt)
                .FirstOrDefault();
            
            if (oldestTask != null)
            {
                State.CoordinationTasks.Remove(oldestTask);
                State.AgentResponses.Remove(oldestTask.RequestId);
            }
        }
    }

    /// <summary>
    /// 获取协调统计信息
    /// </summary>
    public Task<CoordinationStatistics> GetStatisticsAsync()
    {
        var stats = new CoordinationStatistics
        {
            TotalRequests = State.TotalCoordinationRequests,
            ActiveTasks = State.CoordinationTasks.Where(t => !t.IsComplete).ToList(),
            CompletedTasks = State.CoordinationTasks.Where(t => t.IsComplete)
                .OrderByDescending(t => t.CompletedAt)
                .Take(10)
                .ToList(),
            SuccessRate = State.CoordinationTasks.Count > 0 ?
                (double)State.CoordinationTasks.Count(t => t.IsComplete && 
                    t.AgentAcceptance.All(kvp => kvp.Value)) / State.CoordinationTasks.Count : 0
        };

        return Task.FromResult(stats);
    }

    /// <summary>
    /// 创建测试协调任务
    /// </summary>
    public async Task CreateTestCoordinationAsync(string taskName, List<string> agents)
    {
        var request = new CoordinationRequestEvent
        {
            TaskName = taskName,
            RequiredAgents = agents,
            Parameters = new Dictionary<string, string>
            {
                ["ProcessData"] = "true",
                ["TestMode"] = "true"
            }
        };

        await HandleCoordinationRequestAsync(request);
    }
}

[GenerateSerializer]
public class CoordinationStatistics
{
    [Id(0)] public int TotalRequests { get; set; }
    [Id(1)] public List<CoordinationTask> ActiveTasks { get; set; } = new();
    [Id(2)] public List<CoordinationTask> CompletedTasks { get; set; } = new();
    [Id(3)] public double SuccessRate { get; set; }
} 