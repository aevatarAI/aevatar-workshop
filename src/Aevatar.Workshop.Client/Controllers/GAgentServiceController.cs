using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Executor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Aevatar.Workshop.GAgent;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GAgentServiceController : ControllerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IHostEnvironment _environment;
    private IGAgentInfoGrain? _gagentInfoGrain;

    public GAgentServiceController(
        IClusterClient clusterClient,
        IHostEnvironment environment)
    {
        _clusterClient = clusterClient;
        _environment = environment;
    }

    private async Task<IGAgentInfoGrain> GetGAgentInfoGrainAsync()
    {
        _gagentInfoGrain ??= _clusterClient.GetGrain<IGAgentInfoGrain>(Guid.Empty);
        return _gagentInfoGrain;
    }

    /// <summary>
    /// List all available GAgents in the system
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListGAgents()
    {
        try
        {
            var grain = await GetGAgentInfoGrainAsync();
            var allGAgents = await grain.GetAllGAgentInfoAsync();

            var result = new List<object>();
            foreach (var gagent in allGAgents)
            {
                result.Add(new
                {
                    grainType = gagent.GrainType,
                    eventCount = gagent.EventTypes.Count,
                    events = gagent.EventTypes.Select(eventName => new
                    {
                        name = eventName.Split('.').Last(),
                        fullName = eventName
                    })
                });
            }

            return Ok(new
            {
                success = true,
                total = result.Count,
                gAgents = result
            });
        }
        catch (Exception ex)
        {
            if (_environment.IsDevelopment())
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }

            return StatusCode(500, new
            {
                success = false,
                error = "Failed to list GAgents"
            });
        }
    }

    /// <summary>
    /// Get detailed information about a specific GAgent
    /// </summary>
    [HttpGet("info")]
    public async Task<IActionResult> GetGAgentInfo([FromQuery] string grainType)
    {
        if (string.IsNullOrEmpty(grainType))
        {
            return BadRequest(new { success = false, error = "GrainType is required" });
        }

        try
        {
            var grain = await GetGAgentInfoGrainAsync();
            var detailInfo = await grain.GetGAgentDetailInfoAsync(grainType);

            if (detailInfo == null)
            {
                return NotFound(new { success = false, error = $"GAgent {grainType} not found" });
            }

            return Ok(new
            {
                success = true,
                info = new
                {
                    grainType = detailInfo.GrainType,
                    description = detailInfo.Description,
                    supportedEvents = detailInfo.EventTypes.Select(eventName => new
                    {
                        name = eventName.Split('.').Last(),
                        fullName = eventName
                    }),
                    configurationType = detailInfo.ConfigurationType
                }
            });
        }
        catch (Exception ex)
        {
            if (_environment.IsDevelopment())
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }

            return StatusCode(500, new
            {
                success = false,
                error = "Failed to get GAgent info"
            });
        }
    }

    /// <summary>
    /// Execute a GAgent event handler
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteGAgent([FromBody] ExecuteGAgentRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.GrainType) || string.IsNullOrEmpty(request.EventTypeName))
        {
            return BadRequest(new
            {
                success = false,
                error = "Invalid request. GrainType and EventTypeName are required."
            });
        }

        try
        {
            var grain = await GetGAgentInfoGrainAsync();

            // Execute the GAgent through the grain
            var executionRequest = new GAgentExecutionRequest
            {
                GrainType = request.GrainType,
                EventTypeName = request.EventTypeName,
                EventDataJson = request.EventParameters ?? "{}"
            };

            var executionResult = await grain.ExecuteGAgentAsync(executionRequest);

            if (!executionResult.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    error = executionResult.Error
                });
            }

            return Ok(new
            {
                success = true,
                result = executionResult.Result
            });
        }
        catch (TimeoutException tex)
        {
            return StatusCode(408, new
            {
                success = false,
                error = "Operation timed out",
                details = tex.Message
            });
        }
        catch (Exception ex)
        {
            if (_environment.IsDevelopment())
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }

            return StatusCode(500, new
            {
                success = false,
                error = "Failed to execute GAgent"
            });
        }
    }

    /// <summary>
    /// Get ResultGAgent state (for debugging)
    /// </summary>
    [HttpGet("result-state/{grainId}")]
    public async Task<IActionResult> GetResultGAgentState(string grainId)
    {
        try
        {
            var gAgentFactory = new GAgentFactory(_clusterClient);
            var resultGAgent = await gAgentFactory.GetGAgentAsync<IResultGAgent>(Guid.Parse(grainId));
            var state = await resultGAgent.GetStateAsync();

            return Ok(new
            {
                success = true,
                state = new
                {
                    result = state.Result,
                    executionId = state.ExecutionId,
                    hasResult = !string.IsNullOrEmpty(state.Result)
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Find GAgents that support a specific event type
    /// </summary>
    [HttpGet("find-by-event")]
    public async Task<IActionResult> FindGAgentsByEventType([FromQuery] string eventTypeName)
    {
        if (string.IsNullOrEmpty(eventTypeName))
        {
            return BadRequest(new { success = false, error = "EventTypeName is required" });
        }

        try
        {
            var grain = await GetGAgentInfoGrainAsync();
            var grainTypes = await grain.FindGAgentsByEventTypeAsync(eventTypeName);

            // For each matching grain type, get detailed info
            var matchingGAgents = new List<object>();

            foreach (var grainType in grainTypes)
            {
                var detailInfo = await grain.GetGAgentDetailInfoAsync(grainType);
                if (detailInfo != null)
                {
                    var matchingEventName = detailInfo.EventTypes.FirstOrDefault(e =>
                        e.EndsWith(eventTypeName, StringComparison.OrdinalIgnoreCase));

                    if (matchingEventName != null)
                    {
                        matchingGAgents.Add(new
                        {
                            grainType = grainType,
                            eventType = new
                            {
                                name = matchingEventName.Split('.').Last(),
                                fullName = matchingEventName
                            }
                        });
                    }
                }
            }

            return Ok(new
            {
                success = true,
                total = matchingGAgents.Count,
                gAgents = matchingGAgents
            });
        }
        catch (Exception ex)
        {
            if (_environment.IsDevelopment())
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }

            return StatusCode(500, new
            {
                success = false,
                error = "Failed to find GAgents by event type"
            });
        }
    }
}

/// <summary>
/// Request model for executing a GAgent
/// </summary>
public class ExecuteGAgentRequest
{
    public string GrainType { get; set; } = string.Empty;
    public string EventTypeName { get; set; } = string.Empty;
    public string? EventParameters { get; set; }
}