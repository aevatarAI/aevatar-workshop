using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using System.ComponentModel;

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
    // Simplified service access
    private IGAgentFactory GAgentFactory => ServiceProvider.GetRequiredService<IGAgentFactory>();

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("CoordinatorDemoGAgent - Demo GAgent for coordinating multiple GAgents to work together");
    }

    /// <summary>
    /// Handle coordination request events
    /// </summary>
    [EventHandler]
    public async Task HandleCoordinationRequestAsync(CoordinationRequestEvent @event)
    {
        Logger.LogInformation("Received coordination request {RequestId}: {TaskName}",
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

        // Log event
        await PublishAsync(new EventLoggedEvent
        {
            SourceAgent = this.GetGrainId().ToString(),
            EventType = nameof(CoordinationRequestEvent),
            EventData = $"Coordinating {task.TaskName} with {task.RequiredAgents.Count} agents",
            Success = true
        });

        // Start coordination process
        await StartCoordinationAsync(@event, task);
    }

    /// <summary>
    /// Handle coordination response events
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

            Logger.LogInformation("Received response from {Agent} for task {RequestId}: {Accepted}",
                @event.RespondingAgent, @event.RequestId, @event.Accepted ? "Accepted" : "Rejected");

            // Check if all agents have responded
            if (task.AgentAcceptance.Count == task.RequiredAgents.Count)
            {
                await CompleteCoordinationAsync(task);
            }
        }
    }

    private async Task StartCoordinationAsync(CoordinationRequestEvent request, CoordinationTask task)
    {
        // Simulate sending coordination requests to required agents
        foreach (var agentName in request.RequiredAgents)
        {
            try
            {
                // Send notification to each agent
                await PublishAsync(new NotificationEvent
                {
                    Title = $"Coordination Request: {task.TaskName}",
                    Message = $"Your participation is needed for task: {task.TaskName}",
                    Level = NotificationLevel.Info,
                    Source = this.GetGrainId().ToString()
                });

                // If processing task, trigger data processing
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

                // Simulate agent response (in real applications, other agents would send response events)
                await Task.Delay(Random.Shared.Next(100, 500));
                await SimulateAgentResponseAsync(request.RequestId, agentName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error occurred while coordinating {Agent}", agentName);
            }
        }
    }

    private async Task SimulateAgentResponseAsync(string requestId, string agentName)
    {
        // Simulate agent response (80% probability of acceptance)
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

        Logger.LogInformation("Coordination task {RequestId} completed: {AcceptedCount}/{TotalCount} agents accepted",
            task.RequestId, acceptedCount, task.RequiredAgents.Count);

        // Send completion notification
        await PublishAsync(new NotificationEvent
        {
            Title = $"Coordination Completed: {task.TaskName}",
            Message = $"{acceptedCount}/{task.RequiredAgents.Count} agents participated in the task",
            Level = success ? NotificationLevel.Success : NotificationLevel.Warning,
            Source = this.GetGrainId().ToString()
        });

        // Keep task history to last 20 records
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
    /// Get coordination statistics
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
    /// Create test coordination task
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