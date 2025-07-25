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

// Specific state change events
[GenerateSerializer]
public class EventLogAddedEvent : EventLoggerStateLogEvent
{
    [Id(0)] public EventLogRecord Record { get; set; } = null!;
}

[GenerateSerializer]
public class EventLogsClearedEvent : EventLoggerStateLogEvent
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
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("EventLoggerGAgent - Demo GAgent for logging and analyzing all events in the system");
    }

    /// <summary>
    /// Handle event logged events (dedicated handler)
    /// </summary>
    [EventHandler]
    public async Task HandleEventLoggedAsync(EventLoggedEvent @event)
    {
        Logger.LogInformation("Logging event: {EventType} from {SourceAgent}", 
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

        // Use event sourcing
        RaiseEvent(new EventLogAddedEvent { Record = record });
        await ConfirmEvents();
    }

    /// <summary>
    /// Log all events (demonstrates global event handling with AllEventHandler)
    /// </summary>
    [AllEventHandler(allowSelfHandling: true)]
    public async Task LogAllEventsAsync(EventWrapperBase eventWrapper)
    {
        // Extract the actual event using reflection due to generic covariance limitations
        var eventWrapperType = eventWrapper.GetType();
        var eventProperty = eventWrapperType.GetProperty("Event");
        var publisherProperty = eventWrapperType.GetProperty("PublisherGrainId");
        
        if (eventProperty == null || publisherProperty == null)
        {
            Logger.LogWarning("Unable to extract event information from EventWrapper");
            return;
        }

        var eventObj = eventProperty.GetValue(eventWrapper);
        var publisherIdObj = publisherProperty.GetValue(eventWrapper);
        
        if (eventObj is EventBase eventBase)
        {
            var eventType = eventBase.GetType().Name;
            
            // Avoid duplicate logging of EventLoggedEvent
            if (eventBase is EventLoggedEvent)
            {
                // Already handled in dedicated handler
                return;
            }

            var publisherIdString = publisherIdObj?.ToString() ?? "Unknown";
            
            Logger.LogDebug("Captured event: {EventType} from {SenderId}", 
                eventType, publisherIdString);

            var record = new EventLogRecord
            {
                EventType = eventType,
                SourceAgent = publisherIdString,
                LoggedAt = DateTime.UtcNow,
                EventData = GetEventSummary(eventBase),
                Success = true
            };

            // Use event sourcing
            RaiseEvent(new EventLogAddedEvent { Record = record });
            await ConfirmEvents();
        }
    }

    /// <summary>
    /// Override GAgentTransitionState to handle custom state transitions
    /// </summary>
    protected override void GAgentTransitionState(EventLoggerGAgentState state, StateLogEventBase<EventLoggerStateLogEvent> @event)
    {
        switch (@event)
        {
            case EventLogAddedEvent e:
                // Add the log record
                state.EventLogs.Add(e.Record);
                state.TotalEventsLogged++;

                // Update statistics
                if (!state.EventTypeCounts.ContainsKey(e.Record.EventType))
                {
                    state.EventTypeCounts[e.Record.EventType] = 0;
                }
                state.EventTypeCounts[e.Record.EventType]++;

                if (!state.SourceAgentCounts.ContainsKey(e.Record.SourceAgent))
                {
                    state.SourceAgentCounts[e.Record.SourceAgent] = 0;
                }
                state.SourceAgentCounts[e.Record.SourceAgent]++;

                // Update timestamps
                state.FirstEventTime ??= e.Record.LoggedAt;
                state.LastEventTime = e.Record.LoggedAt;

                // Keep only last 200 logs
                if (state.EventLogs.Count > 200)
                {
                    state.EventLogs.RemoveAt(0);
                }
                break;
                
            case EventLogsClearedEvent _:
                state.EventLogs.Clear();
                state.EventTypeCounts.Clear();
                state.SourceAgentCounts.Clear();
                state.TotalEventsLogged = 0;
                state.FirstEventTime = null;
                state.LastEventTime = null;
                break;
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
    /// Search event logs
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

        return Task.FromResult(query.OrderByDescending(e => e.LoggedAt).ToList());
    }

    /// <summary>
    /// Get recent event logs
    /// </summary>
    public Task<List<EventLogRecord>> GetRecentEventsAsync(int count = 50)
    {
        return Task.FromResult(State.EventLogs
            .OrderByDescending(e => e.LoggedAt)
            .Take(count)
            .ToList());
    }

    /// <summary>
    /// Get event statistics
    /// </summary>
    public Task<EventLoggerStatistics> GetStatisticsAsync()
    {
        return Task.FromResult(new EventLoggerStatistics
        {
            TotalEvents = State.TotalEventsLogged,
            EventTypeCounts = new Dictionary<string, int>(State.EventTypeCounts),
            SourceAgentCounts = new Dictionary<string, int>(State.SourceAgentCounts),
            FirstEventTime = State.FirstEventTime,
            LastEventTime = State.LastEventTime,
            RecentEvents = State.EventLogs.OrderByDescending(e => e.LoggedAt).Take(10).ToList()
        });
    }

    /// <summary>
    /// Clear all event logs
    /// </summary>
    public async Task ClearEventLogsAsync()
    {
        // Use event sourcing
        RaiseEvent(new EventLogsClearedEvent());
        await ConfirmEvents();
        
        Logger.LogInformation("Event logs cleared");
    }
}

[GenerateSerializer]
public class EventLoggerStatistics
{
    [Id(0)] public int TotalEvents { get; set; }
    [Id(1)] public Dictionary<string, int> EventTypeCounts { get; set; } = new();
    [Id(2)] public Dictionary<string, int> SourceAgentCounts { get; set; } = new();
    [Id(3)] public DateTime? FirstEventTime { get; set; }
    [Id(4)] public DateTime? LastEventTime { get; set; }
    [Id(5)] public List<EventLogRecord> RecentEvents { get; set; } = new();
} 