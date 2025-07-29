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
/// AI智能家居助手状态
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
/// 状态日志事件基类
/// </summary>
[GenerateSerializer]
public class HomeAIStateLogEvent : StateLogEventBase<HomeAIStateLogEvent>;

/// <summary>
/// 系统初始化事件
/// </summary>
[GenerateSerializer]
public class HomeAIInitializedLogEvent : HomeAIStateLogEvent
{
    [Id(0)] public string LLMSystem { get; set; } = string.Empty;
    [Id(1)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// 聊天消息记录事件
/// </summary>
[GenerateSerializer]
public class ChatMessageLogEvent : HomeAIStateLogEvent
{
    [Id(0)] public string Role { get; set; } = string.Empty;
    [Id(1)] public string Message { get; set; } = string.Empty;
    [Id(2)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// 命令解析事件
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
/// 智能家居AI助手接口
/// </summary>
public interface IHomeAIGAgent : IStateGAgent<HomeAIGAgentState>, IAIGAgent
{
    Task<bool> InitializeAsync(string llmSystem);
    Task<ChatWithDetailsResponse> ProcessCommandAsync(string userInput);
    Task<List<string>> GetChatHistoryAsync();
}

#endregion

#region Implementation

/// <summary>
/// 智能家居AI助手智能体
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

            // Use the base class SetSystemLLM to configure AI
            await SetSystemLLMAsync(llmSystem);

            // Initialize with home-specific instructions
            var initDto = new InitializeDto
            {
                LLMConfig = new LLMConfigDto { SystemLLM = llmSystem },
                Instructions = @"你是一个智能家居AI助手，通过自然语言帮助用户控制家中的设备。

你可以控制以下智能设备：
1. 灯光系统 - 开关灯、调节亮度
2. 温控系统 - 设置温度、切换模式
3. 安防系统 - 布防/撤防
4. 窗帘系统 - 开关窗帘、调节开合度

当用户要求控制设备时，你需要：
1. 识别目标设备类型（灯光/温控/安防/窗帘）
2. 理解具体操作（开/关/调节/设置等）
3. 调用对应的GAgent工具函数

重要：每个设备都是一个独立的GAgent，你需要通过工具调用来控制它们。

灯光控制示例：
- 用户说：'打开灯' → 调用 LightGAgent 的 TurnOnLightCommand
- 用户说：'关闭灯光' → 调用 LightGAgent 的 TurnOffLightCommand  
- 用户说：'把灯调到20%' → 调用 LightGAgent 的 SetBrightnessCommand，参数 Brightness=20
- 用户说：'把灯调暗一点' → 先获取当前亮度，然后调用 SetBrightnessCommand 降低亮度值

温控控制示例：
- 用户说：'设置温度22度' → 调用 ThermostatGAgent 的 SetTemperatureCommand，参数 Temperature=22.0
- 用户说：'打开制冷' → 调用 ThermostatGAgent 的 ChangeModeCommand，参数 Mode='Cooling'

安防控制示例：
- 用户说：'启动安防' → 调用 SecurityGAgent 的 ArmSecurityCommand
- 用户说：'关闭警报' → 调用 SecurityGAgent 的 DisarmSecurityCommand

窗帘控制示例：
- 用户说：'打开窗帘' → 调用 CurtainGAgent 的 OpenCurtainCommand
- 用户说：'把窗帘调到50%' → 调用 CurtainGAgent 的 SetCurtainPositionCommand，参数 Position=50

场景模式：
- 早安模式：开灯(100%)、温度22度、关闭安防、打开窗帘(100%)
- 晚安模式：关灯、温度20度、启动安防、关闭窗帘(0%)
- 观影模式：灯光20%、温度21度、关闭安防、关闭窗帘(0%)

重要：调用设备命令时必须使用正确的设备ID：
- 灯光设备 LightId: """ + LIGHT_ID.ToString("N") + @"""
- 温控设备 ThermostatId: """ + THERMOSTAT_ID.ToString("N") + @"""
- 安防设备 SecuritySystemId: """ + SECURITY_ID.ToString("N") + @"""
- 窗帘设备 CurtainId: """ + CURTAIN_ID.ToString("N") + @"""

注意事项：
1. 亮度和窗帘位置范围都是 0-100
2. 温度范围是 16-30°C
3. 设备ID必须完全匹配，否则命令会被忽略
4. 请根据用户的自然语言灵活理解意图，不要机械匹配关键词",
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
    /// 处理用户的自然语言命令
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