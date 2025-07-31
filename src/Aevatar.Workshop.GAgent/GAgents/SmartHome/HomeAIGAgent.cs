using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.Workshop.GAgent.GAgents.SmartHome;

#region State and Events

/// <summary>
/// AI smart home assistant state
/// </summary>
[GenerateSerializer]
public class HomeAIGAgentState : AIGAgentStateBase
{
    [Id(0)] public bool Initialized { get; set; }
    [Id(1)] public List<string> ChatHistory { get; set; } = new();
    [Id(2)] public Dictionary<string, int> CommandHistory { get; set; } = new(); // Command type -> count
    [Id(3)] public DateTime LastCommandAt { get; set; } = DateTime.MinValue;
}

/// <summary>
/// State log event base class
/// </summary>
[GenerateSerializer]
public class HomeAIStateLogEvent : StateLogEventBase<HomeAIStateLogEvent>;

/// <summary>
/// System initialization event
/// </summary>
[GenerateSerializer]
public class HomeAIInitializedLogEvent : HomeAIStateLogEvent
{
    [Id(0)] public string LLMSystem { get; set; } = string.Empty;
    [Id(1)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// AI reset event
/// </summary>
[GenerateSerializer]
public class HomeAIResetLogEvent : HomeAIStateLogEvent
{
    [Id(0)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// Chat message record event
/// </summary>
[GenerateSerializer]
public class ChatMessageLogEvent : HomeAIStateLogEvent
{
    [Id(0)] public string Role { get; set; } = string.Empty;
    [Id(1)] public string Message { get; set; } = string.Empty;
    [Id(2)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// Command parsing event
/// </summary>
[GenerateSerializer]
public class CommandParsedLogEvent : HomeAIStateLogEvent
{
    [Id(0)] public string CommandType { get; set; } = string.Empty;
    [Id(1)] public string OriginalMessage { get; set; } = string.Empty;
    [Id(2)] public DateTime Timestamp { get; set; }
}

#endregion

#region Interface

/// <summary>
/// Smart home AI assistant interface
/// </summary>
public interface IHomeAIGAgent : IStateGAgent<HomeAIGAgentState>, IAIGAgent
{
    Task<bool> InitializeAsync(string llmSystem);
    Task<ChatWithDetailsResponse> ProcessCommandAsync(string userInput);
    Task<List<string>> GetChatHistoryAsync();
    Task<bool> IsInitializedAsync();
}

#endregion

#region Implementation

/// <summary>
/// Smart home AI assistant GAgent
/// </summary>
[GAgent("home.ai", "smarthome")]
public class HomeAIGAgent : WorkshopAIGAgentBase<HomeAIGAgentState, HomeAIStateLogEvent>, IHomeAIGAgent
{
    #region Device ID Constants
    private static readonly Guid LIGHT_ID = "light agent".ToGuid();
    private static readonly Guid THERMOSTAT_ID = "thermostat agent".ToGuid();
    private static readonly Guid SECURITY_ID = "security agent".ToGuid();
    private static readonly Guid CURTAIN_ID = "curtain agent".ToGuid();
    #endregion

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("AI-powered smart home assistant that understands natural language commands " +
                          "and controls home devices through intent recognition.");

    #region Public Methods

    public async Task<bool> InitializeAsync(string llmSystem)
    {
        try
        {
            Logger.LogInformation("Initializing HomeAIGAgent with LLM system: {System}", llmSystem);
            
            // If already initialized with the same LLM system, skip initialization
            if (State.Initialized && State.SystemLLM == llmSystem)
            {
                Logger.LogInformation("HomeAIGAgent already initialized with the same LLM system: {System}", llmSystem);
                return true;
            }
            
            // Reset initialization state if we're reinitializing (either different LLM or failed previous init)
            if (State.Initialized || !string.IsNullOrEmpty(State.SystemLLM))
            {
                Logger.LogInformation("Resetting HomeAIGAgent state before reinitializing with LLM system: {System}", llmSystem);
                RaiseEvent(new HomeAIResetLogEvent { Timestamp = DateTime.UtcNow });
                await ConfirmEvents();
            }

            // Use the base class SetSystemLLM to configure AI
            await SetSystemLLMAsync(llmSystem);

            // Initialize with home-specific instructions
            var initDto = new InitializeDto
            {
                LLMConfig = new LLMConfigDto { SystemLLM = llmSystem },
                Instructions = @"You are a smart home AI assistant that helps users control home devices through natural language.

You can control the following smart devices:
1. Lighting system - Turn lights on/off, adjust brightness
2. Temperature control system - Set temperature, switch modes
3. Security system - Arm/disarm
4. Curtain system - Open/close curtains, adjust position

When users request to control devices, you need to:
1. Identify the target device type (lighting/temperature/security/curtain)
2. Understand the specific operation (on/off/adjust/set etc.)
3. Call the corresponding GAgent tool function

Important: Each device is an independent GAgent, you need to control them through tool calls.

Light control examples:
- User says: 'turn on light' → Call LightGAgent's TurnOnLightCommand
- User says: 'turn off light' → Call LightGAgent's TurnOffLightCommand  
- User says: 'set light to 20%' → Call LightGAgent's SetBrightnessCommand, parameter Brightness=20
- User says: 'dim the light' → First get current brightness, then call SetBrightnessCommand to decrease brightness value

Temperature control examples:
- User says: 'set temperature to 22 degrees' → Call ThermostatGAgent's SetTemperatureCommand, parameter Temperature=22.0
- User says: 'turn on cooling' → Call ThermostatGAgent's ChangeModeCommand, parameter Mode='Cooling'

Security control examples:
- User says: 'arm security' → Call SecurityGAgent's ArmSecurityCommand
- User says: 'disable alarm' → Call SecurityGAgent's DisarmSecurityCommand

Curtain control examples:
- User says: 'open curtains' → Call CurtainGAgent's OpenCurtainCommand
- User says: 'set curtains to 50%' → Call CurtainGAgent's SetCurtainPositionCommand, parameter Position=50

Scene modes:
- Good morning mode: Lights on (100%), temperature 22°C, security disarmed, curtains open (100%)
- Good night mode: Lights off, temperature 20°C, security armed, curtains closed (0%)
- Movie mode: Lights 20%, temperature 21°C, security disarmed, curtains closed (0%)

Important: When calling device commands, you must use the correct device ID:
- Light device LightId: """ + LIGHT_ID.ToString("N") + @"""
- Temperature device ThermostatId: """ + THERMOSTAT_ID.ToString("N") + @"""
- Security device SecuritySystemId: """ + SECURITY_ID.ToString("N") + @"""
- Curtain device CurtainId: """ + CURTAIN_ID.ToString("N") + @"""

Notes:
1. Brightness and curtain position range is 0-100
2. Temperature range is 16-30°C
3. Device IDs must match exactly, otherwise commands will be ignored
4. Please understand user intent flexibly based on natural language, don't mechanically match keywords",
                ToolGAgents =
                [
                    GrainId.Create("smarthome.light", LIGHT_ID.ToString("N")),
                    GrainId.Create("smarthome.thermostat", THERMOSTAT_ID.ToString("N")),
                    GrainId.Create("smarthome.security", SECURITY_ID.ToString("N")),
                    GrainId.Create("smarthome.curtain", CURTAIN_ID.ToString("N"))
                ]
            };

            // Use base class InitializeAsync
            var success = await base.InitializeAsync(initDto);
            if (!success)
            {
                throw new InvalidOperationException("Failed to initialize AI agent");
            }

            // Update state
            RaiseEvent(new HomeAIInitializedLogEvent 
            { 
                LLMSystem = llmSystem,
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();

            Logger.LogInformation("HomeAIGAgent initialized successfully with GAgent tools");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize HomeAIGAgent");
            return false;
        }
    }

    /// <summary>
    /// Process user's natural language commands
    /// </summary>
    public async Task<ChatWithDetailsResponse> ProcessCommandAsync(string userInput)
    {
        if (!State.Initialized)
        {
            return new ChatWithDetailsResponse 
            { 
                Response = "I'm not ready yet. Please wait for initialization to complete.",
                TotalDurationMs = 0,
                ToolCalls = []
            };
        }

        try
        {
            Logger.LogInformation("Processing user command: {Command}", userInput);

            // Record the message
            RaiseEvent(new ChatMessageLogEvent
            {
                Role = "user",
                Message = userInput,
                Timestamp = DateTime.UtcNow
            });

            // Build chat history from state
            var history = new List<ChatMessage>();
            foreach (var msg in State.ChatHistory.TakeLast(6))
            {
                var parts = msg.Split(':', 2);
                if (parts.Length == 2)
                {
                    var role = parts[0].ToLower();
                    var content = parts[1].Trim();
                    if (role == "user")
                        history.Add(new ChatMessage { ChatRole = ChatRole.User, Content = content });
                    else if (role == "assistant")
                        history.Add(new ChatMessage { ChatRole = ChatRole.Assistant, Content = content });
                }
            }

            // Use the base class method that handles tool calling automatically
            var result = await ChatWithHistoryAndToolsAsync(
                userInput,
                history,
                new ExecutionPromptSettings { Temperature = "0.3", MaxToken = 300 }
            );

            // Record assistant response
            RaiseEvent(new ChatMessageLogEvent
            {
                Role = "assistant",
                Message = result.Response ?? "I couldn't process that command.",
                Timestamp = DateTime.UtcNow
            });

            await ConfirmEvents();

            Logger.LogInformation("AI response: {Response}, Tool calls: {ToolCallCount}", 
                result.Response, result.ToolCalls?.Count ?? 0);
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing command: {Command}", userInput);
            return new ChatWithDetailsResponse
            {
                Response = $"I'm sorry, I encountered an error processing your request: {ex.Message}",
                TotalDurationMs = 0,
                ToolCalls = new List<ToolCallDetail>()
            };
        }
    }

    public Task<List<string>> GetChatHistoryAsync()
        => Task.FromResult(State.ChatHistory.ToList());

    public Task<bool> IsInitializedAsync()
        => Task.FromResult(State.Initialized);

    #endregion

    #region Private Methods

    #endregion

    #region State Transitions

    protected override void AIGAgentTransitionState(HomeAIGAgentState state, StateLogEventBase<HomeAIStateLogEvent> @event)
    {
        switch (@event)
        {
            case HomeAIInitializedLogEvent e:
                state.Initialized = true;
                state.SystemLLM = e.LLMSystem;
                Logger.LogDebug("HomeAIGAgent initialized with LLM: {LLM}", e.LLMSystem);
                break;

            case HomeAIResetLogEvent e:
                state.Initialized = false;
                state.SystemLLM = string.Empty;
                state.ChatHistory.Clear();
                Logger.LogDebug("HomeAIGAgent reset at {Timestamp}", e.Timestamp);
                break;

            case ChatMessageLogEvent e:
                var entry = $"{e.Role}: {e.Message}";
                state.ChatHistory.Add(entry);
                
                // Keep only last 20 messages
                if (state.ChatHistory.Count > 20)
                {
                    state.ChatHistory.RemoveAt(0);
                }
                
                Logger.LogDebug("Chat message recorded: {Role} - {Message}", e.Role, e.Message);
                break;

            case CommandParsedLogEvent e:
                state.LastCommandAt = e.Timestamp;
                
                // Update command history count
                if (state.CommandHistory.ContainsKey(e.CommandType))
                {
                    state.CommandHistory[e.CommandType]++;
                }
                else
                {
                    state.CommandHistory[e.CommandType] = 1;
                }
                
                Logger.LogDebug("Command parsed: {Type} - {Message}", e.CommandType, e.OriginalMessage);
                break;
        }
    }

    #endregion
}

#endregion 