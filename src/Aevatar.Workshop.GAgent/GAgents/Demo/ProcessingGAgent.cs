using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Workshop.GAgent.GAgents.Demo;

[GenerateSerializer]
public class ProcessingGAgentState : StateBase
{
    [Id(0)] public List<ProcessingTask> ActiveTasks { get; set; } = new();
    [Id(1)] public List<ProcessingTask> CompletedTasks { get; set; } = new();
    [Id(2)] public int TotalTasksProcessed { get; set; }
    [Id(3)] public Dictionary<ProcessingPriority, int> TasksByPriority { get; set; } = new();
}

[GenerateSerializer]
public class ProcessingTask
{
    [Id(0)] public string ProcessingId { get; set; } = string.Empty;
    [Id(1)] public string DataType { get; set; } = string.Empty;
    [Id(2)] public ProcessingPriority Priority { get; set; }
    [Id(3)] public DateTime StartedAt { get; set; }
    [Id(4)] public DateTime? CompletedAt { get; set; }
    [Id(5)] public ProcessingResult? Result { get; set; }
    [Id(6)] public string? ErrorMessage { get; set; }
}

[GenerateSerializer]
public class ProcessingStateLogEvent : StateLogEventBase<ProcessingStateLogEvent>
{
}

public interface IProcessingGAgent : IStateGAgent<ProcessingGAgentState>;

[GAgent("processing-demo", "workshop")]
public class ProcessingGAgent : GAgentBase<ProcessingGAgentState, ProcessingStateLogEvent>
{
    private readonly ILogger<ProcessingGAgent> _logger;

    public ProcessingGAgent(ILogger<ProcessingGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("ProcessingGAgent - 处理数据处理任务的演示GAgent，支持优先级队列");
    }

    /// <summary>
    /// 处理数据处理事件
    /// </summary>
    [EventHandler]
    public async Task HandleDataProcessingAsync(DataProcessingEvent @event)
    {
        _logger.LogInformation("开始处理任务 {ProcessingId}, 类型: {DataType}, 优先级: {Priority}",
            @event.ProcessingId, @event.DataType, @event.Priority);

        // 创建处理任务
        var task = new ProcessingTask
        {
            ProcessingId = @event.ProcessingId,
            DataType = @event.DataType,
            Priority = @event.Priority,
            StartedAt = DateTime.UtcNow
        };

        State.ActiveTasks.Add(task);
        
        // 更新优先级统计
        if (!State.TasksByPriority.ContainsKey(@event.Priority))
        {
            State.TasksByPriority[@event.Priority] = 0;
        }
        State.TasksByPriority[@event.Priority]++;

        // 发送事件记录
        await PublishAsync(new EventLoggedEvent
        {
            SourceAgent = this.GetGrainId().ToString(),
            EventType = nameof(DataProcessingEvent),
            EventData = $"Processing {task.DataType} with priority {task.Priority}",
            Success = true
        });

        // 模拟异步处理
        await ProcessDataAsync(@event, task);
    }

    /// <summary>
    /// 处理处理完成事件
    /// </summary>
    [EventHandler]
    public async Task HandleProcessingCompletedAsync(ProcessingCompletedEvent @event)
    {
        var task = State.ActiveTasks.FirstOrDefault(t => t.ProcessingId == @event.ProcessingId);
        if (task != null)
        {
            task.CompletedAt = @event.CompletedAt;
            task.Result = @event.Result;
            task.ErrorMessage = @event.ErrorDetails;

            State.ActiveTasks.Remove(task);
            State.CompletedTasks.Add(task);
            State.TotalTasksProcessed++;

            // 保持完成任务历史在最近50条
            if (State.CompletedTasks.Count > 50)
            {
                State.CompletedTasks.RemoveAt(0);
            }

            _logger.LogInformation("任务 {ProcessingId} 完成，结果: {Result}",
                @event.ProcessingId, @event.Result);

            // 发送通知
            await PublishAsync(new NotificationEvent
            {
                Title = $"任务完成: {task.DataType}",
                Message = $"处理结果: {@event.Result}",
                Level = @event.Result == ProcessingResult.Success ? 
                    NotificationLevel.Success : NotificationLevel.Warning,
                Source = this.GetGrainId().ToString()
            });
        }
    }

    /// <summary>
    /// 默认事件处理器（演示HandleEventAsync方法名约定）
    /// </summary>
    public Task HandleEventAsync(EventBase @event)
    {
        _logger.LogDebug("ProcessingGAgent收到通用事件: {EventType}", @event.GetType().Name);
        return Task.CompletedTask;
    }

    private async Task ProcessDataAsync(DataProcessingEvent @event, ProcessingTask task)
    {
        try
        {
            // 模拟处理时间（基于优先级）
            var delay = @event.Priority switch
            {
                ProcessingPriority.Critical => 100,
                ProcessingPriority.High => 500,
                ProcessingPriority.Normal => 1000,
                ProcessingPriority.Low => 2000,
                _ => 1000
            };

            await Task.Delay(delay);

            // 模拟处理逻辑
            var outputData = new Dictionary<string, object>
            {
                ["ProcessedCount"] = @event.Data.Count,
                ["ProcessingTime"] = delay,
                ["DataType"] = @event.DataType
            };

            // 发送完成事件
            await PublishAsync(new ProcessingCompletedEvent
            {
                ProcessingId = @event.ProcessingId,
                CompletedAt = DateTime.UtcNow,
                Result = ProcessingResult.Success,
                OutputData = outputData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理任务 {ProcessingId} 时发生错误", @event.ProcessingId);
            
            await PublishAsync(new ProcessingCompletedEvent
            {
                ProcessingId = @event.ProcessingId,
                CompletedAt = DateTime.UtcNow,
                Result = ProcessingResult.Failed,
                ErrorDetails = ex.Message
            });
        }
    }

    /// <summary>
    /// 获取处理统计信息
    /// </summary>
    public Task<ProcessingStatistics> GetStatisticsAsync()
    {
        var stats = new ProcessingStatistics
        {
            ActiveTasksCount = State.ActiveTasks.Count,
            CompletedTasksCount = State.TotalTasksProcessed,
            TasksByPriority = new Dictionary<ProcessingPriority, int>(State.TasksByPriority),
            ActiveTasks = State.ActiveTasks.OrderBy(t => t.Priority).ToList(),
            RecentCompletedTasks = State.CompletedTasks
                .OrderByDescending(t => t.CompletedAt)
                .Take(10)
                .ToList()
        };

        return Task.FromResult(stats);
    }
}

[GenerateSerializer]
public class ProcessingStatistics
{
    [Id(0)] public int ActiveTasksCount { get; set; }
    [Id(1)] public int CompletedTasksCount { get; set; }
    [Id(2)] public Dictionary<ProcessingPriority, int> TasksByPriority { get; set; } = new();
    [Id(3)] public List<ProcessingTask> ActiveTasks { get; set; } = new();
    [Id(4)] public List<ProcessingTask> RecentCompletedTasks { get; set; } = new();
} 