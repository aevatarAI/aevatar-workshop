using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent.GAgents.Demo;

[GenerateSerializer]
public class EventLoggerGAgentState : StateBase
{
    [Id(0)] public List<EventLogRecord> EventLogs { get; set; } = new();
    [Id(1)] public Dictionary<string, int> EventTypeCounts { get; set; } = new();
    [Id(2)] public Dictionary<string, int> SourceAgentCounts { get; set; } = new();
    [Id(3)] public int TotalEventsLogged { get; set; }
    [Id(4)] public DateTime? FirstEventTime { get; set; }
    [Id(5)] public DateTime? LastEventTime { get; set; }
}

[GenerateSerializer]
public class EventLogRecord
{
    [Id(0)] public string EventId { get; set; } = Guid.NewGuid().ToString();
    [Id(1)] public string EventType { get; set; } = string.Empty;
    [Id(2)] public string SourceAgent { get; set; } = string.Empty;
    [Id(3)] public DateTime LoggedAt { get; set; }
    [Id(4)] public string? EventData { get; set; }
    [Id(5)] public bool Success { get; set; }
    [Id(6)] public string? ErrorMessage { get; set; }
}

[GenerateSerializer]
public class EventLoggerStateLogEvent : StateLogEventBase<EventLoggerStateLogEvent>
{
}

public interface IEventLoggerGAgent : IStateGAgent<EventLoggerGAgentState>
{
    Task<List<EventLogRecord>> SearchEventsAsync(
        string? eventType = null,
        string? sourceAgent = null,
        DateTime? startTime = null,
        DateTime? endTime = null);
}

[GAgent("event-logger-demo", "workshop")]
public class EventLoggerGAgent : GAgentBase<EventLoggerGAgentState, EventLoggerStateLogEvent>, IEventLoggerGAgent
{
    private readonly ILogger<EventLoggerGAgent> _logger;

    public EventLoggerGAgent(ILogger<EventLoggerGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("EventLoggerGAgent - 记录和分析系统中所有事件的演示GAgent");
    }

