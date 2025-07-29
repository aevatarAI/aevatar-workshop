using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.MCP.Options;
using Aevatar.Workshop.GAgent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DynamicAIMCPController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<DynamicAIMCPController> _logger;
    private readonly IConfiguration _configuration;

    public DynamicAIMCPController(IGAgentFactory gAgentFactory, ILogger<DynamicAIMCPController> logger,
        IConfiguration configuration)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("mcp-servers")]
    public async Task<IActionResult> GetMCPServers()
    {
        try
        {
            // For now, use local configuration directly
            // In a production scenario, you might want to get this from ConfigManagerGAgent
            // using a proper request/response pattern or dedicated methods
            
            var mcpServersSection = _configuration.GetSection("MCPServers");
            var servers = new List<MCPServerUIConfig>();

            foreach (var serverSection in mcpServersSection.GetChildren())
            {
                var serverConfig = new MCPServerUIConfig
                {
                    Name = serverSection.Key,
                    Command = serverSection["command"] ?? string.Empty,
                    Args = serverSection.GetSection("args").Get<List<string>>() ?? new List<string>(),
                    Description = serverSection["description"] ?? string.Empty,
                    Icon = serverSection["icon"] ?? "🛠️", // Default icon if not specified
                    Env = serverSection.GetSection("env").Get<Dictionary<string, string>>(),
                    Url = serverSection["url"],
                    InitialDelayMs = serverSection.GetValue<int?>("initialDelayMs"),
                    MaxRetries = serverSection.GetValue<int?>("maxRetries")
                };

                // Only include enabled servers
                if (serverSection.GetValue<bool>("enabled", true))
                {
                    servers.Add(serverConfig);
                }
            }

            return Ok(new
            {
                success = true,
                servers = servers.Select(s => new
                {
                    name = s.Name,
                    command = s.Command,
                    args = s.Args,
                    description = s.Description,
                    icon = s.Icon,
                    env = s.Env,
                    url = s.Url
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MCP servers configuration");
            return Ok(new
            {
                success = false,
                error = ex.Message,
                servers = new List<MCPServerUIConfig>()
            });
        }
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeAgent([FromBody] InitializeAgentRequest request)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>();
            var agentId = agent.GetPrimaryKey();

            // Initialize the agent with system prompt
            var systemPrompt =
                @"You are a helpful AI assistant with access to various tools through MCP (Model Context Protocol).
When asked to perform tasks, use the available tools to help provide accurate and complete responses.
Always explain what tools you're using and why.
When using tools, be clear about the results and how they help answer the user's question.";

            await agent.InitializeAsync(new InitializeDto
            {
                Instructions = systemPrompt,
                LLMConfig = new LLMConfigDto
                {
                    SystemLLM = request.SystemLLM // Use the selected LLM from the request
                }
            });

            // Also explicitly configure the brain to ensure it's initialized
            await agent.ConfigureBrainAsync(request.SystemLLM);

            return Ok(new
            {
                success = true,
                agentId = agentId.ToString(),
                message = "Agent created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing agent");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("configure-servers")]
    public async Task<IActionResult> ConfigureServers([FromBody] ConfigureServersRequest request)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>(Guid.Parse(request.AgentId));

            // Configure MCP servers
            var servers = request.Servers.Select(s =>
            {
                return new MCPServerConfig
                {
                    ServerName = s.ServerName,
                    Command = s.Command,
                    Args = s.Args?.ToList() ?? new List<string>(),
                    Env = s.Env?.ToDictionary(kv => kv.Key, kv => kv.Value) ??
                          new Dictionary<string, string>(),
                    Description = s.Description ?? string.Empty,
                };
            }).ToList();

            var success = await agent.ConfigureMCPServersAsync(servers);

            if (!success)
            {
                return BadRequest("Failed to configure MCP servers");
            }

            // Get available tools
            var tools = await agent.GetAvailableMCPToolsAsync();

            return Ok(new
            {
                success = true,
                message = $"Configured {servers.Count} servers",
                availableTools = tools.GroupBy(t => t.ServerName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(t => new
                        {
                            name = t.Name,
                            description = t.Description
                        }).ToList()
                    )
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring servers");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("available-tools/{agentId}")]
    public async Task<IActionResult> GetAvailableTools(string agentId)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>(Guid.Parse(agentId));
            var tools = await agent.GetAvailableMCPToolsAsync();

            return Ok(new
            {
                success = true,
                tools = tools
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available tools");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>(Guid.Parse(request.AgentId));

            // Ensure brain is configured (this is idempotent, so safe to call multiple times)
            var state = await agent.GetStateAsync();
            if (!string.IsNullOrEmpty(state.SystemLLM))
            {
                await agent.ConfigureBrainAsync(state.SystemLLM);
            }
            else
            {
                // Use default if not configured
                await agent.ConfigureBrainAsync("DeepSeek");
            }

            // Process the chat message
            var response = await agent.ChatAsync(request.Message);

            return Ok(new
            {
                success = true,
                response = response,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("chat-with-details")]
    public async Task<IActionResult> ChatWithDetails([FromBody] ChatRequest request)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>(Guid.Parse(request.AgentId));

            // Ensure brain is configured (this is idempotent, so safe to call multiple times)
            var state = await agent.GetStateAsync();
            if (!string.IsNullOrEmpty(state.SystemLLM))
            {
                await agent.ConfigureBrainAsync(state.SystemLLM);
            }
            else
            {
                // Use default if not configured
                await agent.ConfigureBrainAsync("DeepSeek");
            }

            // Process the chat message with tool call details
            var detailedResponse = await agent.ChatWithDetailsAsync(request.Message);

            return Ok(new
            {
                success = true,
                response = detailedResponse.Response,
                toolCalls = detailedResponse.ToolCalls,
                totalDurationMs = detailedResponse.TotalDurationMs,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat with details");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("configured-servers/{agentId}")]
    public async Task<IActionResult> GetConfiguredServers(string agentId)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>(Guid.Parse(agentId));
            var state = await agent.GetStateAsync();

            return Ok(new
            {
                success = true,
                servers = state.MCPAgents
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configured servers");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("conversation-history/{agentId}")]
    public async Task<IActionResult> GetConversationHistory(string agentId)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>(Guid.Parse(agentId));
            var state = await agent.GetStateAsync();

            return Ok(new
            {
                success = true,
                history = new List<object>() // Conversation history not implemented yet
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("clear-history")]
    public async Task<IActionResult> ClearHistory([FromBody] ClearHistoryRequest request)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>(Guid.Parse(request.AgentId));
            // Clear conversation history - not implemented yet

            return Ok(new
            {
                success = true,
                message = "Conversation history cleared"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing history");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("available-gagents")]
    public async Task<IActionResult> GetAvailableGAgents()
    {
        try
        {
            // Create a temporary agent to get available GAgents
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>();
            var gAgents = await agent.GetAvailableGAgentsAsync();

            return Ok(new
            {
                success = true,
                gAgents = gAgents.Select(g =>
                {
                    // Parse alias and namespace from GrainType
                    var grainTypeString = g.GrainType.ToString();
                    var parts = grainTypeString.Split('/');
                    var alias = parts.Length > 0 ? parts[0] : grainTypeString;
                    var nameSpace = parts.Length > 1 ? parts[1] : "default";

                    return new
                    {
                        grainType = grainTypeString,
                        alias = alias,
                        nameSpace = nameSpace,
                        description = g.Description,
                        eventHandlers = g.SupportedEventTypes.Select(e => new
                        {
                            eventType = e.Name,
                            fullName = e.FullName
                        }).ToList()
                    };
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available GAgents");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("configure-gagent-tools")]
    public async Task<IActionResult> ConfigureGAgentTools([FromBody] ConfigureGAgentToolsRequest request)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>(Guid.Parse(request.AgentId));

            // Convert string grain types to GrainType objects
            var grainTypes = request.ToolGAgents.Select(GrainType.Create).ToList();

            var success = await agent.ConfigureGAgentToolsAsync(grainTypes);

            if (!success)
            {
                return BadRequest("Failed to configure GAgent tools");
            }

            // Get all available tools (MCP + GAgent)
            var mcpTools = await agent.GetAvailableMCPToolsAsync();

            // Get configured GAgent info for response
            var allGAgents = await agent.GetAvailableGAgentsAsync();
            var configuredGAgents = allGAgents
                .Where(g => request.ToolGAgents.Contains(g.GrainType.ToString()))
                .Select(g =>
                {
                    // Parse alias from GrainType
                    var grainTypeString = g.GrainType.ToString();
                    var parts = grainTypeString.Split('/');
                    var alias = parts.Length > 0 ? parts[0] : grainTypeString;

                    return new
                    {
                        grainType = grainTypeString,
                        alias = alias,
                        description = g.Description,
                        eventCount = g.SupportedEventTypes.Count
                    };
                })
                .ToList();

            return Ok(new
            {
                success = true,
                message = $"Configured {request.ToolGAgents.Count} GAgent tools",
                configuredGAgents = configuredGAgents,
                totalAvailableTools = mcpTools.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring GAgent tools");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("agent-info")]
    public async Task<IActionResult> GetAgentInfo([FromQuery] string agentId)
    {
        try
        {
            var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>(Guid.Parse(agentId));
            var state = await agent.GetStateAsync();

            // Get MCP tools
            var mcpTools = await agent.GetAvailableMCPToolsAsync();

            // Create the response
            var response = new
            {
                success = true,
                agentId = agentId,
                systemLLM = state.SystemLLM,
                enableMCPTools = state.EnableMCPTools,
                enableGAgentTools = state.EnableGAgentTools,
                toolGAents = state.ToolGAgents.Select(g => g.ToString()).ToList(),
                registeredGAgentFunctions = state.RegisteredGAgentFunctions,
                mcpTools = mcpTools.Select(t => new
                {
                    serverName = t.ServerName,
                    name = t.Name,
                    description = t.Description
                }).ToList(),
                totalTools = mcpTools.Count + (state.RegisteredGAgentFunctions?.Count ?? 0)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent info");
            return Ok(new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}

public class InitializeAgentRequest
{
    public string SystemLLM { get; set; } = "OpenAI";
}

public class ConfigureServersRequest
{
    public string AgentId { get; set; }
    public List<MCPServerConfigDto> Servers { get; set; }
}

public class MCPServerConfigDto
{
    public string ServerName { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string>? Args { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public int? InitialDelayMs { get; set; }
    public int? MaxRetries { get; set; }
}

public class ChatRequest
{
    public string AgentId { get; set; }
    public string Message { get; set; }
}

public class ClearHistoryRequest
{
    public string AgentId { get; set; }
}

public class ConfigureGAgentToolsRequest
{
    public string AgentId { get; set; }
    public List<string> ToolGAgents { get; set; } = new();
}

public class MCPServerUIConfig
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("command")] public string Command { get; set; } = string.Empty;

    [JsonPropertyName("args")] public List<string> Args { get; set; } = new();

    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    [JsonPropertyName("icon")] public string? Icon { get; set; }

    [JsonPropertyName("env")] public Dictionary<string, string>? Env { get; set; }

    [JsonPropertyName("url")] public string? Url { get; set; }
    
    [JsonPropertyName("initialDelayMs")] public int? InitialDelayMs { get; set; }
    
    [JsonPropertyName("maxRetries")] public int? MaxRetries { get; set; }
}