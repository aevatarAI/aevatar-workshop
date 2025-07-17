using System.Text.Json;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.Basic.PublishGAgent;
using Aevatar.GAgents.GroupChat.Core.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.Workshop.GAgent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowDemoController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<WorkflowDemoController> _logger;
    
    // Store workflow instances
    private static readonly Dictionary<Guid, WorkflowInstance> _workflows = new();

    public WorkflowDemoController(
        IClusterClient clusterClient,
        ILogger<WorkflowDemoController> logger)
    {
        _gAgentFactory = new GAgentFactory(clusterClient);
        _logger = logger;
    }

    /// <summary>
    /// Create a new workflow instance
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateWorkflowRequest request)
    {
        try
        {
            var workflowId = Guid.NewGuid();
            
            // Create workflow coordinator
            var coordinator = await _gAgentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(workflowId);
            
            // Create group for workflow
            var groupId = Guid.NewGuid();
            var groupAgent = await _gAgentFactory.GetGAgentAsync<IGroupGAgent>(groupId);
            
            // Create workflow instance
            var instance = new WorkflowInstance
            {
                Id = workflowId,
                Name = request.Name,
                Description = request.Description,
                Coordinator = coordinator,
                GroupAgent = groupAgent,
                WorkUnits = new Dictionary<string, IGAgent>(),
                CreatedAt = DateTime.UtcNow
            };
            
            _workflows[workflowId] = instance;
            
            _logger.LogInformation($"Created workflow: {workflowId}");
            
            return Ok(new
            {
                workflowId,
                name = request.Name,
                description = request.Description,
                status = "created"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Add a work unit to the workflow
    /// </summary>
    [HttpPost("{workflowId}/units")]
    public async Task<IActionResult> AddWorkUnit(Guid workflowId, [FromBody] AddWorkUnitRequest request)
    {
        try
        {
            if (!_workflows.TryGetValue(workflowId, out var workflow))
            {
                return NotFound(new { error = "Workflow not found" });
            }
            
            IGAgent workUnit;
            
            // Create appropriate GAgent based on type
            switch (request.Type)
            {
                case "WorkflowMathGAgent":
                    workUnit = await _gAgentFactory.GetGAgentAsync<IWorkflowMathGAgent>(Guid.NewGuid());
                    await ((IWorkflowMathGAgent)workUnit).ConfigAsync(new MathGAgentConfigDto 
                    { 
                        MemberName = request.Name 
                    });
                    break;
                    
                case "WorkflowTimeConverterGAgent":
                    workUnit = await _gAgentFactory.GetGAgentAsync<IWorkflowTimeConverterGAgent>(Guid.NewGuid());
                    await ((IWorkflowTimeConverterGAgent)workUnit).ConfigAsync(new TimeConverterGAgentConfigDto 
                    { 
                        MemberName = request.Name 
                    });
                    break;
                    
                case "AIGAgent":
                    workUnit = await _gAgentFactory.GetGAgentAsync<IWorkflowAIGAgent>(Guid.NewGuid());
                    var aiConfig = request.Config?.Deserialize<InitializeDto>() ?? new InitializeDto();
                    aiConfig.Instructions = request.SystemPrompt ?? "You are a helpful AI assistant in a workflow.";
                    await ((IWorkflowAIGAgent)workUnit).InitializeAsync(aiConfig);
                    break;
                    
                default:
                    return BadRequest(new { error = $"Unknown work unit type: {request.Type}" });
            }
            
            var grainId = workUnit.GetGrainId().ToString();
            workflow.WorkUnits[grainId] = workUnit;
            
            _logger.LogInformation($"Added work unit {request.Name} ({request.Type}) to workflow {workflowId}");
            
            return Ok(new
            {
                unitId = grainId,
                name = request.Name,
                type = request.Type,
                status = "added"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add work unit");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Configure workflow connections
    /// </summary>
    [HttpPost("{workflowId}/configure")]
    public async Task<IActionResult> ConfigureWorkflow(Guid workflowId, [FromBody] ConfigureWorkflowRequest request)
    {
        try
        {
            if (!_workflows.TryGetValue(workflowId, out var workflow))
            {
                return NotFound(new { error = "Workflow not found" });
            }
            
            // Convert connections to WorkflowUnitDto list
            var workflowUnits = request.Connections.Select(c => new WorkflowUnitDto
            {
                GrainId = c.FromUnitId,
                NextGrainId = c.ToUnitId
            }).ToList();
            
            // Add terminal nodes (units with no next)
            var terminalUnits = workflow.WorkUnits.Keys
                .Where(id => !request.Connections.Any(c => c.FromUnitId == id))
                .Select(id => new WorkflowUnitDto
                {
                    GrainId = id,
                    NextGrainId = null
                });
            
            workflowUnits.AddRange(terminalUnits);
            
            // Configure coordinator
            await workflow.Coordinator.ConfigAsync(new WorkflowCoordinatorConfigDto
            {
                WorkflowUnitList = workflowUnits,
                InitContent = request.InitialContent ?? "Start workflow"
            });
            
            // Register all work units with the group
            foreach (var unit in workflow.WorkUnits.Values)
            {
                await workflow.GroupAgent.RegisterAsync(unit);
            }
            
            await workflow.GroupAgent.RegisterAsync(workflow.Coordinator);
            
            workflow.Status = "configured";
            
            _logger.LogInformation($"Configured workflow {workflowId} with {workflowUnits.Count} units");
            
            return Ok(new
            {
                workflowId,
                units = workflowUnits.Count,
                connections = request.Connections.Count,
                status = "configured"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure workflow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Start workflow execution
    /// </summary>
    [HttpPost("{workflowId}/start")]
    public async Task<IActionResult> StartWorkflow(Guid workflowId, [FromBody] StartWorkflowRequest request)
    {
        try
        {
            if (!_workflows.TryGetValue(workflowId, out var workflow))
            {
                return NotFound(new { error = "Workflow not found" });
            }
            
            if (workflow.Status != "configured")
            {
                return BadRequest(new { error = "Workflow must be configured before starting" });
            }

            // Start the workflow
            var publishingGAgent = await _gAgentFactory.GetGAgentAsync<Core.Abstractions.IPublishingGAgent>(Guid.NewGuid());
            await publishingGAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent
            {
                InitContent = request.InitialInput ?? "Start workflow execution"
            }, workflow.Coordinator);
            
            workflow.Status = "running";
            workflow.StartedAt = DateTime.UtcNow;
            
            _logger.LogInformation($"Started workflow {workflowId}");
            
            return Ok(new
            {
                workflowId,
                status = "running",
                startedAt = workflow.StartedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start workflow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get workflow status
    /// </summary>
    [HttpGet("{workflowId}/status")]
    public async Task<IActionResult> GetWorkflowStatus(Guid workflowId)
    {
        try
        {
            if (!_workflows.TryGetValue(workflowId, out var workflow))
            {
                return NotFound(new { error = "Workflow not found" });
            }
            
            var state = await workflow.Coordinator.GetStateAsync();
            
            return Ok(new
            {
                workflowId,
                name = workflow.Name,
                status = workflow.Status,
                workflowStatus = state.WorkflowStatus.ToString(),
                currentTerm = state.Term,
                createdAt = workflow.CreatedAt,
                startedAt = workflow.StartedAt,
                units = workflow.WorkUnits.Count,
                unitDetails = state.CurrentWorkUnitInfos.Select(u => new
                {
                    grainId = u.GrainId,
                    status = u.UnitStatusEnum.ToString(),
                    unitStatusEnum = u.UnitStatusEnum
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// List all workflows
    /// </summary>
    [HttpGet]
    public IActionResult ListWorkflows()
    {
        var workflows = _workflows.Values.Select(w => new
        {
            id = w.Id,
            name = w.Name,
            description = w.Description,
            status = w.Status,
            createdAt = w.CreatedAt,
            startedAt = w.StartedAt,
            units = w.WorkUnits.Count
        });
        
        return Ok(workflows);
    }

    /// <summary>
    /// Reset workflow
    /// </summary>
    [HttpPost("{workflowId}/reset")]
    public async Task<IActionResult> ResetWorkflow(Guid workflowId)
    {
        try
        {
            if (!_workflows.TryGetValue(workflowId, out var workflow))
            {
                return NotFound(new { error = "Workflow not found" });
            }
            
            var publishingGAgent = await _gAgentFactory.GetGAgentAsync<Core.Abstractions.IPublishingGAgent>(Guid.NewGuid());
            await publishingGAgent.PublishEventAsync(new ResetWorkflowEvent(), workflow.Coordinator);

            workflow.Status = "configured";
            workflow.StartedAt = null;
            
            _logger.LogInformation($"Reset workflow {workflowId}");
            
            return Ok(new
            {
                workflowId,
                status = "reset"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset workflow");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

// Request/Response DTOs
public class CreateWorkflowRequest
{
    public string Name { get; set; } = "New Workflow";
    public string Description { get; set; } = "";
}

public class AddWorkUnitRequest
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = ""; // MathGAgent, TimeConverterGAgent, AIGAgent
    public string? SystemPrompt { get; set; }
    public JsonElement? Config { get; set; }
}

public class ConfigureWorkflowRequest
{
    public List<WorkflowConnection> Connections { get; set; } = new();
    public string? InitialContent { get; set; }
}

public class WorkflowConnection
{
    public string FromUnitId { get; set; } = "";
    public string ToUnitId { get; set; } = "";
}

public class StartWorkflowRequest
{
    public string? InitialInput { get; set; }
}

// Internal models
internal class WorkflowInstance
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public IWorkflowCoordinatorGAgent Coordinator { get; set; } = null!;
    public IGroupGAgent GroupAgent { get; set; } = null!;
    public Dictionary<string, IGAgent> WorkUnits { get; set; } = new();
    public string Status { get; set; } = "created";
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
} 