using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.MCP.GAgents;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Orleans;

namespace Aevatar.Workshop.GAgent;

public interface IDynamicToolAIGAgent : IAIGAgent, IStateGAgent<DynamicToolAIGAgentState>
{
    Task<bool> ConfigureServersAsync(List<MCPServerConfig> servers);
    Task<List<MCPToolInfo>> GetAvailableToolsAsync();
    Task<string> ChatAsync(string message);
}

[GenerateSerializer]
public class DynamicToolAIGAgentState : AIGAgentStateBase
{
    [Id(0)] public Dictionary<string, MCPGAgentReference> MCPAgents { get; set; } = new();
}

[GenerateSerializer]
public class DynamicToolAIGAgentStateLogEvent : StateLogEventBase<DynamicToolAIGAgentStateLogEvent>
{
}

[GenerateSerializer]
public class ConfigureServersStateLogEvent : StateLogEventBase<DynamicToolAIGAgentStateLogEvent>
{
    [Id(0)] public Dictionary<string, MCPGAgentReference> Servers { get; set; } = new();
}

[GenerateSerializer]
public class ToolCalledStateLogEvent : StateLogEventBase<DynamicToolAIGAgentStateLogEvent>
{
    [Id(0)] public string ToolName { get; set; } = string.Empty;
    [Id(1)] public Dictionary<string, MCPToolInfo> Tools { get; set; } = new();
}

