using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace Aevatar.Workshop.GAgent.GAgents.SmartHome;

#region State and Events

/// <summary>
/// 灯光状态
/// </summary>
[GenerateSerializer]
public class LightState : StateBase
{
    [Id(0)] public string LightId { get; set; } = string.Empty;
    [Id(1)] public bool IsOn { get; set; }
    [Id(2)] public int Brightness { get; set; } = 100; // 0-100
    [Id(3)] public string Location { get; set; } = "Default Room";
    [Id(4)] public DateTime LastChangeAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 状态日志事件基类
/// </summary>
[GenerateSerializer]
public class LightStateLogEvent : StateLogEventBase<LightStateLogEvent>;

/// <summary>
/// 灯光开启事件
/// </summary>
[GenerateSerializer]
public class LightTurnedOnLogEvent : LightStateLogEvent
{
    [Id(0)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// 灯光关闭事件
/// </summary>
[GenerateSerializer]
public class LightTurnedOffLogEvent : LightStateLogEvent
{
    [Id(0)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// 亮度调整事件
/// </summary>
[GenerateSerializer]
public class BrightnessChangedLogEvent : LightStateLogEvent
{
    [Id(0)] public int OldBrightness { get; set; }
    [Id(1)] public int NewBrightness { get; set; }
    [Id(2)] public DateTime Timestamp { get; set; }
}

#endregion

#region Interface

/// <summary>
/// 灯光控制接口
/// </summary>
public interface ILightGAgent : IStateGAgent<LightState>
{
    Task TurnOnAsync();
    Task TurnOffAsync();
    Task SetBrightnessAsync(int brightness);
    Task<bool> IsOnAsync();
    Task<int> GetBrightnessAsync();
}

#endregion

#region Implementation

/// <summary>
/// 灯光控制智能体
/// </summary>
[GenerateSerializer]
public class LightConfiguration : ConfigurationBase
{
    [Id(0)] public string LightId { get; set; } = string.Empty;
    [Id(1)] public string Location { get; set; } = "Default Room";
}

[GenerateSerializer]
public class SetLightInitialConfigurationLogEvent : LightStateLogEvent
{
    [Id(0)] public string LightId { get; set; } = string.Empty;
    [Id(1)] public string Location { get; set; } = string.Empty;
}

[GAgent("light", "smarthome")]
public class LightGAgent : GAgentBase<LightState, LightStateLogEvent, EventBase, LightConfiguration>, ILightGAgent
{
    protected IGAgentFactory GAgentFactory => ServiceProvider.GetRequiredService<IGAgentFactory>();
    
    protected override async Task PerformConfigAsync(LightConfiguration configuration)
    {
        // 使用事件来初始化状态
        RaiseEvent(new SetLightInitialConfigurationLogEvent
        {
            LightId = configuration.LightId,
            Location = configuration.Location
        });
        
        await ConfirmEvents();
    }

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult(
            $"【智能灯光控制器】控制{State.Location}的灯光设备。\n" +
            $"当前状态：{(State.IsOn ? "已开启" : "已关闭")}，亮度：{State.Brightness}%\n\n" +
            $"可用命令：\n" +
            $"• TurnOnLightCommand - 开灯\n" +
            $"  参数：LightId (string) - 设备ID\n" +
            $"• TurnOffLightCommand - 关灯\n" + 
            $"  参数：LightId (string) - 设备ID\n" +
            $"• SetBrightnessCommand - 调节亮度\n" +
            $"  参数：LightId (string) - 设备ID, Brightness (int) - 亮度值(0-100)\n\n" +
            $"使用示例：\n" +
            $"- 用户说'打开灯' → 使用TurnOnLightCommand\n" +
            $"- 用户说'把灯调到20%' → 使用SetBrightnessCommand，Brightness=20\n" +
            $"- 用户说'把灯调暗' → 使用SetBrightnessCommand，降低当前亮度值");

    #region Public Methods

    public async Task TurnOnAsync()
    {
        if (!State.IsOn)
        {
            RaiseEvent(new LightTurnedOnLogEvent { Timestamp = DateTime.UtcNow });
            await ConfirmEvents();
        }
        
        Logger.LogInformation("Light {LightId} is already on", State.LightId);
    }

    public Task TurnOffAsync()
    {
        if (State.IsOn)
        {
            RaiseEvent(new LightTurnedOffLogEvent { Timestamp = DateTime.UtcNow });
            return ConfirmEvents();
        }
        
        Logger.LogDebug("Light {LightId} is already off", State.LightId);
        return Task.CompletedTask;
    }

    public Task SetBrightnessAsync(int brightness)
    {
        if (brightness < 0 || brightness > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(brightness), "Brightness must be between 0 and 100");
        }

        if (State.Brightness != brightness)
        {
            RaiseEvent(new BrightnessChangedLogEvent
            {
                OldBrightness = State.Brightness,
                NewBrightness = brightness,
                Timestamp = DateTime.UtcNow
            });
            return ConfirmEvents();
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsOnAsync() => Task.FromResult(State.IsOn);

    public Task<int> GetBrightnessAsync() => Task.FromResult(State.Brightness);

    #endregion

    #region Event Handlers

    [EventHandler]
    public async Task HandleTurnOnCommand(TurnOnLightCommand command)
    {
        if (command.LightId != State.LightId && !string.IsNullOrEmpty(command.LightId))
        {
            Logger.LogInformation("Ignoring TurnOnCommand for different light: {TargetId}, current light id: {LightId}",
                command.LightId, State.LightId);
            var lightGAgent =
                await GAgentFactory.GetGAgentAsync<ILightGAgent>("light agent".ToGuid());
            Logger.LogInformation($"GrainId： {lightGAgent.GetGrainId().ToString()}");
            await lightGAgent.TurnOnAsync();
            // 发布状态变化事件
            await PublishAsync(new LightStateChangedEvent
            {
                LightId = "light agent".ToGuid().ToString("N"),
                IsOn = true,
                Brightness = State.Brightness,
                ChangedAt = State.LastChangeAt
            });
            return;
        }

        Logger.LogInformation("Turning on light {LightId}", State.LightId);
        await TurnOnAsync();
        
        // 发布状态变化事件
        await PublishAsync(new LightStateChangedEvent
        {
            LightId = State.LightId,
            IsOn = State.IsOn,
            Brightness = State.Brightness,
            ChangedAt = State.LastChangeAt
        });
    }

    [EventHandler]
    public async Task HandleTurnOffCommand(TurnOffLightCommand command)
    {
        if (command.LightId != State.LightId && !string.IsNullOrEmpty(command.LightId))
        {
            Logger.LogInformation("Ignoring TurnOffCommand for different light: {TargetId}", command.LightId);
            var lightGAgent =
                await GAgentFactory.GetGAgentAsync<ILightGAgent>("light agent".ToGuid());
            Logger.LogInformation($"GrainId： {lightGAgent.GetGrainId().ToString()}");
            await lightGAgent.TurnOffAsync();
            // 发布状态变化事件
            await PublishAsync(new LightStateChangedEvent
            {
                LightId = "light agent".ToGuid().ToString("N"),
                IsOn = false,
                Brightness = State.Brightness,
                ChangedAt = State.LastChangeAt
            });
            return;
        }

        Logger.LogInformation("Turning off light {LightId}", State.LightId);
        await TurnOffAsync();
        
        // 发布状态变化事件
        await PublishAsync(new LightStateChangedEvent
        {
            LightId = State.LightId,
            IsOn = State.IsOn,
            Brightness = State.Brightness,
            ChangedAt = State.LastChangeAt
        });
    }

    [EventHandler]
    public async Task HandleSetBrightnessCommand(SetBrightnessCommand command)
    {
        if (command.LightId != State.LightId && !string.IsNullOrEmpty(command.LightId))
        {
            Logger.LogInformation(
                "Ignoring SetBrightnessCommand for different light: {TargetId}, Current light id: {LightId}",
                command.LightId, State.LightId);
            return;
        }

        Logger.LogInformation("Setting light {LightId} brightness to {Brightness}%", 
            State.LightId, command.Brightness);
        await SetBrightnessAsync(command.Brightness);
        
        // 发布状态变化事件
        await PublishAsync(new LightStateChangedEvent
        {
            LightId = State.LightId,
            IsOn = State.IsOn,
            Brightness = State.Brightness,
            ChangedAt = State.LastChangeAt
        });
    }

    #endregion

    #region State Transitions

    protected override void GAgentTransitionState(LightState state, StateLogEventBase<LightStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetLightInitialConfigurationLogEvent configEvent:
                state.LightId = configEvent.LightId;
                state.Location = configEvent.Location;
                Logger.LogDebug("Light initialized with ID {LightId} at {Location}", state.LightId, state.Location);
                break;
                
            case LightTurnedOnLogEvent e:
                state.IsOn = true;
                state.LastChangeAt = e.Timestamp;
                Logger.LogDebug("Light {LightId} turned on at {Timestamp}", state.LightId, e.Timestamp);
                break;
                
            case LightTurnedOffLogEvent e:
                state.IsOn = false;
                state.LastChangeAt = e.Timestamp;
                Logger.LogDebug("Light {LightId} turned off at {Timestamp}", state.LightId, e.Timestamp);
                break;
                
            case BrightnessChangedLogEvent e:
                state.Brightness = e.NewBrightness;
                state.LastChangeAt = e.Timestamp;
                Logger.LogDebug("Light {LightId} brightness changed from {Old}% to {New}%", 
                    state.LightId, e.OldBrightness, e.NewBrightness);
                break;
        }
    }

    #endregion
}

#endregion 