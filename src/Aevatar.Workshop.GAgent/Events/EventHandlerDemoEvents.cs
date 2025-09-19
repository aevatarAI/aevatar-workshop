using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Workshop.GAgent.Events;

/// <summary>
/// Notification event - Used to send notification messages
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
/// Data processing event - Used to trigger data processing tasks
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
/// Coordination request event - Used to request multiple GAgents to work together
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
/// Coordination response event - GAgent's response to coordination request
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
/// Event record event - Used to record the processing of other events
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
/// Processing completed event - Notification after data processing is completed
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