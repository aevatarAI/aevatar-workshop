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
            DisplayName = "通知处理器",
            Description = "处理和记录通知事件的演示GAgent",
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
            DisplayName = "数据处理器",
            Description = "处理数据处理任务的演示GAgent，支持优先级队列",
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
            DisplayName = "协调器",
            Description = "协调多个GAgent协同工作的演示GAgent",
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
            DisplayName = "事件记录器",
            Description = "记录和分析系统中所有事件的演示GAgent",
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
    /// 获取所有demo GAgent信息
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
    /// 初始化demo环境
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeDemoAsync()
    {
        try
        {
            var results = new List<object>();

            // 1. 创建PublishingGAgent作为parent
            var publishingAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>(PublishingAgentId);
            await publishingAgent.ActivateAsync();
            
            _logger.LogInformation("创建PublishingGAgent: {GrainId}", PublishingAgentId);

            // 2. 创建每个demo GAgent并注册为PublishingGAgent的children
            foreach (var agentInfo in DemoGAgents)
            {
                var agentId = Guid.NewGuid();
                DemoGAgentIds[agentInfo.Name] = agentId;
                
                var grainId = GrainId.Create(agentInfo.GrainType, agentId.ToString());
                var agent = await _gAgentFactory.GetGAgentAsync(grainId);
                
                // 激活agent
                await agent.ActivateAsync();
                var description = await agent.GetDescriptionAsync();
                
                // 将agent注册为PublishingGAgent的child
                await publishingAgent.RegisterAsync(agent);
                
                results.Add(new
                {
                    agentInfo.Name,
                    grainId = grainId.ToString(),
                    description,
                    status = "initialized"
                });

                _logger.LogInformation("初始化并注册 {AgentName}: {GrainId}", agentInfo.Name, grainId);
            }

            // 3. 设置特殊的订阅关系
            // EventLoggerGAgent应该也订阅其他GAgent以记录它们的事件
            var eventLogger = await _gAgentFactory.GetGAgentAsync<IEventLoggerGAgent>(DemoGAgentIds["EventLoggerGAgent"]);
            
            foreach (var agentInfo in DemoGAgents.Where(a => a.Name != "EventLoggerGAgent"))
            {
                var agentGrainId = GrainId.Create(agentInfo.GrainType, DemoGAgentIds[agentInfo.Name].ToString());
                var agent = await _gAgentFactory.GetGAgentAsync(agentGrainId);
                
                // EventLogger订阅其他agent的事件
                await agent.RegisterAsync(eventLogger);
                _logger.LogInformation("EventLogger订阅 {AgentName}", agentInfo.Name);
            }
            
            // 同时，为了让EventLogger能记录PublishingGAgent发布的事件，也需要让PublishingGAgent注册EventLogger
            await publishingAgent.RegisterAsync(eventLogger);
            _logger.LogInformation("PublishingGAgent注册EventLogger为child");

            return Ok(new { success = true, agents = results, publishingAgentId = PublishingAgentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化demo环境失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定GAgent的统计信息
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
            _logger.LogError(ex, "获取统计信息失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 触发通知事件
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

            // 通过PublishingGAgent发布事件
            await PublishEventAsync(notificationEvent);

            return Ok(new { success = true, eventId = notificationEvent.GetHashCode() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发通知事件失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 触发数据处理事件
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
            _logger.LogError(ex, "触发数据处理事件失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 触发协调请求事件
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
            _logger.LogError(ex, "触发协调请求事件失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 搜索事件日志
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

            // 获取EventLogger实例
            var loggerAgent = await _gAgentFactory.GetGAgentAsync<IEventLoggerGAgent>(guid);
            
            var events = await loggerAgent.SearchEventsAsync(eventType, sourceAgent);
            
            return Ok(new { events });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索事件日志失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 创建模拟场景
    /// </summary>
    [HttpPost("scenario/simulate")]
    public async Task<IActionResult> SimulateScenarioAsync()
    {
        try
        {
            // 1. 发送一些通知
            await TriggerNotificationAsync(new TriggerNotificationRequest
            {
                Title = "系统启动",
                Message = "Event Handler Demo系统已启动",
                Level = NotificationLevel.Info
            });

            await Task.Delay(500);

            // 2. 触发数据处理
            await TriggerProcessingAsync(new TriggerProcessingRequest
            {
                DataType = "UserData",
                Data = new Dictionary<string, object> { ["userId"] = "123", ["action"] = "login" },
                Priority = ProcessingPriority.High
            });

            await Task.Delay(500);

            // 3. 触发协调任务
            await TriggerCoordinationAsync(new TriggerCoordinationRequest
            {
                TaskName = "数据同步任务",
                RequiredAgents = new List<string> { "DataAgent", "CacheAgent", "DBAgent" }
            });

            await Task.Delay(500);

            // 4. 触发错误通知
            await TriggerNotificationAsync(new TriggerNotificationRequest
            {
                Title = "处理错误",
                Message = "数据验证失败",
                Level = NotificationLevel.Error
            });

            return Ok(new { success = true, message = "模拟场景已执行" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行模拟场景失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task PublishEventAsync(EventBase eventBase)
    {
        // 获取PublishingGAgent并发布事件
        var publishingAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>(PublishingAgentId);
        await publishingAgent.PublishEventAsync(eventBase);
        
        _logger.LogInformation("通过PublishingGAgent发布事件: {EventType}", eventBase.GetType().Name);
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