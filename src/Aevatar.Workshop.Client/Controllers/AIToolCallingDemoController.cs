using Microsoft.AspNetCore.Mvc;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Aevatar.Workshop.GAgent;
using Microsoft.Extensions.Configuration;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIToolCallingDemoController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<AIToolCallingDemoController> _logger;
    private readonly IConfiguration _configuration;

    public AIToolCallingDemoController(
        IGAgentFactory gAgentFactory,
        ILogger<AIToolCallingDemoController> logger,
        IConfiguration configuration)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get available LLM systems from configuration
    /// </summary>
    [HttpGet("llm-systems")]
    public IActionResult GetLLMSystems()
    {
        try
        {
            var llmSystemsSection = _configuration.GetSection("SystemLLMConfigs");
            var systems = new List<object>();

            foreach (var systemSection in llmSystemsSection.GetChildren())
            {
                systems.Add(new
                {
                    name = systemSection.Key,
                    provider = systemSection["ProviderEnum"] ?? "Unknown",
                    model = systemSection["DeploymentOrModelId"] ?? systemSection["ModelName"] ?? "Unknown",
                    description = $"{systemSection.Key} - {systemSection["DeploymentOrModelId"] ?? systemSection["ModelName"] ?? "Unknown"}"
                });
            }

            return Ok(new
            {
                success = true,
                systems = systems
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get LLM systems");
            return Ok(new
            {
                success = false,
                error = ex.Message,
                systems = new List<object>()
            });
        }
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
            _logger.LogInformation("Processing chat message: {Message}", request.Message);
            var response = await agent.ChatAsync(request.Message);
            _logger.LogInformation("Received response: {Response}", response);
            
            // Get tool call history directly from agent
            var toolCallHistory = await agent.GetToolCallHistoryAsync();
            
            // Get recent tool calls for immediate display
            var recentToolCalls = toolCallHistory
                .OrderByDescending(h => h.Timestamp)
                .Take(5)
                .Select(h => new
                {
                    tool = h.ToolName,
                    timestamp = h.Timestamp,
                    input = h.Input,
                    output = h.Output
                })
                .ToList();
            
            return Ok(new
            {
                success = true,
                response = response,
                toolCallsCount = toolCallHistory.Count,
                recentToolCalls = recentToolCalls
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
    public async Task<IActionResult> GetToolCallHistory(string agentId)
    {
        if (!Guid.TryParse(agentId, out var id))
        {
            return BadRequest(new { success = false, error = "Invalid agent ID" });
        }

        var agent = await _gAgentFactory.GetGAgentAsync<IToolCallingAIGAgent>(id);
        var toolCallHistory = await agent.GetToolCallHistoryAsync();
        
        return Ok(new
        {
            success = true,
            agentId = agentId,
            toolCalls = toolCallHistory.OrderByDescending(h => h.Timestamp).Take(20).Select(h => new
            {
                tool = h.ToolName,
                function = h.ToolName,
                parameters = h.Input,
                input = h.Input, // Add both for compatibility
                output = h.Output,
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
                        
                        // Track this manual tool call if agent ID is provided
                        if (!string.IsNullOrEmpty(request.AgentId) && Guid.TryParse(request.AgentId, out var agentId))
                        {
                            _logger.LogInformation("Manual tool test for agent {AgentId}", agentId);
                        }
                        
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
        public string? AgentId { get; set; }
    }

    public class DemoToolsRequest
    {
        public bool DemoMath { get; set; }
        public bool DemoTime { get; set; }
    }
} 