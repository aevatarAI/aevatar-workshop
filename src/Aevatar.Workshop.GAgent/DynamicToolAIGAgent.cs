using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.MCP.GAgents;
using Aevatar.GAgents.MCP.Model;
using Aevatar.GAgents.MCP.Options;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aevatar.Workshop.GAgent;

public interface IDynamicToolAIGAgent : IAIGAgent, IStateGAgent<DynamicToolAIGAgentState>
{
    Task<bool> ConfigureServersAsync(List<MCPServerConfig> servers);
    Task<bool> ConfigureBrainAsync(string systemLLM);
    Task<List<MCPToolInfo>> GetAvailableToolsAsync();
    Task<string> ChatAsync(string message);
    Task<ChatWithDetailsResponse> ChatWithDetailsAsync(string message);
    Task<List<GAgentDetailInfo>> GetAvailableGAgentsAsync();
    Task<bool> ConfigureGAgentToolsAsync(List<GrainType> selectedGAgents);
}

[GenerateSerializer]
public class DynamicToolAIGAgentState : AIGAgentStateBase
{
    [Id(0)] public Dictionary<string, MCPGAgentReference> MCPAgents { get; set; } = new();
    [Id(1)] public List<GrainType> SelectedGAgents { get; set; } = new();
    [Id(2)] public Dictionary<string, string> GAgentToolMapping { get; set; } = new(); // Maps kernel function names to GAgent info
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

[GenerateSerializer]
public class ChatWithDetailsResponse
{
    [Id(0)] public string Response { get; set; } = string.Empty;
    [Id(1)] public List<ToolCallDetail> ToolCalls { get; set; } = new();
    [Id(2)] public long TotalDurationMs { get; set; }
}

[GenerateSerializer]
public class ToolCallDetail
{
    [Id(0)] public string ToolName { get; set; } = string.Empty;
    [Id(1)] public string ServerName { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, object> Arguments { get; set; } = new();
    [Id(3)] public string Result { get; set; } = string.Empty;
    [Id(4)] public bool Success { get; set; }
    [Id(5)] public long DurationMs { get; set; }
    [Id(6)] public string Timestamp { get; set; } = string.Empty;
}

[GAgent("dynamictoolai", "demo")]
public class DynamicToolAIGAgent : AIGAgentBase<DynamicToolAIGAgentState, DynamicToolAIGAgentStateLogEvent>,
    IDynamicToolAIGAgent
{
    private readonly ILogger<DynamicToolAIGAgent> _logger;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentService _gAgentService;
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly IBrainFactory _brainFactory;
    private Dictionary<string, string> _toolNameMapping = new(); // Maps kernel function names to MCP tool names
    private List<ToolCallDetail> _currentToolCalls = new(); // Track tool calls for current request

    public DynamicToolAIGAgent()
    {
        _gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
        _logger = ServiceProvider.GetRequiredService<ILogger<DynamicToolAIGAgent>>();
        _gAgentService = ServiceProvider.GetRequiredService<IGAgentService>();
        _gAgentExecutor = ServiceProvider.GetRequiredService<IGAgentExecutor>();
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Dynamic AI agent that can use MCP tools at runtime");
    }
    
    /// <summary>
    /// Configure the brain with a specific LLM system
    /// </summary>
    public async Task<bool> ConfigureBrainAsync(string systemLLM)
    {
        try
        {
            Logger.LogInformation("Configuring brain with system LLM: {SystemLLM}", systemLLM);
            
            // Set the system LLM
            State.SystemLLM = systemLLM;
            
            // Set a default prompt template if not already set
            if (string.IsNullOrEmpty(State.PromptTemplate))
            {
                State.PromptTemplate = @"You are a helpful AI assistant with access to various tools through MCP (Model Context Protocol) and other GAgent tools.

When asked to perform tasks, use the available tools to help provide accurate and complete responses.

Before using any tool:
- Check the tool's description to understand what it does
- Review the parameter descriptions to understand what inputs are required
- Use the exact tool names and parameter names as provided

Always explain what tools you're using and why.
When using tools, be clear about the results and how they help answer the user's question.";
            }
            
            RaiseEvent(new DynamicToolAIGAgentStateLogEvent());
            await ConfirmEvents();
            
            // Manually trigger brain initialization by calling the activation logic
            await OnAIGAgentActivateAsync(CancellationToken.None);
            
            // Check if brain was initialized successfully
            if (GetKernelFromBrain() != null)
            {
                Logger.LogInformation("Brain configured and initialized successfully");
                return true;
            }
            else
            {
                Logger.LogError("Failed to initialize brain for system LLM: {SystemLLM}", systemLLM);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure brain");
            return false;
        }
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

    public async Task<List<GAgentDetailInfo>> GetAvailableGAgentsAsync()
    {
        try
        {
            // Get all available GAgents
            var allGAgentInfos = await _gAgentService.GetAllAvailableGAgentInformation();
            var gAgentList = new List<GAgentDetailInfo>();

            // Filter out self and MCP-related agents
            var selfGrainType = this.GetGrainId().Type;
            
            foreach (var (grainType, eventTypes) in allGAgentInfos)
            {
                // Skip self and MCP agents
                var grainTypeString = grainType.ToString();
                if (grainType.Equals(selfGrainType) || 
                    grainTypeString.Contains("MCP", StringComparison.OrdinalIgnoreCase) ||
                    grainTypeString.Contains("DynamicTool", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Get detailed info
                var detailInfo = await _gAgentService.GetGAgentDetailInfoAsync(grainType);
                if (detailInfo != null)
                {
                    gAgentList.Add(detailInfo);
                }
            }

            Logger.LogInformation($"Found {gAgentList.Count} available GAgents for tool usage");
            return gAgentList;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get available GAgents");
            return new List<GAgentDetailInfo>();
        }
    }

    public async Task<bool> ConfigureGAgentToolsAsync(List<GrainType> selectedGAgents)
    {
        try
        {
            State.SelectedGAgents = selectedGAgents;
            
            RaiseEvent(new DynamicToolAIGAgentStateLogEvent());
            await ConfirmEvents();

            // Update kernel tools if brain is initialized
            if (GetKernelFromBrain() != null)
            {
                await UpdateKernelToolsAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to configure GAgent tools");
            return false;
        }
    }

    public async Task<string> ChatAsync(string message)
    {
        var detailedResponse = await ChatWithDetailsAsync(message);
        return detailedResponse.Response;
    }

    public async Task<ChatWithDetailsResponse> ChatWithDetailsAsync(string message)
    {
        var response = new ChatWithDetailsResponse();
        var overallStartTime = DateTime.UtcNow;
        
        // Clear tool calls from previous request
        _currentToolCalls.Clear();

        try
        {
            Logger.LogInformation("Processing chat message with details: {Message}", message);
            
            // Get the kernel from brain
            var kernel = GetKernelFromBrain();
            if (kernel == null)
            {
                Logger.LogWarning("Kernel not available, falling back to base implementation");
                var fallbackHistory = await ChatWithHistory(message);
                response.Response = fallbackHistory?.LastOrDefault()?.Content ?? "No response generated.";
                response.TotalDurationMs = (long)(DateTime.UtcNow - overallStartTime).TotalMilliseconds;
                return response;
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
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, // Auto-invoke tools
                Temperature = 0.1,
                MaxTokens = 2000
            };
            
            // Get response with automatic tool invocation
            Logger.LogInformation("[{Timestamp}] Starting LLM call with auto tool invocation",
                DateTime.UtcNow.ToString("HH:mm:ss.fff"));
            
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            
            var chatResponse = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel,
                cts.Token);
            
            response.Response = chatResponse.Content ?? "I couldn't generate a response.";
            
            // Copy collected tool calls to response
            response.ToolCalls = new List<ToolCallDetail>(_currentToolCalls);
            
            response.TotalDurationMs = (long)(DateTime.UtcNow - overallStartTime).TotalMilliseconds;
            
            Logger.LogInformation("[{Timestamp}] Chat completed with {ToolCount} tool calls in {Duration}ms",
                DateTime.UtcNow.ToString("HH:mm:ss.fff"),
                response.ToolCalls.Count,
                response.TotalDurationMs);
            
            return response;
        }
        catch (TaskCanceledException)
        {
            var timeoutDuration = DateTime.UtcNow - overallStartTime;
            Logger.LogError("Chat completion timed out after {Duration}ms", timeoutDuration.TotalMilliseconds);
            response.Response = $"Request timed out after {timeoutDuration.TotalSeconds:F1} seconds. Please try again.";
            response.TotalDurationMs = (long)timeoutDuration.TotalMilliseconds;
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during chat with details");
            response.Response = $"Error: {ex.Message}";
            response.TotalDurationMs = (long)(DateTime.UtcNow - overallStartTime).TotalMilliseconds;
            return response;
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
        // Register GAgent tools
        if (State.SelectedGAgents != null && State.SelectedGAgents.Any())
        {
            var gAgentFunctions = new List<KernelFunction>();
            
            foreach (var grainType in State.SelectedGAgents)
            {
                try
                {
                    // Get GAgent information
                    var gAgentInfo = await _gAgentService.GetGAgentDetailInfoAsync(grainType);
                    if (gAgentInfo == null) continue;

                    // Get event types for this GAgent
                    var allGAgentInfos = await _gAgentService.GetAllAvailableGAgentInformation();
                    if (!allGAgentInfos.TryGetValue(grainType, out var eventTypes)) continue;

                    foreach (var eventType in eventTypes)
                    {
                        // Create function name
                        var functionName = GenerateFunctionName(grainType, eventType);
                        
                        // Store mapping
                        State.GAgentToolMapping[functionName] = $"{grainType}|{eventType.FullName}";

                        // Get event properties to create parameters
                        var eventProperties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanWrite && p.Name != "CorrelationId" && p.Name != "PublisherGrainId")
                            .ToList();

                        // Create function with dynamic parameter binding
                        var function = KernelFunctionFactory.CreateFromMethod(
                            async (KernelArguments args) => 
                            {
                                // Convert KernelArguments to JSON for GAgent
                                var parameters = new Dictionary<string, object>();
                                foreach (var (key, value) in args)
                                {
                                    if (value != null)
                                    {
                                        parameters[key] = ConvertJsonElementToBasicType(value);
                                    }
                                }
                                
                                var parametersJson = JsonSerializer.Serialize(parameters);
                                return await CallGAgentToolAsync(grainType, eventType, parametersJson);
                            },
                            functionName: functionName,
                            description: GenerateFunctionDescription(grainType, eventType, gAgentInfo.Description),
                            parameters: eventProperties.Select(p => 
                            {
                                // Try to get the Description attribute
                                var descriptionAttr = p.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                                var description = descriptionAttr?.Description ?? 
                                                 $"Parameter {p.Name} of type {p.PropertyType.Name}";
                                
                                return new KernelParameterMetadata(p.Name)
                                {
                                    Description = description,
                                    IsRequired = !IsNullableType(p.PropertyType),
                                    ParameterType = p.PropertyType
                                };
                            }).ToArray()
                        );

                        gAgentFunctions.Add(function);
                        Logger.LogInformation($"Registered GAgent tool: {functionName} for {grainType}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Failed to register GAgent tools for {grainType}");
                }
            }

            if (gAgentFunctions.Any())
            {
                kernel.Plugins.AddFromFunctions("GAgentTools", gAgentFunctions);
                Logger.LogInformation($"Registered {gAgentFunctions.Count} GAgent tools");
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
        var toolStartTime = DateTime.UtcNow;
        var toolDetail = new ToolCallDetail
        {
            ServerName = serverName,
            ToolName = toolName,
            Timestamp = toolStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            Arguments = new Dictionary<string, object>()
        };
        
        if (!State.MCPAgents.TryGetValue(serverName, out var agentRef))
        {
            toolDetail.Result = $"Error: Server {serverName} not found";
            toolDetail.Success = false;
            toolDetail.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
            _currentToolCalls.Add(toolDetail);
            return toolDetail.Result;
        }

        try
        {
            // Convert KernelArguments to Dictionary for MCP
            var toolArgs = new Dictionary<string, object>();
            foreach (var (key, value) in kernelArgs)
            {
                if (value != null)
                {
                    // Convert JsonElement to basic types
                    var convertedValue = ConvertJsonElementToBasicType(value);
                    toolArgs[key] = convertedValue;
                    toolDetail.Arguments[key] = convertedValue;
                }
            }
            
            Logger.LogInformation("[{Timestamp}] Executing tool: {Server}.{Tool} with args: {Args}",
                DateTime.UtcNow.ToString("HH:mm:ss.fff"),
                serverName, toolName,
                JsonSerializer.Serialize(toolArgs));

            // Get the MCP agent and call the tool
            var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPGAgent>(agentRef.AgentId);
            
            // Call the tool through the MCP agent
            var response = await mcpAgent.CallToolAsync(serverName, toolName, toolArgs);
            
            if (response.Success)
            {
                // Return the result as a string
                if (response.Result is string strResult)
                {
                    toolDetail.Result = strResult;
                }
                else if (response.Result != null)
                {
                    // Serialize complex results to JSON
                    toolDetail.Result = JsonSerializer.Serialize(response.Result, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                }
                else
                {
                    toolDetail.Result = "Tool executed successfully but returned no result.";
                }
                toolDetail.Success = true;
            }
            else
            {
                toolDetail.Result = response.ErrorMessage ?? "Unknown error occurred";
                toolDetail.Success = false;
            }
            
            toolDetail.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
            _currentToolCalls.Add(toolDetail);
            
            Logger.LogInformation("[{Timestamp}] Tool execution completed in {Duration}ms: {Server}.{Tool}",
                DateTime.UtcNow.ToString("HH:mm:ss.fff"),
                toolDetail.DurationMs,
                serverName, toolName);
            
            return toolDetail.Result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error calling tool {toolName} on server {serverName}");
            toolDetail.Result = $"Error: {ex.Message}";
            toolDetail.Success = false;
            toolDetail.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
            _currentToolCalls.Add(toolDetail);
            return toolDetail.Result;
        }
    }

    private async Task<string> CallGAgentToolAsync(GrainType grainType, Type eventType, string parametersJson)
    {
        var toolStartTime = DateTime.UtcNow;
        var toolDetail = new ToolCallDetail
        {
            ServerName = "GAgent",
            ToolName = $"{grainType.ToString()}.{eventType.Name}",
            Timestamp = toolStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            Arguments = new Dictionary<string, object>()
        };

        try
        {
            Logger.LogInformation("[{Timestamp}] Executing GAgent tool: {GrainType}.{EventType} with params: {Params}",
                DateTime.UtcNow.ToString("HH:mm:ss.fff"),
                grainType, eventType.Name, parametersJson);

            // Parse parameters if provided
            if (!string.IsNullOrEmpty(parametersJson))
            {
                try
                {
                    var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson);
                    if (parameters != null)
                    {
                        // Convert any JsonElement values to basic types
                        var convertedParams = new Dictionary<string, object>();
                        foreach (var (key, value) in parameters)
                        {
                            convertedParams[key] = ConvertJsonElementToBasicType(value);
                        }
                        toolDetail.Arguments = convertedParams;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to parse parameters as JSON, using as string");
                    toolDetail.Arguments["value"] = parametersJson;
                }
            }

            // Create the event instance
            EventBase? @event = Activator.CreateInstance(eventType) as EventBase;
            if (@event == null)
            {
                throw new InvalidOperationException($"Failed to create instance of event type {eventType.Name}");
            }
            
            // Set properties from parameters if provided
            if (!string.IsNullOrEmpty(parametersJson))
            {
                try
                {
                    // Parse the JSON parameters
                    using var doc = JsonDocument.Parse(parametersJson);
                    var root = doc.RootElement;
                    
                    // If it's an object, map properties
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in root.EnumerateObject())
                        {
                            // Find the corresponding property on the event type
                            var eventProperty = eventType.GetProperty(property.Name, 
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                            
                            if (eventProperty != null && eventProperty.CanWrite)
                            {
                                // Convert the JSON value to the property type
                                var value = ConvertJsonElementToPropertyType(property.Value, eventProperty.PropertyType);
                                eventProperty.SetValue(@event, value);
                                
                                Logger.LogDebug("Set property {PropertyName} = {Value} on event {EventType}", 
                                    property.Name, value, eventType.Name);
                            }
                        }
                    }
                    // If it's a simple value, try to find a single writable property
                    else
                    {
                        var writableProperties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanWrite && p.Name != "CorrelationId" && p.Name != "PublisherGrainId")
                            .ToList();
                            
                        if (writableProperties.Count == 1)
                        {
                            var value = ConvertJsonElementToPropertyType(root, writableProperties[0].PropertyType);
                            writableProperties[0].SetValue(@event, value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to set event properties from parameters JSON: {Json}", parametersJson);
                }
            }

            // Execute the GAgent event handler
            var result = await _gAgentExecutor.ExecuteGAgentEventHandler(grainType, @event);

            toolDetail.Result = result ?? "No result returned";
            toolDetail.Success = true;
            toolDetail.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
            _currentToolCalls.Add(toolDetail);

            Logger.LogInformation("[{Timestamp}] GAgent tool execution completed in {Duration}ms: {GrainType}.{EventType}",
                DateTime.UtcNow.ToString("HH:mm:ss.fff"),
                toolDetail.DurationMs,
                grainType, eventType.Name);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling GAgent tool {EventType} on {GrainType}", eventType.Name, grainType);
            toolDetail.Result = $"Error: {ex.Message}";
            toolDetail.Success = false;
            toolDetail.DurationMs = (long)(DateTime.UtcNow - toolStartTime).TotalMilliseconds;
            _currentToolCalls.Add(toolDetail);
            return toolDetail.Result;
        }
    }

    private string GenerateFunctionName(GrainType grainType, Type eventType)
    {
        // Semantic Kernel function names can only contain ASCII letters, digits, and underscores
        var cleanGrainType = grainType.ToString()!
            .Replace("/", "_")
            .Replace(".", "_")
            .Replace("-", "_");
        
        return $"{cleanGrainType}_{eventType.Name}";
    }

    private string GenerateFunctionDescription(GrainType grainType, Type eventType, string gAgentDescription)
    {
        return $"Execute {eventType.Name} on {grainType.ToString()} GAgent. {gAgentDescription}";
    }

    private KernelParameterMetadata[] ConvertToKernelParameters(Dictionary<string, MCPParameterInfo> mcpParameters)
    {
        var parameters = new List<KernelParameterMetadata>();

        foreach (var (name, param) in mcpParameters)
        {
            parameters.Add(new KernelParameterMetadata(name)
            {
                Description = param.Description,
                DefaultValue = param.DefaultValue != null ? ConvertJsonElementToBasicType(param.DefaultValue) : null,
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
        
        // Register MCP tools after brain initialization if we have any configured
        if (State.MCPAgents.Any() && GetKernelFromBrain() != null)
        {
            await UpdateKernelToolsAsync();
        }
    }
    
    private object ConvertJsonElementToBasicType(object value)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString()!,
                JsonValueKind.Number => jsonElement.TryGetInt64(out var longValue) ? longValue :
                                       jsonElement.TryGetDouble(out var doubleValue) ? doubleValue : 
                                       jsonElement.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                JsonValueKind.Array => jsonElement.EnumerateArray()
                    .Select(e => ConvertJsonElementToBasicType(e))
                    .ToList(),
                JsonValueKind.Object => jsonElement.EnumerateObject()
                    .ToDictionary(prop => prop.Name, prop => ConvertJsonElementToBasicType(prop.Value)),
                _ => jsonElement.ToString()
            };
        }
        
        // If it's a dictionary, recursively convert its values
        if (value is Dictionary<string, object> dict)
        {
            return dict.ToDictionary(kvp => kvp.Key, kvp => ConvertJsonElementToBasicType(kvp.Value));
        }
        
        // If it's a list, recursively convert its items
        if (value is IEnumerable list && value is not string && value is not Dictionary<string, object>)
        {
            return list.Cast<object>().Select(ConvertJsonElementToBasicType).ToList();
        }
        
        return value;
    }
    
    private object? ConvertJsonElementToPropertyType(JsonElement element, Type targetType)
    {
        try
        {
            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (element.ValueKind == JsonValueKind.Null)
                    return null;
                targetType = Nullable.GetUnderlyingType(targetType)!;
            }

            // Handle null values
            if (element.ValueKind == JsonValueKind.Null)
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            // Handle string type
            if (targetType == typeof(string))
            {
                return element.GetString();
            }

            // Handle numeric types
            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                return element.GetInt32();
            }
            if (targetType == typeof(long) || targetType == typeof(long?))
            {
                return element.GetInt64();
            }
            if (targetType == typeof(double) || targetType == typeof(double?))
            {
                return element.GetDouble();
            }
            if (targetType == typeof(float) || targetType == typeof(float?))
            {
                return (float)element.GetDouble();
            }
            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
            {
                return element.GetDecimal();
            }

            // Handle boolean type
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                return element.GetBoolean();
            }

            // Handle DateTime
            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                return element.GetDateTime();
            }

            // Handle Guid
            if (targetType == typeof(Guid) || targetType == typeof(Guid?))
            {
                return element.GetGuid();
            }

            // For complex types, try to deserialize
            var json = element.GetRawText();
            return JsonSerializer.Deserialize(json, targetType);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to convert JsonElement to type {TargetType}", targetType.Name);
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }
    
    private static bool IsNullableType(Type type)
    {
        return !type.IsValueType || 
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
    }
}

[GenerateSerializer]
public class MCPGAgentReference
{
    [Id(0)] public Guid AgentId { get; set; }
    [Id(1)] public string ServerName { get; set; } = string.Empty;
    [Id(2)] public string Description { get; set; } = string.Empty;
}