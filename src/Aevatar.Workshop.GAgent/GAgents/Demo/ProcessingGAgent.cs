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
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("ProcessingGAgent - Demo GAgent for handling data processing tasks with priority queue support");
    }

    /// <summary>
    /// Handle data processing events
    /// </summary>
    [EventHandler]
    public async Task HandleDataProcessingAsync(DataProcessingEvent @event)
    {
        Logger.LogInformation("Starting processing task {ProcessingId}, Type: {DataType}, Priority: {Priority}",
            @event.ProcessingId, @event.DataType, @event.Priority);

        // Create processing task
        var task = new ProcessingTask
        {
            ProcessingId = @event.ProcessingId,
            DataType = @event.DataType,
            Priority = @event.Priority,
            StartedAt = DateTime.UtcNow
        };

        State.ActiveTasks.Add(task);
        
        // Update priority statistics
        if (!State.TasksByPriority.ContainsKey(@event.Priority))
        {
            State.TasksByPriority[@event.Priority] = 0;
        }
        State.TasksByPriority[@event.Priority]++;

        // Send event log
        await PublishAsync(new EventLoggedEvent
        {
            SourceAgent = this.GetGrainId().ToString(),
            EventType = nameof(DataProcessingEvent),
            EventData = $"Processing {task.DataType} with priority {task.Priority}",
            Success = true
        });

        // Simulate async processing
        await ProcessDataAsync(@event, task);
    }

    /// <summary>
    /// Handle processing completed events
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

            // Keep completed task history to last 50 records
            if (State.CompletedTasks.Count > 50)
            {
                State.CompletedTasks.RemoveAt(0);
            }

            Logger.LogInformation("Task {ProcessingId} completed, Result: {Result}",
                @event.ProcessingId, @event.Result);

            // Send notification
            await PublishAsync(new NotificationEvent
            {
                Title = $"Task Completed: {task.DataType}",
                Message = $"Processing Result: {@event.Result}",
                Level = @event.Result == ProcessingResult.Success ? 
                    NotificationLevel.Success : NotificationLevel.Warning,
                Source = this.GetGrainId().ToString()
            });
        }
    }

    /// <summary>
    /// Default event handler (demonstrates HandleEventAsync method name convention)
    /// </summary>
    public Task HandleEventAsync(EventBase @event)
    {
        Logger.LogDebug("ProcessingGAgent received generic event: {EventType}", @event.GetType().Name);
        return Task.CompletedTask;
    }

    private async Task ProcessDataAsync(DataProcessingEvent @event, ProcessingTask task)
    {
        try
        {
            // Simulate processing time (based on priority)
            var delay = @event.Priority switch
            {
                ProcessingPriority.Critical => 100,
                ProcessingPriority.High => 500,
                ProcessingPriority.Normal => 1000,
                ProcessingPriority.Low => 2000,
                _ => 1000
            };

            await Task.Delay(delay);

            // Simulate processing logic
            var outputData = new Dictionary<string, object>
            {
                ["ProcessedCount"] = @event.Data.Count,
                ["ProcessingTime"] = delay,
                ["DataType"] = @event.DataType
            };

            // Send completion event
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
            Logger.LogError(ex, "Error occurred while processing task {ProcessingId}", @event.ProcessingId);
            
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
    /// Get processing statistics
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