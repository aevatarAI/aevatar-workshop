using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Workshop.GAgent.GAgents.SmartHome;

#region State and Events

/// <summary>
/// 协调器状态
/// </summary>
[GenerateSerializer]
public class HomeCoordinatorState : StateBase
{
    [Id(0)] public Dictionary<string, string> RegisteredDevices { get; set; } = new(); // DeviceType -> GAgentId
    [Id(1)] public Dictionary<string, SceneConfiguration> Scenes { get; set; } = new();
    [Id(2)] public string LastExecutedScene { get; set; } = string.Empty;
    [Id(3)] public DateTime LastSceneExecutedAt { get; set; } = DateTime.MinValue;
    [Id(4)] public List<string> SceneExecutionHistory { get; set; } = new();
}

/// <summary>
/// 场景配置
/// </summary>
[GenerateSerializer]
public class SceneConfiguration
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public Dictionary<string, object> Actions { get; set; } = new();
    [Id(2)] public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 状态日志事件基类
/// </summary>
[GenerateSerializer]
public class HomeCoordinatorStateLogEvent : StateLogEventBase<HomeCoordinatorStateLogEvent>;

/// <summary>
/// 设备注册事件
/// </summary>
[GenerateSerializer]
public class DeviceRegisteredLogEvent : HomeCoordinatorStateLogEvent
{
    [Id(0)] public string DeviceType { get; set; } = string.Empty;
    [Id(1)] public string GAgentId { get; set; } = string.Empty;
    [Id(2)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// 场景执行事件
/// </summary>
[GenerateSerializer]
public class SceneExecutedLogEvent : HomeCoordinatorStateLogEvent
{
    [Id(0)] public string SceneName { get; set; } = string.Empty;
    [Id(1)] public DateTime Timestamp { get; set; }
    [Id(2)] public bool Success { get; set; }
    [Id(3)] public string? ErrorMessage { get; set; }
}

#endregion

#region Interface

/// <summary>
/// 家居协调器接口
/// </summary>
public interface IHomeCoordinatorGAgent : IStateGAgent<HomeCoordinatorState>
{
    Task RegisterDeviceAsync(string deviceType, string gAgentId);
    Task ExecuteSceneAsync(string sceneName);
    Task<List<string>> GetAvailableScenesAsync();
    Task ProcessDeviceControlIntentAsync(DeviceControlIntentEvent intent);
}

#endregion

#region Implementation

/// <summary>
/// 家居协调器智能体
/// </summary>
[GAgent("coordinator", "smarthome")]
public class HomeCoordinatorGAgent : GAgentBase<HomeCoordinatorState, HomeCoordinatorStateLogEvent>, IHomeCoordinatorGAgent
{
    private IGAgentFactory GAgentFactory => 
        ServiceProvider.GetRequiredService<IGAgentFactory>();

    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // 初始化预定义场景
        InitializeScenes();
        
        return base.OnGAgentActivateAsync(cancellationToken);
    }

    private void InitializeScenes()
    {
        if (State.Scenes.Count == 0)
        {
            State.Scenes = new Dictionary<string, SceneConfiguration>
            {
                ["goodnight"] = new SceneConfiguration
                {
                    Name = "goodnight",
                    Description = "Turn off all lights, set temperature to 20°C, arm security, and close curtains",
                    Actions = new Dictionary<string, object>
                    {
                        ["lights"] = "off",
                        ["temperature"] = 20.0,
                        ["security"] = "arm",
                        ["curtain"] = 0  // 关闭窗帘
                    }
                },
                ["晚安"] = new SceneConfiguration
                {
                    Name = "晚安",
                    Description = "关闭所有灯光，设置温度为20°C，启动安防系统",
                    Actions = new Dictionary<string, object>
                    {
                        ["lights"] = "off",
                        ["temperature"] = 20.0,
                        ["security"] = "arm"
                    }
                },
                ["goodmorning"] = new SceneConfiguration
                {
                    Name = "goodmorning",
                    Description = "Turn on lights, set temperature to 22°C, disarm security, and open curtains",
                    Actions = new Dictionary<string, object>
                    {
                        ["lights"] = "on",
                        ["temperature"] = 22.0,
                        ["security"] = "disarm",
                        ["curtain"] = 100  // 打开窗帘
                    }
                },
                ["早安"] = new SceneConfiguration
                {
                    Name = "早安",
                    Description = "打开灯光，设置温度为22°C，关闭安防系统",
                    Actions = new Dictionary<string, object>
                    {
                        ["lights"] = "on",
                        ["temperature"] = 22.0,
                        ["security"] = "disarm"
                    }
                },
                ["away"] = new SceneConfiguration
                {
                    Name = "away",
                    Description = "Turn off all lights, lower temperature, arm security, and close curtains",
                    Actions = new Dictionary<string, object>
                    {
                        ["lights"] = "off",
                        ["temperature"] = 18.0,
                        ["security"] = "arm",
                        ["curtain"] = 0  // 关闭窗帘（外出模式）
                    }
                },
                ["外出"] = new SceneConfiguration
                {
                    Name = "外出",
                    Description = "关闭所有灯光，降低温度，启动安防系统",
                    Actions = new Dictionary<string, object>
                    {
                        ["lights"] = "off",
                        ["temperature"] = 18.0,
                        ["security"] = "arm"
                    }
                },
                ["movie"] = new SceneConfiguration
                {
                    Name = "movie",
                    Description = "Dim lights to 20%, set comfortable temperature, disarm security, and close curtains",
                    Actions = new Dictionary<string, object>
                    {
                        ["lights"] = "dim",
                        ["brightness"] = 20,
                        ["temperature"] = 21.0,
                        ["security"] = "disarm",
                        ["curtain"] = 0  // 关闭窗帘（观影模式）
                    }
                },
                ["观影"] = new SceneConfiguration
                {
                    Name = "观影",
                    Description = "将灯光调暗至20%，设置舒适温度，关闭安防系统",
                    Actions = new Dictionary<string, object>
                    {
                        ["lights"] = "dim",
                        ["brightness"] = 20,
                        ["temperature"] = 21.0,
                        ["security"] = "disarm"
                    }
                },
                ["party"] = new SceneConfiguration
                {
                    Name = "party",
                    Description = "Bright lights at 100%, warmer temperature, and disarm security",
                    Actions = new Dictionary<string, object>
                    {
                        ["lights"] = "on",
                        ["brightness"] = 100,
                        ["temperature"] = 23.0,
                        ["security"] = "disarm"
                    }
                },
                ["派对"] = new SceneConfiguration
                {
                    Name = "派对",
                    Description = "灯光全开100%，提高温度，关闭安防系统",
                    Actions = new Dictionary<string, object>
                    {
                        ["lights"] = "on",
                        ["brightness"] = 100,
                        ["temperature"] = 23.0,
                        ["security"] = "disarm"
                    }
                }
            };
        }
    }

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult($"Smart home coordinator managing {State.RegisteredDevices.Count} devices. " +
                          $"Available scenes: {string.Join(", ", State.Scenes.Keys)}");

    #region Public Methods

    public Task RegisterDeviceAsync(string deviceType, string gAgentId)
    {
        if (!State.RegisteredDevices.ContainsKey(deviceType))
        {
            RaiseEvent(new DeviceRegisteredLogEvent
            {
                DeviceType = deviceType,
                GAgentId = gAgentId,
                Timestamp = DateTime.UtcNow
            });
            return ConfirmEvents();
        }

        return Task.CompletedTask;
    }

    public async Task ExecuteSceneAsync(string sceneName)
    {
        if (!State.Scenes.TryGetValue(sceneName.ToLower(), out var scene))
        {
            Logger.LogWarning("Unknown scene: {SceneName}", sceneName);
            await PublishAsync(new SceneExecutedEvent
            {
                SceneName = sceneName,
                Success = false,
                ErrorMessage = $"Unknown scene: {sceneName}",
                ExecutedAt = DateTime.UtcNow
            });
            return;
        }

        try
        {
            Logger.LogInformation("Executing scene: {SceneName}", sceneName);
            
            // Execute scene actions
            foreach (var action in scene.Actions)
            {
                switch (action.Key)
                {
                    case "lights":
                        await ControlLightsAsync(action.Value.ToString() ?? "off");
                        break;
                    
                    case "brightness":
                        await SetLightBrightnessAsync(Convert.ToInt32(action.Value));
                        break;
                    
                    case "temperature":
                        await SetTemperatureAsync(Convert.ToDouble(action.Value));
                        break;
                    
                    case "security":
                        await ControlSecurityAsync(action.Value.ToString() ?? "disarm");
                        break;
                    
                    case "curtain":
                        await SetCurtainPositionAsync(Convert.ToInt32(action.Value));
                        break;
                }
            }

            // Record scene execution
            RaiseEvent(new SceneExecutedLogEvent
            {
                SceneName = sceneName,
                Timestamp = DateTime.UtcNow,
                Success = true
            });
            await ConfirmEvents();

            // Publish success event
            await PublishAsync(new SceneExecutedEvent
            {
                SceneName = sceneName,
                Success = true,
                ExecutedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing scene {SceneName}", sceneName);
            
            // Record failure
            RaiseEvent(new SceneExecutedLogEvent
            {
                SceneName = sceneName,
                Timestamp = DateTime.UtcNow,
                Success = false,
                ErrorMessage = ex.Message
            });
            await ConfirmEvents();

            // Publish failure event
            await PublishAsync(new SceneExecutedEvent
            {
                SceneName = sceneName,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutedAt = DateTime.UtcNow
            });
        }
    }

    public Task<List<string>> GetAvailableScenesAsync()
        => Task.FromResult(State.Scenes.Keys.ToList());

    public async Task ProcessDeviceControlIntentAsync(DeviceControlIntentEvent intent)
    {
        Logger.LogInformation("Processing device control intent: {DeviceType} - {Action}", 
            intent.DeviceType, intent.Action);

        switch (intent.DeviceType.ToLower())
        {
            case "light":
            case "lights":
                await ProcessLightIntent(intent);
                break;
                
            case "thermostat":
            case "temperature":
                await ProcessThermostatIntent(intent);
                break;
                
            case "security":
            case "alarm":
                await ProcessSecurityIntent(intent);
                break;
                
            default:
                Logger.LogWarning("Unknown device type: {DeviceType}", intent.DeviceType);
                break;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task ProcessLightIntent(DeviceControlIntentEvent intent)
    {
        // Get the first registered light if no specific location is provided
        var lightId = intent.Location;
        if (string.IsNullOrEmpty(lightId))
        {
            if (State.RegisteredDevices.TryGetValue("light", out var registeredLightId))
            {
                lightId = registeredLightId;
            }
            else
            {
                Logger.LogWarning("No light device registered");
                return;
            }
        }
        
        switch (intent.Action.ToLower())
        {
            case "turn_on":
            case "on":
                await PublishAsync(new TurnOnLightCommand { LightId = lightId });
                break;
                
            case "turn_off":
            case "off":
                await PublishAsync(new TurnOffLightCommand { LightId = lightId });
                break;
                
            case "set_brightness":
            case "dim":
                if (intent.Parameters.TryGetValue("brightness", out var brightness))
                {
                    await PublishAsync(new SetBrightnessCommand 
                    { 
                        LightId = lightId,
                        Brightness = Convert.ToInt32(brightness)
                    });
                }
                break;
        }
    }

    private async Task ProcessThermostatIntent(DeviceControlIntentEvent intent)
    {
        // Get the first registered thermostat
        string thermostatId;
        if (!State.RegisteredDevices.TryGetValue("thermostat", out thermostatId))
        {
            Logger.LogWarning("No thermostat device registered");
            return;
        }
        
        switch (intent.Action.ToLower())
        {
            case "set_temperature":
            case "set":
                if (intent.Parameters.TryGetValue("temperature", out var temp))
                {
                    await PublishAsync(new SetTemperatureCommand 
                    { 
                        ThermostatId = thermostatId,
                        TargetTemperature = Convert.ToDouble(temp)
                    });
                }
                break;
                
            case "set_mode":
            case "mode":
                if (intent.Parameters.TryGetValue("mode", out var mode))
                {
                    if (Enum.TryParse<ThermostatMode>(mode.ToString(), true, out var thermostatMode))
                    {
                        await PublishAsync(new ChangeModeCommand 
                        { 
                            ThermostatId = thermostatId,
                            Mode = thermostatMode
                        });
                    }
                }
                break;
        }
    }

    private async Task ProcessSecurityIntent(DeviceControlIntentEvent intent)
    {
        // Get the first registered security system
        string securityId;
        if (!State.RegisteredDevices.TryGetValue("security", out securityId))
        {
            Logger.LogWarning("No security device registered");
            return;
        }
        
        switch (intent.Action.ToLower())
        {
            case "arm":
            case "activate":
                await PublishAsync(new ArmSecurityCommand { SecuritySystemId = securityId });
                break;
                
            case "disarm":
            case "deactivate":
                await PublishAsync(new DisarmSecurityCommand { SecuritySystemId = securityId });
                break;
        }
    }

    private async Task ControlLightsAsync(string action)
    {
        // Get all registered light devices
        if (!State.RegisteredDevices.TryGetValue("light", out var lightId))
        {
            Logger.LogWarning("No light device registered for scene execution");
            return;
        }

        // Handle "dim" as turn on (brightness will be set separately)
        var actionLower = action.ToLower();
        EventBase lightCommand;
        
        if (actionLower == "on" || actionLower == "dim")
        {
            lightCommand = new TurnOnLightCommand { LightId = lightId };
        }
        else
        {
            lightCommand = new TurnOffLightCommand { LightId = lightId };
        }
            
        await PublishAsync(lightCommand);
    }

    private async Task SetTemperatureAsync(double temperature)
    {
        if (!State.RegisteredDevices.TryGetValue("thermostat", out var thermostatId))
        {
            Logger.LogWarning("No thermostat device registered for scene execution");
            return;
        }

        await PublishAsync(new SetTemperatureCommand
        {
            ThermostatId = thermostatId,
            TargetTemperature = temperature
        });
    }

    private async Task ControlSecurityAsync(string action)
    {
        if (!State.RegisteredDevices.TryGetValue("security", out var securityId))
        {
            Logger.LogWarning("No security device registered for scene execution");
            return;
        }

        var securityCommand = action.ToLower() == "arm"
            ? (EventBase)new ArmSecurityCommand { SecuritySystemId = securityId }
            : new DisarmSecurityCommand { SecuritySystemId = securityId };
            
        await PublishAsync(securityCommand);
    }

    #endregion

    #region Event Handlers

    [EventHandler]
    public async Task HandleSceneActivationIntent(SceneActivationIntentEvent intent)
    {
        Logger.LogInformation("Received scene activation intent: {SceneName}", intent.SceneName);
        await ExecuteSceneAsync(intent.SceneName);
    }

    [EventHandler]
    public async Task HandleDeviceControlIntent(DeviceControlIntentEvent intent)
    {
        await ProcessDeviceControlIntentAsync(intent);
    }

    [EventHandler]
    public async Task HandleExecuteSceneCommand(ExecuteSceneCommand command)
    {
        await ExecuteSceneAsync(command.SceneName);
    }

    #endregion

    #region State Transitions

    protected override void GAgentTransitionState(HomeCoordinatorState state, StateLogEventBase<HomeCoordinatorStateLogEvent> @event)
    {
        switch (@event)
        {
            case DeviceRegisteredLogEvent e:
                state.RegisteredDevices[e.DeviceType] = e.GAgentId;
                Logger.LogDebug("Registered device {DeviceType} with GAgent {GAgentId}", 
                    e.DeviceType, e.GAgentId);
                break;
                
            case SceneExecutedLogEvent e:
                state.LastExecutedScene = e.SceneName;
                state.LastSceneExecutedAt = e.Timestamp;
                state.SceneExecutionHistory.Add($"{e.SceneName} at {e.Timestamp:HH:mm:ss}");
                
                // Keep only last 10 executions
                if (state.SceneExecutionHistory.Count > 10)
                {
                    state.SceneExecutionHistory.RemoveAt(0);
                }
                
                Logger.LogDebug("Scene {SceneName} executed at {Timestamp} - Success: {Success}", 
                    e.SceneName, e.Timestamp, e.Success);
                break;
        }
    }

    private async Task SetLightBrightnessAsync(int brightness)
    {
        if (!State.RegisteredDevices.TryGetValue("light", out var lightId))
        {
            Logger.LogWarning("No light device registered for brightness adjustment");
            return;
        }

        await PublishAsync(new SetBrightnessCommand
        {
            LightId = lightId,
            Brightness = brightness
        });
    }
    
    private async Task SetCurtainPositionAsync(int position)
    {
        if (!State.RegisteredDevices.TryGetValue("curtain", out var curtainId))
        {
            Logger.LogWarning("No curtain device registered for position adjustment");
            return;
        }
        
        await PublishAsync(new SetCurtainPositionCommand
        {
            CurtainId = curtainId,
            Position = position
        });
    }

    #endregion
}

#endregion 