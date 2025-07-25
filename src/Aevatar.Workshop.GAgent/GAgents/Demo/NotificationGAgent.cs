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
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("NotificationGAgent - Demo GAgent for handling and logging notification events");
    }

    /// <summary>
    /// Main handler for notification events
    /// </summary>
    [EventHandler]
    public async Task HandleNotificationAsync(NotificationEvent @event)
    {
        Logger.LogInformation("Received notification: {Title} - {Message} (Level: {Level})", 
            @event.Title, @event.Message, @event.Level);

        // Record notification
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

        // Update count
        if (!State.NotificationCounts.ContainsKey(@event.Level))
        {
            State.NotificationCounts[@event.Level] = 0;
        }
        State.NotificationCounts[@event.Level]++;

        // Keep history to last 100 records
        if (State.NotificationHistory.Count > 100)
        {
            State.NotificationHistory.RemoveAt(0);
        }

        // Send event log
        await PublishAsync(new EventLoggedEvent
        {
            SourceAgent = this.GetGrainId().ToString(),
            EventType = nameof(NotificationEvent),
            EventData = $"{@event.Title}: {@event.Message}",
            Success = true
        });

        // If error level, may need to trigger other actions
        if (@event.Level == NotificationLevel.Error)
        {
            await HandleErrorNotificationAsync(@event);
        }
    }

    /// <summary>
    /// Generic handler for all events (to demonstrate [AllEventHandler])
    /// </summary>
    [AllEventHandler]
    public Task LogAllEventsAsync(EventWrapperBase eventWrapper)
    {
        if (eventWrapper is not EventWrapper<EventBase> typedWrapper)
        {
            return Task.CompletedTask;
        }

        Logger.LogDebug("NotificationGAgent received event: {EventType}",
            typedWrapper.Event.GetType().Name ?? "Unknown");
        return Task.CompletedTask;
    }

    private async Task HandleErrorNotificationAsync(NotificationEvent errorEvent)
    {
        Logger.LogWarning("Processing error notification: {Title}", errorEvent.Title);
        
        // Can trigger data processing event to log errors
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
    /// Get notification statistics
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
    /// Clear history
    /// </summary>
    public Task ClearHistoryAsync()
    {
        State.NotificationHistory.Clear();
        State.TotalNotifications = 0;
        State.NotificationCounts.Clear();
        
        Logger.LogInformation("Notification history cleared");
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