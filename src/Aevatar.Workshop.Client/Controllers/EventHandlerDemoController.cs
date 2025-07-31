using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.Events;
using Aevatar.Workshop.GAgent.GAgents.Demo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventHandlerDemoController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<EventHandlerDemoController> _logger;
    
    // Store the main publishing agent ID for the demo
    private static readonly Guid PublishingAgentId = Guid.NewGuid();
    
    // Store demo GAgent instances
    private static readonly Dictionary<string, Guid> DemoGAgentIds = new();

    // Demo GAgent types
    private static readonly List<DemoGAgentInfo> DemoGAgents = new()
    {
        new DemoGAgentInfo
        {
            Name = "NotificationGAgent",
            DisplayName = "Notification Handler",
            Description = "Demo GAgent for processing and recording notification events",
            GrainType = GrainType.Create("workshop.notification-demo"),
            EventHandlers = new List<string> 
            { 
                "HandleNotificationAsync(NotificationEvent)",
                "LogAllEventsAsync(EventWrapperBase)"
            }
        },
        new DemoGAgentInfo
        {
            Name = "ProcessingGAgent",
            DisplayName = "Data Processor",
            Description = "Demo GAgent for handling data processing tasks with priority queue support",
            GrainType = GrainType.Create("workshop.processing-demo"),
            EventHandlers =
            [
                "HandleDataProcessingAsync(DataProcessingEvent)",
                "HandleProcessingCompletedAsync(ProcessingCompletedEvent)",
                "HandleEventAsync(EventBase)"
            ]
        },
        new DemoGAgentInfo
        {
            Name = "CoordinatorDemoGAgent",
            DisplayName = "Coordinator",
            Description = "Demo GAgent for coordinating multiple GAgents to work together",
            GrainType = GrainType.Create("workshop.coordinator-demo"),
            EventHandlers = new List<string>
            {
                "HandleCoordinationRequestAsync(CoordinationRequestEvent)",
                "HandleCoordinationResponseAsync(CoordinationResponseEvent)"
            }
        },
        new DemoGAgentInfo
        {
            Name = "EventLoggerGAgent",
            DisplayName = "Event Logger",
            Description = "Demo GAgent for recording and analyzing all events in the system",
            GrainType = GrainType.Create("workshop.event-logger-demo"),
            EventHandlers = new List<string>
            {
                "HandleEventLoggedAsync(EventLoggedEvent)",
                "LogAllEventsAsync(EventWrapperBase)"
            }
        }
    };

    public EventHandlerDemoController(
        IGAgentFactory gAgentFactory,
        ILogger<EventHandlerDemoController> logger)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
    }

    /// <summary>
    /// Get all demo GAgent information
    /// </summary>
    [HttpGet("agents")]
    public IActionResult GetDemoAgents()
    {
        var agents = DemoGAgents.Select(agent => new
        {
            agent.Name,
            agent.DisplayName,
            agent.Description,
            agent.GrainType,
            agent.EventHandlers,
            grainId = DemoGAgentIds.ContainsKey(agent.Name) ? 
                GrainId.Create(agent.GrainType, DemoGAgentIds[agent.Name].ToString()).ToString() : null
        }).ToList();
        
        return Ok(new { agents });
    }

    /// <summary>
    /// Initialize demo environment
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeDemoAsync()
    {
        try
        {
            var results = new List<object>();

            // 1. Create PublishingGAgent as parent
            var publishingAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>(PublishingAgentId);
            await publishingAgent.ActivateAsync();
            
            _logger.LogInformation("Creating PublishingGAgent: {GrainId}", PublishingAgentId);

            // 2. Create each demo GAgent and register as children of PublishingGAgent
            foreach (var agentInfo in DemoGAgents)
            {
                var agentId = Guid.NewGuid();
                DemoGAgentIds[agentInfo.Name] = agentId;
                
                var grainId = GrainId.Create(agentInfo.GrainType, agentId.ToString());
                var agent = await _gAgentFactory.GetGAgentAsync(grainId);
                
                // Activate agent
                await agent.ActivateAsync();
                var description = await agent.GetDescriptionAsync();
                
                // Register agent as child of PublishingGAgent
                await publishingAgent.RegisterAsync(agent);
                
                results.Add(new
                {
                    agentInfo.Name,
                    grainId = grainId.ToString(),
                    description,
                    status = "initialized"
                });

                _logger.LogInformation("Initializing and registering {AgentName}: {GrainId}", agentInfo.Name, grainId);
            }

            // 3. Set up special subscription relationships
            // EventLoggerGAgent should also subscribe to other GAgents to record their events
            var eventLogger = await _gAgentFactory.GetGAgentAsync<IEventLoggerGAgent>(DemoGAgentIds["EventLoggerGAgent"]);
            
            foreach (var agentInfo in DemoGAgents.Where(a => a.Name != "EventLoggerGAgent"))
            {
                var agentGrainId = GrainId.Create(agentInfo.GrainType, DemoGAgentIds[agentInfo.Name].ToString());
                var agent = await _gAgentFactory.GetGAgentAsync(agentGrainId);
                
                // EventLogger subscribes to other agents' events
                await agent.RegisterAsync(eventLogger);
                _logger.LogInformation("EventLogger subscribing to {AgentName}", agentInfo.Name);
            }
            
            // At the same time, to allow EventLogger to record events published by PublishingGAgent, PublishingGAgent also needs to register EventLogger
            await publishingAgent.RegisterAsync(eventLogger);
            _logger.LogInformation("PublishingGAgent registering EventLogger as child");

            return Ok(new { success = true, agents = results, publishingAgentId = PublishingAgentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize demo environment");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get statistics for specified GAgent
    /// </summary>
    [HttpGet("statistics/{agentName}")]
    public async Task<IActionResult> GetAgentStatisticsAsync(string agentName)
    {
        try
        {
            if (!DemoGAgentIds.ContainsKey(agentName))
            {
                return NotFound(new { error = "Agent not initialized" });
            }

            var agentInfo = DemoGAgents.FirstOrDefault(a => a.Name == agentName);
            if (agentInfo == null)
            {
                return NotFound(new { error = "Unknown agent type" });
            }

            var grainId = GrainId.Create(agentInfo.GrainType, DemoGAgentIds[agentName].ToString());
            
            if (agentName == "NotificationGAgent")
            {
                var agent = await _gAgentFactory.GetGAgentAsync<NotificationGAgent>(grainId);
                var stats = await agent.GetStatisticsAsync();
                return Ok(new { type = "notification", statistics = stats });
            }
            else if (agentName == "ProcessingGAgent")
            {
                var agent = await _gAgentFactory.GetGAgentAsync<ProcessingGAgent>(grainId);
                var stats = await agent.GetStatisticsAsync();
                return Ok(new { type = "processing", statistics = stats });
            }
            else if (agentName == "CoordinatorDemoGAgent")
            {
                var agent = await _gAgentFactory.GetGAgentAsync<CoordinatorDemoGAgent>(grainId);
                var stats = await agent.GetStatisticsAsync();
                return Ok(new { type = "coordinator", statistics = stats });
            }
            else if (agentName == "EventLoggerGAgent")
            {
                var agent = await _gAgentFactory.GetGAgentAsync<EventLoggerGAgent>(grainId);
                var stats = await agent.GetStatisticsAsync();
                return Ok(new { type = "eventLogger", statistics = stats });
            }

            return NotFound(new { error = "Unknown agent type" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger notification event
    /// </summary>
    [HttpPost("trigger/notification")]
    public async Task<IActionResult> TriggerNotificationAsync([FromBody] TriggerNotificationRequest request)
    {
        try
        {
            var notificationEvent = new NotificationEvent
            {
                Title = request.Title,
                Message = request.Message,
                Level = request.Level,
                Source = request.Source ?? "EventHandlerDemo"
            };

            // Publish event through PublishingGAgent
            await PublishEventAsync(notificationEvent);

            return Ok(new { success = true, eventId = notificationEvent.GetHashCode() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger notification event");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger data processing event
    /// </summary>
    [HttpPost("trigger/processing")]
    public async Task<IActionResult> TriggerProcessingAsync([FromBody] TriggerProcessingRequest request)
    {
        try
        {
            var processingEvent = new DataProcessingEvent
            {
                DataType = request.DataType,
                Data = request.Data ?? new Dictionary<string, object>(),
                Priority = request.Priority
            };

            await PublishEventAsync(processingEvent);

            return Ok(new { success = true, processingId = processingEvent.ProcessingId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger data processing event");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger coordination request event
    /// </summary>
    [HttpPost("trigger/coordination")]
    public async Task<IActionResult> TriggerCoordinationAsync([FromBody] TriggerCoordinationRequest request)
    {
        try
        {
            var coordinationEvent = new CoordinationRequestEvent
            {
                TaskName = request.TaskName,
                RequiredAgents = request.RequiredAgents ?? ["Agent1", "Agent2", "Agent3"],
                Parameters = request.Parameters ?? new Dictionary<string, string>()
            };

            await PublishEventAsync(coordinationEvent);

            return Ok(new { success = true, requestId = coordinationEvent.RequestId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger coordination request event");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search event logs
    /// </summary>
    [HttpGet("events/search")]
    public async Task<IActionResult> SearchEventsAsync(
        [FromQuery] string? eventType = null,
        [FromQuery] string? sourceAgent = null)
    {
        try
        {
            if (!DemoGAgentIds.TryGetValue("EventLoggerGAgent", out var guid))
            {
                return BadRequest(new { error = "EventLogger not initialized" });
            }

            // Get EventLogger instance
            var loggerAgent = await _gAgentFactory.GetGAgentAsync<IEventLoggerGAgent>(guid);
            
            var events = await loggerAgent.SearchEventsAsync(eventType, sourceAgent);
            
            return Ok(new { events });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search event logs");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create simulation scenario
    /// </summary>
    [HttpPost("scenario/simulate")]
    public async Task<IActionResult> SimulateScenarioAsync()
    {
        try
        {
            // 1. Send some notifications
            await TriggerNotificationAsync(new TriggerNotificationRequest
            {
                Title = "System Startup",
                Message = "Event Handler Demo system started",
                Level = NotificationLevel.Info
            });

            await Task.Delay(500);

            // 2. Trigger data processing
            await TriggerProcessingAsync(new TriggerProcessingRequest
            {
                DataType = "UserData",
                Data = new Dictionary<string, object> { ["userId"] = "123", ["action"] = "login" },
                Priority = ProcessingPriority.High
            });

            await Task.Delay(500);

            // 3. Trigger coordination task
            await TriggerCoordinationAsync(new TriggerCoordinationRequest
            {
                TaskName = "Data Synchronization Task",
                RequiredAgents = new List<string> { "DataAgent", "CacheAgent", "DBAgent" }
            });

            await Task.Delay(500);

            // 4. Trigger error notification
            await TriggerNotificationAsync(new TriggerNotificationRequest
            {
                Title = "Processing Error",
                Message = "Data validation failed",
                Level = NotificationLevel.Error
            });

            return Ok(new { success = true, message = "Simulation scenario executed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute simulation scenario");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task PublishEventAsync(EventBase eventBase)
    {
        // Get PublishingGAgent and publish event
        var publishingAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>(PublishingAgentId);
        await publishingAgent.PublishEventAsync(eventBase);
        
        _logger.LogInformation("Publishing event through PublishingGAgent: {EventType}", eventBase.GetType().Name);
    }

    // DTOs
    public class DemoGAgentInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GrainType GrainType { get; set; }
        public List<string> EventHandlers { get; set; } = new();
    }

    public class TriggerNotificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationLevel Level { get; set; } = NotificationLevel.Info;
        public string? Source { get; set; }
    }

    public class TriggerProcessingRequest
    {
        public string DataType { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
        public ProcessingPriority Priority { get; set; } = ProcessingPriority.Normal;
    }

    public class TriggerCoordinationRequest
    {
        public string TaskName { get; set; } = string.Empty;
        public List<string>? RequiredAgents { get; set; }
        public Dictionary<string, string>? Parameters { get; set; }
    }
} 