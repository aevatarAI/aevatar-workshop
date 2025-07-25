using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.PsiOmni;
using Aevatar.GAgents.PsiOmni.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PsiGAgentDemoController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<PsiGAgentDemoController> _logger;
    private readonly IOptions<SystemLLMConfigOptions> _systemLLMConfigs;

    // Store agent IDs in memory for demo purposes
    private static readonly Dictionary<string, string> _agentSessions = new();

    public PsiGAgentDemoController(
        IGAgentFactory gAgentFactory,
        ILogger<PsiGAgentDemoController> logger,
        IOptions<SystemLLMConfigOptions> systemLLMConfigs)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
        _systemLLMConfigs = systemLLMConfigs;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateAgent([FromBody] CreateAgentRequest request)
    {
        try
        {
            // Create PsiOmniGAgent
            var psi = await _gAgentFactory.GetGAgentAsync<IPshOmniGAgent>();
            var publisher = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>(Guid.NewGuid());

            // // Create configuration for the agent with Brain support
            // var psiConfig = new PsiOmniGAgentConfig
            // {
            //     MemberName = "PsiOmniAgent",
            //     Depth = 0, // Root agent
            //     LLMConfig = new LLMConfigDto
            //     {
            //         SystemLLM = request.SystemLLM ?? "gpt-4" // Use Brain mode with system LLM
            //     }
            // };
            //
            // // Configure the agent - this will now initialize the Brain
            // await psi.ConfigAsync(psiConfig);
            
            var config = GetAgentConfiguration(request.SystemLLM);
            await publisher.PublishEventAsync(new AgentConfigEvent
            {
                Configuration = config,
                Tools = []
            }, psi);

            var agentId = psi.GetGrainId().ToString();
            var sessionId = Guid.NewGuid().ToString();
            _agentSessions[sessionId] = agentId;

            _logger.LogInformation("Created PsiGAgent with Brain mode - ID: {AgentId}, Session: {SessionId}, LLM: {LLM}", 
                agentId, sessionId, request.SystemLLM);

            return Ok(new { sessionId, agentId, systemLLM = request.SystemLLM });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PsiGAgent");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            if (!_agentSessions.TryGetValue(request.SessionId, out var agentId))
            {
                return BadRequest(new { error = "Invalid session ID" });
            }

            var guid = GrainId.Parse(agentId);
            var psi = await _gAgentFactory.GetGAgentAsync(guid);
            var publisher = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>();

            var callId = Guid.NewGuid().ToString();
            await publisher.PublishEventAsync(new UserMessageEvent
            {
                TargetAgentId = agentId,
                CallId = callId,
                Content = request.Message,
                ReplyToAgentId = null
            }, psi);

            _logger.LogInformation("Sent message to agent {AgentId}: {Message}", agentId, request.Message);

            return Ok(new { callId, message = "Message sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to PsiGAgent");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("state/{sessionId}")]
    public async Task<IActionResult> GetAgentState(string sessionId)
    {
        try
        {
            if (!_agentSessions.TryGetValue(sessionId, out var agentId))
            {
                return BadRequest(new { error = "Invalid session ID" });
            }

            var guid = GrainId.Parse(agentId);
            var visited = new HashSet<string>();
            var agentStates = await GetAgentStatesRecursive(guid, visited);

            return Ok(agentStates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent state");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<List<AgentStateInfo>> GetAgentStatesRecursive(
        GrainId agentId, HashSet<string> visited, int depth = 0)
    {
        var result = new List<AgentStateInfo>();
        if (!visited.Add(agentId.ToString()))
        {
            return result;
        }

        try
        {
            var psi = await _gAgentFactory.GetGAgentAsync<IStateGAgent<PsiOmniGAgentState>>(agentId);
            var state = await psi.GetStateAsync();

            var agentInfo = new AgentStateInfo
            {
                Id = agentId.ToString(),
                Depth = depth,
                State = new AgentStateDto
                {
                    AgentId = state.AgentId,
                    Description = state.Description,
                    RealizationStatus = state.RealizationStatus.ToString(),
                    Tools = state.Tools?.Select(t => t.Name).ToList() ?? new List<string>(),
                    ChatHistory = state.ChatHistory?.Select(msg => new ChatMessageDto
                    {
                        Role = msg.Role,
                        Content = msg.Content,
                        Timestamp = msg.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList() ?? new List<ChatMessageDto>(),
                    Examples = state.Examples?.Select(ex => new ExampleDto
                    {
                        Request = ex.Request,
                        Response = ex.Response
                    }).ToList() ?? new List<ExampleDto>(),
                    ChildAgents = state.ChildAgents?.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new ChildAgentDto
                        {
                            AgentId = kvp.Value.AgentId,
                            Description = kvp.Value.Description,
                            AgentType = kvp.Value.AgentType
                        }
                    ) ?? new Dictionary<string, ChildAgentDto>()
                }
            };

            result.Add(agentInfo);

            // Recursively get child agent states
            if (state.Children != null && state.Children.Count > 0)
            {
                foreach (var childId in state.Children)
                {
                    var childStates = await GetAgentStatesRecursive(childId, visited, depth + 1);
                    result.AddRange(childStates);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting state for agent {AgentId}", agentId);
        }

        return result;
    }

    private AgentConfiguration GetAgentConfiguration(string systemLLM)
    {
        // Get configuration from SystemLLMConfig
        if (_systemLLMConfigs.Value.SystemLLMConfigs == null ||
            !_systemLLMConfigs.Value.SystemLLMConfigs.TryGetValue(systemLLM, out var llmConfig))
        {
            throw new InvalidOperationException($"LLM configuration not found for: {systemLLM}");
        }

        var modelConfig = new ModelConfiguration
        {
            ModelId = llmConfig.ModelName ?? "gpt-4o-mini",
            ApiKey = llmConfig.ApiKey ?? throw new InvalidOperationException($"API key is required for {systemLLM}")
        };

        // Handle different LLM providers
        switch (llmConfig.ProviderEnum)
        {
            case LLMProviderEnum.Azure:
                modelConfig.Endpoint = llmConfig.Endpoint ?? throw new InvalidOperationException("Endpoint is required for Azure");
                modelConfig.DeploymentName = llmConfig.ModelName;
                break;
                
            case LLMProviderEnum.DeepSeek:
                // DeepSeek uses OpenAI-compatible API but with a different base URL
                // For PsiGAgent, we'll use BaseUrl for DeepSeek instead of Endpoint
                modelConfig.BaseUrl = llmConfig.Endpoint ?? "https://api.deepseek.com";
                break;
                
            case LLMProviderEnum.OpenAI:
            default:
                // OpenAI uses default endpoint, no need to set anything
                break;
        }

        return new AgentConfiguration
        {
            Temperature = 0.7,
            MaxTokens = 2000,
            Model = modelConfig,
            //Depth = 0 // Root agent
        };
    }

    // DTOs
    public class CreateAgentRequest
    {
        public string? InitialTask { get; set; }
        public string? SystemLLM { get; set; }
    }

    public class SendMessageRequest
    {
        public string SessionId { get; set; }
        public string Message { get; set; }
    }

    public class AgentStateInfo
    {
        public string Id { get; set; }
        public int Depth { get; set; }
        public AgentStateDto State { get; set; }
    }

    public class AgentStateDto
    {
        public string AgentId { get; set; }
        public string Description { get; set; }
        public string RealizationStatus { get; set; }
        public List<string> Tools { get; set; }
        public List<ChatMessageDto> ChatHistory { get; set; }
        public List<ExampleDto> Examples { get; set; }
        public Dictionary<string, ChildAgentDto> ChildAgents { get; set; }
    }

    public class ChatMessageDto
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public string Timestamp { get; set; }
    }

    public class ExampleDto
    {
        public string Request { get; set; }
        public string Response { get; set; }
    }

    public class ChildAgentDto
    {
        public string AgentId { get; set; }
        public string Description { get; set; }
        public string AgentType { get; set; }
    }
}