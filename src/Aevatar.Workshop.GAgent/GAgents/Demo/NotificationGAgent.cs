using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Workshop.GAgent.GAgents.Demo;

[GenerateSerializer]
public class NotificationGAgentState : StateBase
{
    [Id(0)] public List<NotificationRecord> NotificationHistory { get; set; } = new();
    [Id(1)] public int TotalNotifications { get; set; }
    [Id(2)] public Dictionary<NotificationLevel, int> NotificationCounts { get; set; } = new();
}

[GenerateSerializer]
public class NotificationRecord
{
    [Id(0)] public string Title { get; set; } = string.Empty;
    [Id(1)] public string Message { get; set; } = string.Empty;
    [Id(2)] public NotificationLevel Level { get; set; }
    [Id(3)] public DateTime ReceivedAt { get; set; }
    [Id(4)] public string? Source { get; set; }
}

[GenerateSerializer]
public class NotificationStateLogEvent : StateLogEventBase<NotificationStateLogEvent>
{
}

public interface INotificationGAgent : IStateGAgent<NotificationGAgentState>
{
    
}

[GAgent("notification-demo", "workshop")]
public class NotificationGAgent : GAgentBase<NotificationGAgentState, NotificationStateLogEvent>, INotificationGAgent
{
    private readonly ILogger<NotificationGAgent> _logger;

    public NotificationGAgent(ILogger<NotificationGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("NotificationGAgent - 处理和记录通知事件的演示GAgent");
    }

    /// <summary>
    /// 处理通知事件的主要handler
    /// </summary>
    [EventHandler]
    public async Task HandleNotificationAsync(NotificationEvent @event)
    {
        _logger.LogInformation("收到通知: {Title} - {Message} (级别: {Level})", 
            @event.Title, @event.Message, @event.Level);

        // 记录通知
        var record = new NotificationRecord
        {
            Title = @event.Title,
            Message = @event.Message,
            Level = @event.Level,
            ReceivedAt = @event.Timestamp,
            Source = @event.Source
        };

        State.NotificationHistory.Add(record);
        State.TotalNotifications++;

        // 更新计数
        if (!State.NotificationCounts.ContainsKey(@event.Level))
        {
            State.NotificationCounts[@event.Level] = 0;
        }
        State.NotificationCounts[@event.Level]++;

        // 保持历史记录在最近100条
        if (State.NotificationHistory.Count > 100)
        {
            State.NotificationHistory.RemoveAt(0);
        }

        // 发送事件记录
        await PublishAsync(new EventLoggedEvent
        {
            SourceAgent = this.GetGrainId().ToString(),
            EventType = nameof(NotificationEvent),
            EventData = $"{@event.Title}: {@event.Message}",
            Success = true
        });

        // 如果是错误级别，可能需要触发其他操作
        if (@event.Level == NotificationLevel.Error)
        {
            await HandleErrorNotificationAsync(@event);
        }
    }

    /// <summary>
    /// 处理所有事件的通用handler（用于演示[AllEventHandler]）
    /// </summary>
    [AllEventHandler]
    public Task LogAllEventsAsync(EventWrapperBase eventWrapper)
    {
        if (eventWrapper is not EventWrapper<EventBase> typedWrapper)
        {
            return Task.CompletedTask;
        }

        _logger.LogDebug("NotificationGAgent收到事件: {EventType}",
            typedWrapper.Event.GetType().Name ?? "Unknown");
        return Task.CompletedTask;
    }

    private async Task HandleErrorNotificationAsync(NotificationEvent errorEvent)
    {
        _logger.LogWarning("处理错误通知: {Title}", errorEvent.Title);
        
        // 可以触发数据处理事件来记录错误
        await PublishAsync(new DataProcessingEvent
        {
            DataType = "ErrorLog",
            Data = new Dictionary<string, object>
            {
                ["Title"] = errorEvent.Title,
                ["Message"] = errorEvent.Message,
                ["Timestamp"] = errorEvent.Timestamp.ToString("O")
            },
            Priority = ProcessingPriority.High
        });
    }

    /// <summary>
    /// 获取通知统计信息
    /// </summary>
    public Task<NotificationStatistics> GetStatisticsAsync()
    {
        var stats = new NotificationStatistics
        {
            TotalNotifications = State.TotalNotifications,
            NotificationCounts = new Dictionary<NotificationLevel, int>(State.NotificationCounts),
            RecentNotifications = State.NotificationHistory
                .OrderByDescending(n => n.ReceivedAt)
                .Take(10)
                .ToList()
        };

        return Task.FromResult(stats);
    }

    /// <summary>
    /// 清除历史记录
    /// </summary>
    public Task ClearHistoryAsync()
    {
        State.NotificationHistory.Clear();
        State.TotalNotifications = 0;
        State.NotificationCounts.Clear();
        
        _logger.LogInformation("通知历史已清除");
        return Task.CompletedTask;
    }
}

[GenerateSerializer]
public class NotificationStatistics
{
    [Id(0)] public int TotalNotifications { get; set; }
    [Id(1)] public Dictionary<NotificationLevel, int> NotificationCounts { get; set; } = new();
    [Id(2)] public List<NotificationRecord> RecentNotifications { get; set; } = new();
} 