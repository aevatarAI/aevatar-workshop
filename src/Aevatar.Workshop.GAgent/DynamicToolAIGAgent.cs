using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.MCP;
using Aevatar.GAgents.MCP.GAgents;
using Aevatar.GAgents.MCP.GEvents;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.MCP.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MongoDB.Driver.Core.Clusters;
using Orleans;
using Orleans.Runtime;

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
    private Kernel? _kernel;

    public DynamicToolAIGAgent()
    {
        _gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
        _logger = ServiceProvider.GetRequiredService<ILogger<DynamicToolAIGAgent>>();
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Dynamic AI agent that can use MCP tools at runtime");
    }

    public async Task<bool> ConfigureServersAsync(List<MCPServerConfig> servers)
    {
        var mcpAgents = new Dictionary<string, MCPGAgentReference>();

        try
        {
            foreach (var server in servers)
            {
                var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>();
                var mcpAgentId = mcpAgent.GetPrimaryKey();

                // Initialize the MCP agent
                await mcpAgent.ConfigAsync(new MCPGAgentConfig
                {
                    Servers = [server]
                });

                State.MCPAgents[server.ServerName] = new MCPGAgentReference
                {
                    AgentId = mcpAgentId,
                    ServerName = server.ServerName,
                    Description = await mcpAgent.GetDescriptionAsync()
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
            await UpdateKernelToolsAsync();

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
            // Use ChatWithHistory method from AIGAgentBase
            var chatHistory = await ChatWithHistory(message);
            return chatHistory?.LastOrDefault()?.Content ?? "No response generated.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during chat");
            return $"Error: {ex.Message}";
        }
    }

    private async Task UpdateKernelToolsAsync()
    {
        // Create a new kernel for this agent
        _kernel = new Kernel();

        // Clear existing plugins
        _kernel.Plugins.Clear();

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
                    var function = KernelFunctionFactory.CreateFromMethod(
                        async (KernelArguments args) => await CallMCPToolAsync(serverName, toolName, args),
                        functionName: toolName,
                        description: tool.Description,
                        parameters: ConvertToKernelParameters(tool.Parameters)
                    );

                    functions.Add(function);
                }

                if (functions.Any())
                {
                    _kernel.Plugins.AddFromFunctions(serverName, functions);
                    Logger.LogInformation($"Registered {functions.Count} tools from server {serverName}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to register tools from server {serverName}");
            }
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
        
        // If we have MCP agents configured, update kernel tools
        if (State.MCPAgents.Any())
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