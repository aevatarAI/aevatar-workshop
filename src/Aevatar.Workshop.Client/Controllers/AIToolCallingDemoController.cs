using Microsoft.AspNetCore.Mvc;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Aevatar.Workshop.GAgent;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIToolCallingDemoController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<AIToolCallingDemoController> _logger;
    private static readonly Dictionary<Guid, List<ToolCallInfo>> _toolCallHistory = new();

    public AIToolCallingDemoController(
        IGAgentFactory gAgentFactory,
        ILogger<AIToolCallingDemoController> logger)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
    }

    /// <summary>
    /// Initialize an AI agent with MathGAgent and TimeConverterGAgent tools
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeAIAgent([FromBody] InitializeRequest request)
    {
        try
        {
            // Create a new agent ID
            var agentId = Guid.NewGuid();
            
            // Get the ToolCallingAIGAgent
            var agent = await _gAgentFactory.GetGAgentAsync<IToolCallingAIGAgent>(agentId);
            
            // Initialize with the specified LLM system
            await agent.InitializeAsync(request.LLMSystem);
            
            // Get registered tools
            var tools = await agent.GetRegisteredToolsAsync();
            
            _toolCallHistory[agentId] = new List<ToolCallInfo>();
            
            // Transform tool names into the expected format for frontend
            var availableTools = new List<object>
            {
                new
                {
                    name = "MathGAgent",
                    description = "Mathematical calculation tools",
                    functions = new[] { "calculate_math" }
                },
                new
                {
                    name = "TimeConverterGAgent",
                    description = "Time conversion and timezone tools",
                    functions = new[] { "convert_time", "get_time_in_zone" }
                }
            };
            
            return Ok(new
            {
                success = true,
                agentId = agentId,
                llmSystem = request.LLMSystem,
                availableTools = availableTools,
                message = $"AI agent initialized with {tools.Count} tools"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AI agent");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Chat with the AI agent that can use tools
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        try
        {
            if (!Guid.TryParse(request.AgentId, out var agentId))
            {
                return BadRequest(new { success = false, error = "Invalid agent ID" });
            }

            var agent = await _gAgentFactory.GetGAgentAsync<IToolCallingAIGAgent>(agentId);
            
            // Check if agent is initialized
            var state = await agent.GetStateAsync();
            if (!state.Initialized)
            {
                return BadRequest(new { success = false, error = "Agent not initialized" });
            }

            // Process the chat message
            var response = await agent.ChatAsync(request.Message);
            
            // Track tool calls (simplified tracking based on response content)
            if (_toolCallHistory.TryGetValue(agentId, out var history))
            {
                // Simple heuristic: if response mentions using a tool, track it
                if (response.Contains("calculate_math") || response.Contains("calculating"))
                {
                    history.Add(new ToolCallInfo
                    {
                        ToolName = "calculate_math",
                        Timestamp = DateTime.UtcNow,
                        Input = request.Message,
                        Output = response
                    });
                }
                else if (response.Contains("convert_time") || response.Contains("time conversion"))
                {
                    history.Add(new ToolCallInfo
                    {
                        ToolName = "convert_time",
                        Timestamp = DateTime.UtcNow,
                        Input = request.Message,
                        Output = response
                    });
                }
                else if (response.Contains("get_time_in_zone") || response.Contains("current time"))
                {
                    history.Add(new ToolCallInfo
                    {
                        ToolName = "get_time_in_zone",
                        Timestamp = DateTime.UtcNow,
                        Input = request.Message,
                        Output = response
                    });
                }
            }

            return Ok(new
            {
                success = true,
                response = response,
                toolCallsCount = _toolCallHistory.GetValueOrDefault(agentId)?.Count ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chat message");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get tool call history for an agent
    /// </summary>
    [HttpGet("history/{agentId}")]
    public IActionResult GetToolCallHistory(string agentId)
    {
        if (!Guid.TryParse(agentId, out var id))
        {
            return BadRequest(new { success = false, error = "Invalid agent ID" });
        }

        var history = _toolCallHistory.GetValueOrDefault(id) ?? new List<ToolCallInfo>();
        
        return Ok(new
        {
            success = true,
            agentId = agentId,
            toolCalls = history.OrderByDescending(h => h.Timestamp).Take(20).Select(h => new
            {
                tool = h.ToolName,
                function = h.ToolName,
                parameters = h.Input,
                timestamp = h.Timestamp
            })
        });
    }

    /// <summary>
    /// Demo tools endpoint for testing tools directly
    /// </summary>
    [HttpPost("demo-tools")]
    public async Task<IActionResult> DemoTools([FromBody] DemoToolsRequest request)
    {
        try
        {
            var demos = new List<object>();

            if (request.DemoMath)
            {
                var mathAgent = await _gAgentFactory.GetGAgentAsync<IMathGAgent>(Guid.NewGuid());
                
                // Demo calculations
                var calculations = new[] { "sqrt(25)", "10 * 5", "100 / 4" };
                foreach (var calc in calculations)
                {
                    var result = await mathAgent.CalculateAsync(calc);
                    demos.Add(new
                    {
                        tool = "MathGAgent",
                        operation = calc,
                        result = result
                    });
                }
            }

            if (request.DemoTime)
            {
                var timeAgent = await _gAgentFactory.GetGAgentAsync<ITimeConverterGAgent>(Guid.NewGuid());
                
                // Demo time operations
                var timezones = new[] { "UTC", "EST", "PST" };
                foreach (var tz in timezones)
                {
                    var result = await timeAgent.GetTimeInZoneAsync(tz);
                    demos.Add(new
                    {
                        tool = "TimeConverterGAgent",
                        operation = $"Get time in {tz}",
                        result = result
                    });
                }
            }

            return Ok(new
            {
                success = true,
                demos = demos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run demo tools");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Test tool functionality directly
    /// </summary>
    [HttpPost("test-tool")]
    public async Task<IActionResult> TestTool([FromBody] TestToolRequest request)
    {
        try
        {
            switch (request.ToolName.ToLower())
            {
                case "mathgagent":
                    {
                        var mathAgent = await _gAgentFactory.GetGAgentAsync<IMathGAgent>(Guid.NewGuid());
                        var result = await mathAgent.CalculateAsync(request.Input);
                        return Ok(new { success = true, tool = "MathGAgent", input = request.Input, result = result });
                    }
                case "timeconvertergagent":
                    {
                        var timeAgent = await _gAgentFactory.GetGAgentAsync<ITimeConverterGAgent>(Guid.NewGuid());
                        
                        if (request.Input.Contains("current time in", StringComparison.OrdinalIgnoreCase))
                        {
                            var timezone = request.Input.Replace("current time in", "", StringComparison.OrdinalIgnoreCase).Trim();
                            var result = await timeAgent.GetTimeInZoneAsync(timezone);
                            return Ok(new { success = true, tool = "TimeConverterGAgent", operation = "GetTimeInZone", input = timezone, result = result });
                        }
                        else
                        {
                            return BadRequest(new { success = false, error = "For TimeConverterGAgent, use format: 'current time in [timezone]'" });
                        }
                    }
                default:
                    return BadRequest(new { success = false, error = $"Unknown tool: {request.ToolName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test tool");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    // Request DTOs
    public class InitializeRequest
    {
        public string LLMSystem { get; set; } = "OpenAI";
    }

    public class ChatRequest
    {
        public string AgentId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class TestToolRequest
    {
        public string ToolName { get; set; } = string.Empty;
        public string Input { get; set; } = string.Empty;
    }

    public class DemoToolsRequest
    {
        public bool DemoMath { get; set; }
        public bool DemoTime { get; set; }
    }

    // Tool call tracking
    public class ToolCallInfo
    {
        public string ToolName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
    }
} 