[GAgent("dynamictoolai", "demo")]
public class DynamicToolAIGAgent : AIGAgentBase<DynamicToolAIGAgentState, DynamicToolAIGAgentStateLogEvent>,
    IDynamicToolAIGAgent
{
    private readonly ILogger<DynamicToolAIGAgent> _logger;
    private readonly IGAgentFactory _gAgentFactory;
    private Dictionary<string, string> _toolNameMapping = new(); // Maps kernel function names to MCP tool names

    public DynamicToolAIGAgent()
    {
        _gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
        _logger = ServiceProvider.GetRequiredService<ILogger<DynamicToolAIGAgent>>();
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Dynamic AI agent that can use MCP tools at runtime");
    }
    
    /// <summary>
    /// After brain initialization, register MCP tools
    /// </summary>
    private async Task RegisterMCPToolsAfterBrainInit()
    {
        if (State.MCPAgents.Any())
        {
            await UpdateKernelToolsAsync();
        }
    }

    public async Task<bool> ConfigureServersAsync(List<MCPServerConfig> servers)
    {
        var mcpAgents = new Dictionary<string, MCPGAgentReference>();

        try
        {
            foreach (var server in servers)
            {
                // Create config for the MCP agent
                var mcpConfig = new MCPGAgentConfig
                {
                    Servers = [server]
                };
                
                var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(mcpConfig);
                var mcpAgentId = mcpAgent.GetPrimaryKey();

                State.MCPAgents[server.ServerName] = new MCPGAgentReference
                {
                    AgentId = mcpAgentId,
                    ServerName = server.ServerName,
                    Description = server.ServerName // Use server name as description
                };

                // Get tools from this server
                var serverTools = await mcpAgent.GetAvailableToolsAsync();
                foreach (var (_, tool) in serverTools)
                {
                    var toolKey = $"{server.ServerName}.{tool.Name}";
                    Logger.LogInformation($"Registered tool: {toolKey} - {tool.Description}");
                }
            }

            RaiseEvent(new ConfigureServersStateLogEvent { Servers = State.MCPAgents });
            await ConfirmEvents();

            // Update Kernel tools after configuring servers
            // Only update if brain is initialized
            if (GetKernelFromBrain() != null)
            {
                await UpdateKernelToolsAsync();
            }
            else
            {
                Logger.LogWarning("Brain not initialized yet. Tools will be registered after brain initialization.");
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure MCP servers");
            return false;
        }
    }

    public async Task<List<MCPToolInfo>> GetAvailableToolsAsync()
    {
        var allTools = new List<MCPToolInfo>();

        foreach (var (serverName, agentRef) in State.MCPAgents)
        {
            try
            {
                var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(agentRef.AgentId);
                var tools = await mcpAgent.GetAvailableToolsAsync();
                
                foreach (var (_, tool) in tools)
                {
                    allTools.Add(tool);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to get tools from server {serverName}");
            }
        }

        return allTools;
    }

    public async Task<string> ChatAsync(string message)
    {
        try
        {
            Logger.LogInformation("Processing chat message: {Message}", message);
            
            // Get the kernel from brain
            var kernel = GetKernelFromBrain();
            if (kernel == null)
            {
                Logger.LogWarning("Kernel not available, falling back to base implementation");
                var fallbackHistory = await ChatWithHistory(message);
                return fallbackHistory?.LastOrDefault()?.Content ?? "No response generated.";
            }
            
            // If we have MCP agents but no tools registered yet, register them now
            if (State.MCPAgents.Any() && !kernel.Plugins.Any())
            {
                Logger.LogInformation("Brain is now initialized, registering MCP tools");
                await UpdateKernelToolsAsync();
            }
            
            // Get chat completion service from kernel
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            
            // Create chat history
            var chatHistory = new ChatHistory();
            
            // Add system message
            var systemMessage = State.PromptTemplate ?? 
                "You are a helpful AI assistant with access to various tools through MCP (Model Context Protocol). " +
                "When asked to perform tasks, use the available tools to help provide accurate and complete responses. " +
                "Always explain what tools you're using and why. " +
                "When using tools, be clear about the results and how they help answer the user's question.";
            
            chatHistory.AddSystemMessage(systemMessage);
            
            // Add user message
            chatHistory.AddUserMessage(message);
            
            Logger.LogInformation("Available tools in kernel: {Tools}",
                string.Join(", ", kernel.Plugins.SelectMany(p => p.Select(f => f.Name))));
            
            // Configure execution settings for automatic tool calling
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.1, // Lower temperature for more deterministic tool usage
                MaxTokens = 2000
            };
            
            // Get response with automatic tool invocation
            var startTime = DateTime.UtcNow;
            Logger.LogInformation("[{Timestamp}] Starting LLM call with auto tool invocation",
                startTime.ToString("HH:mm:ss.fff"));
            
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); // 2 minutes timeout
            try
            {
                var response = await chatService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings,
                    kernel,
                    cts.Token);
                
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;
                Logger.LogInformation("[{Timestamp}] Received response from LLM after {Duration}ms",
                    endTime.ToString("HH:mm:ss.fff"), duration.TotalMilliseconds);
                
                var responseText = response.Content ?? "I couldn't generate a response.";
                
                // Log if tools were called
                if (response.Metadata?.TryGetValue("ToolCalls", out var toolCalls) == true)
                {
                    Logger.LogInformation("[{Timestamp}] Tools were called during this request",
                        DateTime.UtcNow.ToString("HH:mm:ss.fff"));
                }
                
                Logger.LogInformation("Chat completed successfully");
                return responseText;
            }
            catch (TaskCanceledException)
            {
                var timeoutDuration = DateTime.UtcNow - startTime;
                Logger.LogError("Chat completion timed out after {Duration}ms", timeoutDuration.TotalMilliseconds);
                return $"Request timed out after {timeoutDuration.TotalSeconds:F1} seconds. Please try again.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during chat");
            return $"Error: {ex.Message}";
        }
    }

    private async Task UpdateKernelToolsAsync()
    {
        // Get the kernel from the brain if it exists
        var kernel = GetKernelFromBrain();
        if (kernel == null)
        {
            Logger.LogWarning("Cannot update kernel tools: Brain not initialized or kernel not accessible");
            return;
        }

        // Clear the tool name mapping
        _toolNameMapping.Clear();

        // Register MCP tools as kernel functions
        foreach (var (serverName, agentRef) in State.MCPAgents)
        {
            try
            {
                var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(agentRef.AgentId);
                var tools = await mcpAgent.GetAvailableToolsAsync();

                var functions = new List<KernelFunction>();

                foreach (var (toolName, tool) in tools)
                {
                    // Semantic Kernel function names can only contain ASCII letters, digits, and underscores
                    // Replace dots with underscores for the kernel function name
                    var mcpToolFullName = $"{serverName}.{toolName}";
                    var kernelFunctionName = $"{serverName}_{toolName}".Replace(".", "_").Replace("-", "_");
                    
                    // Store the mapping for later use
                    _toolNameMapping[kernelFunctionName] = mcpToolFullName;
                    
                    var function = KernelFunctionFactory.CreateFromMethod(
                        async (KernelArguments args) => await CallMCPToolAsync(serverName, toolName, args),
                        functionName: kernelFunctionName,
                        description: tool.Description,
                        parameters: ConvertToKernelParameters(tool.Parameters)
                    );

                    functions.Add(function);
                    Logger.LogInformation($"Registered tool: {kernelFunctionName} (MCP: {mcpToolFullName})");
                }

                if (functions.Any())
                {
                    kernel.Plugins.AddFromFunctions(serverName, functions);
                    Logger.LogInformation($"Registered {functions.Count} tools from server {serverName}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to register tools from server {serverName}");
            }
        }
    }
    
    /// <summary>
    /// Gets the Semantic Kernel from the brain using reflection
    /// </summary>
    private Kernel? GetKernelFromBrain()
    {
        try
        {
            // Use reflection to access the private _brain field from base class
            // Need to search through the inheritance hierarchy
            var currentType = GetType();
            FieldInfo? brainField = null;
            
            while (currentType != null && brainField == null)
            {
                brainField = currentType.GetField("_brain", 
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                
                if (brainField == null)
                {
                    currentType = currentType.BaseType;
                }
            }
            
            if (brainField == null)
            {
                Logger.LogWarning("Cannot find _brain field in base class hierarchy");
                return null;
            }
            
            var brain = brainField.GetValue(this);
            if (brain == null)
            {
                Logger.LogWarning("Brain is not initialized yet");
                return null;
            }

            // Try to get Kernel property or field
            var brainType = brain.GetType();
            
            // First try property
            var kernelProperty = brainType.GetProperty("Kernel",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (kernelProperty != null)
            {
                return kernelProperty.GetValue(brain) as Kernel;
            }

            // Then try field
            var kernelField = brainType.GetField("Kernel",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (kernelField != null)
            {
                return kernelField.GetValue(brain) as Kernel;
            }

            Logger.LogWarning("Cannot find Kernel field or property in brain type {BrainType}", brainType.Name);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error accessing Kernel from brain");
            return null;
        }
    }

    private async Task<string> CallMCPToolAsync(string serverName, string toolName, KernelArguments kernelArgs)
    {
        if (!State.MCPAgents.TryGetValue(serverName, out var agentRef))
        {
            return $"Error: Server {serverName} not found";
        }

        try
        {
            // Convert KernelArguments to Dictionary for MCP
            var toolArgs = new Dictionary<string, object>();
            foreach (var (key, value) in kernelArgs)
            {
                if (value != null)
                {
                    toolArgs[key] = value;
                }
            }

            // Get the MCP agent and call the tool
            var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(agentRef.AgentId);
            
            // Call the tool through the MCP agent
            var response = await mcpAgent.CallToolAsync(serverName, toolName, toolArgs);
            
            if (response.Success)
            {
                // Return the result as a string
                if (response.Result is string strResult)
                {
                    return strResult;
                }
                else if (response.Result != null)
                {
                    // Serialize complex results to JSON
                    return JsonSerializer.Serialize(response.Result, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                }
                else
                {
                    return "Tool executed successfully but returned no result.";
                }
            }
            else
            {
                return $"Error: {response.ErrorMessage ?? "Unknown error occurred"}";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error calling tool {toolName} on server {serverName}");
            return $"Error: {ex.Message}";
        }
    }

    private KernelParameterMetadata[] ConvertToKernelParameters(Dictionary<string, MCPParameterInfo> mcpParameters)
    {
        var parameters = new List<KernelParameterMetadata>();

        foreach (var (name, param) in mcpParameters)
        {
            parameters.Add(new KernelParameterMetadata(name)
            {
                Description = param.Description,
                DefaultValue = param.DefaultValue,
                IsRequired = param.Required
            });
        }

        return parameters.ToArray();
    }

    protected override void AIGAgentTransitionState(DynamicToolAIGAgentState state,
        StateLogEventBase<DynamicToolAIGAgentStateLogEvent> @event)
    {
        switch (@event)
        {
            case ConfigureServersStateLogEvent configEvent:
                state.MCPAgents = configEvent.Servers;
                break;
        }
    }

    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnAIGAgentActivateAsync(cancellationToken);
        
        // If we have MCP agents configured and brain is initialized, update kernel tools
        if (State.MCPAgents.Any() && GetKernelFromBrain() != null)
        {
            await UpdateKernelToolsAsync();
        }
    }
}

[GenerateSerializer]
public class MCPGAgentReference
{
    [Id(0)] public Guid AgentId { get; set; }
    [Id(1)] public string ServerName { get; set; } = string.Empty;
    [Id(2)] public string Description { get; set; } = string.Empty;
}