    /// <summary>
    /// 处理事件记录事件（专门处理）
    /// </summary>
    [EventHandler]
    public Task HandleEventLoggedAsync(EventLoggedEvent @event)
    {
        _logger.LogInformation("记录事件: {EventType} from {SourceAgent}", 
            @event.EventType, @event.SourceAgent);

        var record = new EventLogRecord
        {
            EventType = @event.EventType,
            SourceAgent = @event.SourceAgent,
            LoggedAt = @event.ProcessedAt,
            EventData = @event.EventData,
            Success = @event.Success,
            ErrorMessage = @event.ErrorMessage
        };

        AddEventLog(record);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 记录所有事件（使用AllEventHandler展示全局事件处理）
    /// </summary>
    [AllEventHandler(allowSelfHandling: true)]
    public Task LogAllEventsAsync(EventWrapperBase eventWrapper)
    {
        if (eventWrapper is not EventWrapper<EventBase> typedWrapper)
        {
            return Task.CompletedTask;
        }

        if (typedWrapper.Event is EventBase eventBase)
        {
            var eventType = eventBase.GetType().Name;
            
            // 避免重复记录EventLoggedEvent
            if (eventBase is EventLoggedEvent eventLogged)
            {
                // 已经在专门的handler中处理了
                return Task.CompletedTask;
            }

            _logger.LogDebug("捕获事件: {EventType} from {SenderId}", 
                eventType, typedWrapper.PublisherGrainId.ToString());

            var record = new EventLogRecord
            {
                EventType = eventType,
                SourceAgent = typedWrapper.PublisherGrainId.ToString() ?? "Unknown",
                LoggedAt = DateTime.UtcNow,
                EventData = GetEventSummary(eventBase),
                Success = true
            };

            AddEventLog(record);
        }

        return Task.CompletedTask;
    }

    private void AddEventLog(EventLogRecord record)
    {
        State.EventLogs.Add(record);
        State.TotalEventsLogged++;

        // 更新统计
        if (!State.EventTypeCounts.ContainsKey(record.EventType))
        {
            State.EventTypeCounts[record.EventType] = 0;
        }
        State.EventTypeCounts[record.EventType]++;

        if (!State.SourceAgentCounts.ContainsKey(record.SourceAgent))
        {
            State.SourceAgentCounts[record.SourceAgent] = 0;
        }
        State.SourceAgentCounts[record.SourceAgent]++;

        // 更新时间戳
        State.FirstEventTime ??= record.LoggedAt;
        State.LastEventTime = record.LoggedAt;

        // 保持日志在最近200条
        if (State.EventLogs.Count > 200)
        {
            State.EventLogs.RemoveAt(0);
        }
    }

    private string GetEventSummary(EventBase eventBase)
    {
        return eventBase switch
        {
            NotificationEvent notification => $"{notification.Title}: {notification.Message}",
            DataProcessingEvent processing => $"Processing {processing.DataType} (Priority: {processing.Priority})",
            CoordinationRequestEvent coordination => $"Coordinating {coordination.TaskName}",
            CoordinationResponseEvent response => $"Response: {(response.Accepted ? "Accepted" : "Rejected")}",
            ProcessingCompletedEvent completed => $"Completed with result: {completed.Result}",
            _ => eventBase.GetType().Name
        };
    }

    /// <summary>
    /// 获取事件日志统计信息
    /// </summary>
    public Task<EventLogStatistics> GetStatisticsAsync()
    {
        var stats = new EventLogStatistics
        {
            TotalEventsLogged = State.TotalEventsLogged,
            EventTypeCounts = State.EventTypeCounts
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            SourceAgentCounts = State.SourceAgentCounts
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            RecentEvents = State.EventLogs
                .OrderByDescending(e => e.LoggedAt)
                .Take(20)
                .ToList(),
            FirstEventTime = State.FirstEventTime,
            LastEventTime = State.LastEventTime
        };

        // 计算事件频率
        if (State.FirstEventTime.HasValue && State.LastEventTime.HasValue)
        {
            var duration = State.LastEventTime.Value - State.FirstEventTime.Value;
            if (duration.TotalMinutes > 0)
            {
                stats.EventsPerMinute = State.TotalEventsLogged / duration.TotalMinutes;
            }
        }

        return Task.FromResult(stats);
    }

    /// <summary>
    /// 搜索事件日志
    /// </summary>
    public Task<List<EventLogRecord>> SearchEventsAsync(
        string? eventType = null, 
        string? sourceAgent = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        var query = State.EventLogs.AsEnumerable();

        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(e => e.EventType.Contains(eventType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(sourceAgent))
        {
            query = query.Where(e => e.SourceAgent.Contains(sourceAgent, StringComparison.OrdinalIgnoreCase));
        }

        if (startTime.HasValue)
        {
            query = query.Where(e => e.LoggedAt >= startTime.Value);
        }

        if (endTime.HasValue)
        {
            query = query.Where(e => e.LoggedAt <= endTime.Value);
        }

        return Task.FromResult(query.OrderByDescending(e => e.LoggedAt).Take(50).ToList());
    }

    /// <summary>
    /// 清除事件日志
    /// </summary>
    public Task ClearLogsAsync()
    {
        State.EventLogs.Clear();
        State.EventTypeCounts.Clear();
        State.SourceAgentCounts.Clear();
        State.TotalEventsLogged = 0;
        State.FirstEventTime = null;
        State.LastEventTime = null;

        _logger.LogInformation("事件日志已清除");
        return Task.CompletedTask;
    }
}

[GenerateSerializer]
public class EventLogStatistics
{
    [Id(0)] public int TotalEventsLogged { get; set; }
    [Id(1)] public Dictionary<string, int> EventTypeCounts { get; set; } = new();
    [Id(2)] public Dictionary<string, int> SourceAgentCounts { get; set; } = new();
    [Id(3)] public List<EventLogRecord> RecentEvents { get; set; } = new();
    [Id(4)] public DateTime? FirstEventTime { get; set; }
    [Id(5)] public DateTime? LastEventTime { get; set; }
    [Id(6)] public double EventsPerMinute { get; set; }
} 