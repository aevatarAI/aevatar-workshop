using System.Text.Json;
using Aevatar.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using PsiGAgent.Common;
using PsiGAgent.Common.Models;
using PsiGAgent.Omni;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PsiGAgentDemoController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<PsiGAgentDemoController> _logger;
    
    // Store agent IDs in memory for demo purposes
    private static readonly Dictionary<string, string> _agentSessions = new();
    
    public PsiGAgentDemoController(
        IGAgentFactory gAgentFactory,
        ILogger<PsiGAgentDemoController> logger)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
    }
    
    [HttpPost("create")]
    public async Task<IActionResult> CreateAgent([FromBody] CreateAgentRequest request)
    {
        try
        {
            // Create PsiOmniGAgent
            var psi = await _gAgentFactory.GetGAgentAsync("omni", "psi");
            var publisher = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>(Guid.NewGuid());
            
            // Configure the agent
            var config = GetAgentConfiguration();
            await publisher.PublishEventAsync(new AgentConfigEvent
            {
                Configuration = config,
                Tools = []
            }, psi);
            
            var agentId = psi.GetGrainId().ToString();
            var sessionId = Guid.NewGuid().ToString();
            _agentSessions[sessionId] = agentId;
            
            _logger.LogInformation("Created PsiGAgent with ID: {AgentId}, Session: {SessionId}", agentId, sessionId);
            
            return Ok(new { sessionId, agentId });
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
            var publisher = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>(Guid.NewGuid());
            
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
            var state = (PsiOmniGAgentState)await psi.GetStateAsync();
            
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
    
    private AgentConfiguration GetAgentConfiguration()
    {
        // Try to get configuration from environment variables
        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var modelId = Environment.GetEnvironmentVariable("OPENAI_MODEL_ID") ?? "gpt-4o-mini";
        
        var modelConfig = new ModelConfiguration
        {
            ModelId = modelId,
            ApiKey = openAiApiKey ?? "your-api-key-here"
        };
        
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