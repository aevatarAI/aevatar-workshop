using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AI.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Orleans;
using Volo.Abp.Guids;

namespace Aevatar.Workshop.GAgent;

/// <summary>
/// Simple AI agent state for tool calling demo
/// </summary>
[GenerateSerializer]
public class ToolCallingAIGAgentState : StateBase
{
    [Id(0)] public bool Initialized { get; set; }
    [Id(1)] public string LLMSystem { get; set; } = "OpenAI";
    [Id(2)] public List<string> ChatHistory { get; set; } = new();
    [Id(3)] public List<string> RegisteredTools { get; set; } = new();
}

/// <summary>
/// State log events
/// </summary>
[GenerateSerializer]
public class ToolCallingStateLogEvent : StateLogEventBase<ToolCallingStateLogEvent>;

[GenerateSerializer]
public class InitializedLogEvent : ToolCallingStateLogEvent
{
    [Id(0)] public string LLMSystem { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ChatMessageLogEvent : ToolCallingStateLogEvent
{
    [Id(0)] public string Role { get; set; } = string.Empty;
    [Id(1)] public string Message { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ToolRegisteredLogEvent : ToolCallingStateLogEvent
{
    [Id(0)] public string ToolName { get; set; } = string.Empty;
}

/// <summary>
/// Interface for the tool calling AI agent
/// </summary>
public interface IToolCallingAIGAgent : IStateGAgent<ToolCallingAIGAgentState>
{
    Task InitializeAsync(string llmSystem);
    Task<string> ChatAsync(string message);
    Task<List<string>> GetRegisteredToolsAsync();
}

/// <summary>
/// Simple AI agent that demonstrates tool calling with MathGAgent and TimeConverterGAgent
/// </summary>
[GAgent("toolcalling.ai", "ai")]
public class ToolCallingAIGAgent : GAgentBase<ToolCallingAIGAgentState, ToolCallingStateLogEvent>, IToolCallingAIGAgent
{
    private Kernel? _kernel;
    private IGAgentFactory? _gAgentFactory;
    private IMathGAgent? _mathGAgent;
    private ITimeConverterGAgent? _timeGAgent;

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("AI agent that can use MathGAgent and TimeConverterGAgent as tools");
    }

    public async Task InitializeAsync(string llmSystem)
    {
        try
        {
            Logger.LogInformation("Initializing ToolCallingAIGAgent with LLM system: {System}", llmSystem);

            // Get system LLM configuration
            var systemConfigs = ServiceProvider.GetRequiredService<IOptions<SystemLLMConfigOptions>>();
            if (systemConfigs.Value.SystemLLMConfigs == null ||
                !systemConfigs.Value.SystemLLMConfigs.TryGetValue(llmSystem, out var config))
            {
                throw new InvalidOperationException($"LLM configuration not found for: {llmSystem}");
            }

            // Build kernel
            _kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: string.IsNullOrEmpty(config.ModelName) ? "gpt-3.5-turbo" : config.ModelName,
                    apiKey: config.ApiKey ?? throw new InvalidOperationException("API key is required"))
                .Build();

            // Register tools
            await RegisterToolsAsync();

            // Update state
            RaiseEvent(new InitializedLogEvent { LLMSystem = llmSystem });
            await ConfirmEvents();

            Logger.LogInformation("ToolCallingAIGAgent initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize ToolCallingAIGAgent");
            throw;
        }
    }

    private async Task RegisterToolsAsync()
    {
        try
        {
            _gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
            
            // Get MathGAgent and TimeConverterGAgent
            _mathGAgent = await _gAgentFactory.GetGAgentAsync<IMathGAgent>(Guid.NewGuid());
            _timeGAgent = await _gAgentFactory.GetGAgentAsync<ITimeConverterGAgent>(Guid.NewGuid());

            var tools = new List<string>();

            // Register MathGAgent functions
            var calculateFunc = KernelFunctionFactory.CreateFromMethod(
                method: async (string expression) =>
                {
                    try
                    {
                        var result = await _mathGAgent.CalculateAsync(expression);
                        return $"The result of {expression} is {result}";
                    }
                    catch (Exception ex)
                    {
                        return $"Error calculating {expression}: {ex.Message}";
                    }
                },
                functionName: "calculate_math",
                description: "Calculate a mathematical expression",
                parameters:
                [
                    new KernelParameterMetadata("expression")
                    {
                        Description = "The mathematical expression to calculate",
                        IsRequired = true,
                        ParameterType = typeof(string)
                    }
                ],
                returnParameter: new KernelReturnParameterMetadata
                {
                    Description = "The calculation result",
                    ParameterType = typeof(string)
                });

            // Register TimeConverterGAgent functions
            var convertTimeFunc = KernelFunctionFactory.CreateFromMethod(
                method: async (string time, string fromZone, string toZone) =>
                {
                    try
                    {
                        var result = await _timeGAgent.ConvertTimeAsync(time, fromZone, toZone);
                        return $"Time conversion result: {result}";
                    }
                    catch (Exception ex)
                    {
                        return $"Error converting time: {ex.Message}";
                    }
                },
                functionName: "convert_time",
                description: "Convert time between timezones",
                parameters:
                [
                    new KernelParameterMetadata("time")
                    {
                        Description = "The time to convert (e.g., '3:00 PM', '15:00')",
                        IsRequired = true,
                        ParameterType = typeof(string)
                    },
                    new KernelParameterMetadata("fromZone")
                    {
                        Description = "Source timezone (e.g., 'EST', 'PST', 'UTC')",
                        IsRequired = true,
                        ParameterType = typeof(string)
                    },
                    new KernelParameterMetadata("toZone")
                    {
                        Description = "Target timezone (e.g., 'EST', 'PST', 'UTC')",
                        IsRequired = true,
                        ParameterType = typeof(string)
                    }
                ],
                returnParameter: new KernelReturnParameterMetadata
                {
                    Description = "The converted time",
                    ParameterType = typeof(string)
                });

            var getTimeInZoneFunc = KernelFunctionFactory.CreateFromMethod(
                method: async (string timeZone) =>
                {
                    try
                    {
                        var result = await _timeGAgent.GetTimeInZoneAsync(timeZone);
                        return $"Current time in {timeZone}: {result}";
                    }
                    catch (Exception ex)
                    {
                        return $"Error getting time in {timeZone}: {ex.Message}";
                    }
                },
                functionName: "get_time_in_zone",
                description: "Get current time in a specific timezone",
                parameters:
                [
                    new KernelParameterMetadata("timeZone")
                    {
                        Description = "The timezone to get current time for (e.g., 'EST', 'PST', 'UTC', 'JST')",
                        IsRequired = true,
                        ParameterType = typeof(string)
                    }
                ],
                returnParameter: new KernelReturnParameterMetadata
                {
                    Description = "The current time in the specified timezone",
                    ParameterType = typeof(string)
                });

            // Add functions to kernel as a plugin
            var functions = new[] { calculateFunc, convertTimeFunc, getTimeInZoneFunc };
            _kernel!.Plugins.AddFromFunctions("GAgentTools", functions);

            // Track registered tools
            tools.Add("calculate_math");
            tools.Add("convert_time");
            tools.Add("get_time_in_zone");

            foreach (var tool in tools)
            {
                RaiseEvent(new ToolRegisteredLogEvent { ToolName = tool });
            }

            Logger.LogInformation("Registered {Count} tools successfully", tools.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register tools");
            throw;
        }
    }

    public async Task<string> ChatAsync(string message)
    {
        if (!State.Initialized || _kernel == null)
        {
            return "Agent not initialized. Please initialize first.";
        }

        try
        {
            Logger.LogInformation("Processing chat message: {Message}", message);

            // Add user message to history
            RaiseEvent(new ChatMessageLogEvent { Role = "user", Message = message });

            // Get chat completion service
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            
            // Create chat history
            var chatHistory = new ChatHistory();
            
            // Add system message
            chatHistory.AddSystemMessage(
                "You are a helpful AI assistant with access to mathematical calculation and time conversion tools. " +
                "When users ask you to calculate math expressions, use the calculate_math tool. " +
                "When users ask about time conversions or time in different timezones, use the convert_time or get_time_in_zone tools. " +
                "Always explain what tool you're using and why.");

            // Add conversation history
            foreach (var msg in State.ChatHistory.TakeLast(10)) // Keep last 10 messages for context
            {
                var parts = msg.Split(':', 2);
                if (parts.Length == 2)
                {
                    var role = parts[0].ToLower();
                    var content = parts[1].Trim();
                    if (role == "user")
                        chatHistory.AddUserMessage(content);
                    else if (role == "assistant")
                        chatHistory.AddAssistantMessage(content);
                }
            }

            // Add current user message
            chatHistory.AddUserMessage(message);

            // Configure execution settings for automatic tool calling
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // Get response with automatic tool invocation
            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel);

            var responseText = response.Content ?? "I couldn't generate a response.";

            // Add assistant message to history
            RaiseEvent(new ChatMessageLogEvent { Role = "assistant", Message = responseText });

            await ConfirmEvents();

            Logger.LogInformation("Chat response generated successfully");
            return responseText;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing chat message");
            return $"Error: {ex.Message}";
        }
    }

    public Task<List<string>> GetRegisteredToolsAsync()
    {
        return Task.FromResult(State.RegisteredTools.ToList());
    }

    protected override void GAgentTransitionState(ToolCallingAIGAgentState state, StateLogEventBase<ToolCallingStateLogEvent> @event)
    {
        switch (@event)
        {
            case InitializedLogEvent init:
                state.Initialized = true;
                state.LLMSystem = init.LLMSystem;
                break;
            case ChatMessageLogEvent chat:
                state.ChatHistory.Add($"{chat.Role}: {chat.Message}");
                if (state.ChatHistory.Count > 50) // Keep only last 50 messages
                {
                    state.ChatHistory.RemoveAt(0);
                }
                break;
            case ToolRegisteredLogEvent tool:
                if (!state.RegisteredTools.Contains(tool.ToolName))
                {
                    state.RegisteredTools.Add(tool.ToolName);
                }
                break;
        }
    }
} 