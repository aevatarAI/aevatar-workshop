using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Workshop.GAgent.Events;

/// <summary>
/// 通知事件 - 用于发送通知消息
/// </summary>
[GenerateSerializer]
public class NotificationEvent : EventBase
{
    [Id(0)] public string Title { get; set; } = string.Empty;
    [Id(1)] public string Message { get; set; } = string.Empty;
    [Id(2)] public NotificationLevel Level { get; set; } = NotificationLevel.Info;
    [Id(3)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [Id(4)] public string? Source { get; set; }
}

[GenerateSerializer]
public enum NotificationLevel
{
    Info,
    Warning,
    Error,
    Success
}

/// <summary>
/// 数据处理事件 - 用于触发数据处理任务
/// </summary>
[GenerateSerializer]
public class DataProcessingEvent : EventBase
{
    [Id(0)] public string ProcessingId { get; set; } = Guid.NewGuid().ToString();
    [Id(1)] public string DataType { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, object> Data { get; set; } = new();
    [Id(3)] public ProcessingPriority Priority { get; set; } = ProcessingPriority.Normal;
}

[GenerateSerializer]
public enum ProcessingPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// 协调请求事件 - 用于请求多个GAgent协同工作
/// </summary>
[GenerateSerializer]
public class CoordinationRequestEvent : EventBase
{
    [Id(0)] public string RequestId { get; set; } = Guid.NewGuid().ToString();
    [Id(1)] public string TaskName { get; set; } = string.Empty;
    [Id(2)] public List<string> RequiredAgents { get; set; } = new();
    [Id(3)] public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// 协调响应事件 - GAgent对协调请求的响应
/// </summary>
[GenerateSerializer]
public class CoordinationResponseEvent : EventBase
{
    [Id(0)] public string RequestId { get; set; } = string.Empty;
    [Id(1)] public string RespondingAgent { get; set; } = string.Empty;
    [Id(2)] public bool Accepted { get; set; }
    [Id(3)] public string? Reason { get; set; }
}

/// <summary>
/// 事件记录事件 - 用于记录其他事件的处理
/// </summary>
[GenerateSerializer]
public class EventLoggedEvent : EventBase
{
    [Id(0)] public string SourceAgent { get; set; } = string.Empty;
    [Id(1)] public string EventType { get; set; } = string.Empty;
    [Id(2)] public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    [Id(3)] public string? EventData { get; set; }
    [Id(4)] public bool Success { get; set; } = true;
    [Id(5)] public string? ErrorMessage { get; set; }
}

/// <summary>
/// 处理完成事件 - 数据处理完成后的通知
/// </summary>
[GenerateSerializer]
public class ProcessingCompletedEvent : EventBase
{
    [Id(0)] public string ProcessingId { get; set; } = string.Empty;
    [Id(1)] public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    [Id(2)] public ProcessingResult Result { get; set; } = ProcessingResult.Success;
    [Id(3)] public Dictionary<string, object>? OutputData { get; set; }
    [Id(4)] public string? ErrorDetails { get; set; }
}

[GenerateSerializer]
public enum ProcessingResult
{
    Success,
    PartialSuccess,
    Failed,
    Cancelled
} 