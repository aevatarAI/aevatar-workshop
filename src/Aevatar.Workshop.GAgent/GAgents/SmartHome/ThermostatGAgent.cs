using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Workshop.GAgent.GAgents.SmartHome;

#region State and Events

/// <summary>
/// 恒温器状态
/// </summary>
[GenerateSerializer]
public class ThermostatState : StateBase
{
    [Id(0)] public string ThermostatId { get; set; } = string.Empty;
    [Id(1)] public double CurrentTemperature { get; set; } = 22.0;
    [Id(2)] public double TargetTemperature { get; set; } = 22.0;
    [Id(3)] public ThermostatMode Mode { get; set; } = ThermostatMode.Auto;
    [Id(4)] public string Location { get; set; } = "Living Room";
    [Id(5)] public DateTime LastChangeAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 状态日志事件基类
/// </summary>
[GenerateSerializer]
public class ThermostatStateLogEvent : StateLogEventBase<ThermostatStateLogEvent>;

/// <summary>
/// 目标温度设置事件
/// </summary>
[GenerateSerializer]
public class TargetTemperatureSetLogEvent : ThermostatStateLogEvent
{
    [Id(0)] public double OldTemperature { get; set; }
    [Id(1)] public double NewTemperature { get; set; }
    [Id(2)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// 当前温度更新事件
/// </summary>
[GenerateSerializer]
public class CurrentTemperatureUpdatedLogEvent : ThermostatStateLogEvent
{
    [Id(0)] public double Temperature { get; set; }
    [Id(1)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// 模式改变事件
/// </summary>
[GenerateSerializer]
public class ModeChangedLogEvent : ThermostatStateLogEvent
{
    [Id(0)] public ThermostatMode OldMode { get; set; }
    [Id(1)] public ThermostatMode NewMode { get; set; }
    [Id(2)] public DateTime Timestamp { get; set; }
}

#endregion

#region Interface

/// <summary>
/// 恒温器控制接口
/// </summary>
public interface IThermostatGAgent : IStateGAgent<ThermostatState>
{
    Task SetTargetTemperatureAsync(double temperature);
    Task SetModeAsync(ThermostatMode mode);
    Task<double> GetCurrentTemperatureAsync();
    Task<double> GetTargetTemperatureAsync();
    Task<ThermostatMode> GetModeAsync();
    Task SimulateTemperatureChangeAsync(); // 模拟温度变化
}

#endregion

#region Implementation

/// <summary>
/// 恒温器控制智能体
/// </summary>
[GAgent("thermostat", "smarthome")]
public class ThermostatGAgent : GAgentBase<ThermostatState, ThermostatStateLogEvent>, IThermostatGAgent
{
    private IDisposable? _temperatureSimulationTimer;
    
    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // 初始化恒温器ID
        if (string.IsNullOrEmpty(State.ThermostatId))
        {
            State.ThermostatId = this.GetGrainId().Key.ToString() ?? "default-thermostat";
        }
        
        // 启动温度模拟定时器
        _temperatureSimulationTimer = this.RegisterGrainTimer(
            async (token) => await SimulateTemperatureChangeAsync(),
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(30),
                Period = TimeSpan.FromSeconds(30),
                Interleave = true
            }
        );
        
        return base.OnGAgentActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _temperatureSimulationTimer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult(
            $"【智能恒温器】控制{State.Location}的温度设备。\n" +
            $"当前状态：室温 {State.CurrentTemperature:F1}°C，目标温度 {State.TargetTemperature:F1}°C，模式 {State.Mode}\n\n" +
            $"可用命令：\n" +
            $"• SetTemperatureCommand - 设置目标温度\n" +
            $"  参数：ThermostatId (string) - 设备ID, Temperature (double) - 温度值(16-30°C)\n" +
            $"• ChangeModeCommand - 切换工作模式\n" +
            $"  参数：ThermostatId (string) - 设备ID, Mode (string) - 模式(Off/Heating/Cooling/Auto)\n\n" +
            $"使用示例：\n" +
            $"- 用户说'设置温度22度' → 使用SetTemperatureCommand，Temperature=22.0\n" +
            $"- 用户说'打开制冷' → 使用ChangeModeCommand，Mode='Cooling'\n" +
            $"- 用户说'关闭空调' → 使用ChangeModeCommand，Mode='Off'");

    #region Public Methods

    public Task SetTargetTemperatureAsync(double temperature)
    {
        if (temperature < 10 || temperature > 35)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 10°C and 35°C");
        }

        if (Math.Abs(State.TargetTemperature - temperature) > 0.01)
        {
            RaiseEvent(new TargetTemperatureSetLogEvent
            {
                OldTemperature = State.TargetTemperature,
                NewTemperature = temperature,
                Timestamp = DateTime.UtcNow
            });
            return ConfirmEvents();
        }

        return Task.CompletedTask;
    }

    public Task SetModeAsync(ThermostatMode mode)
    {
        if (State.Mode != mode)
        {
            RaiseEvent(new ModeChangedLogEvent
            {
                OldMode = State.Mode,
                NewMode = mode,
                Timestamp = DateTime.UtcNow
            });
            return ConfirmEvents();
        }

        return Task.CompletedTask;
    }

    public Task<double> GetCurrentTemperatureAsync() => Task.FromResult(State.CurrentTemperature);

    public Task<double> GetTargetTemperatureAsync() => Task.FromResult(State.TargetTemperature);

    public Task<ThermostatMode> GetModeAsync() => Task.FromResult(State.Mode);

    public async Task SimulateTemperatureChangeAsync()
    {
        // 模拟温度逐渐接近目标温度
        var diff = State.TargetTemperature - State.CurrentTemperature;
        if (Math.Abs(diff) > 0.1)
        {
            var change = diff * 0.1; // 每次改变10%的差值
            var newTemp = State.CurrentTemperature + change;
            
            RaiseEvent(new CurrentTemperatureUpdatedLogEvent
            {
                Temperature = Math.Round(newTemp, 1),
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            // 发布温度变化事件
            await PublishAsync(new TemperatureChangedEvent
            {
                ThermostatId = State.ThermostatId,
                CurrentTemperature = State.CurrentTemperature,
                TargetTemperature = State.TargetTemperature,
                ChangedAt = DateTime.UtcNow
            });
        }
    }

    #endregion

    #region Event Handlers

    [EventHandler]
    public async Task HandleSetTemperatureCommand(SetTemperatureCommand command)
    {
        if (command.ThermostatId != State.ThermostatId && !string.IsNullOrEmpty(command.ThermostatId))
        {
            Logger.LogDebug("Ignoring SetTemperatureCommand for different thermostat: {TargetId}", command.ThermostatId);
            return;
        }

        Logger.LogInformation("Setting thermostat {ThermostatId} target temperature to {Temperature}°C", 
            State.ThermostatId, command.TargetTemperature);
        await SetTargetTemperatureAsync(command.TargetTemperature);
        
        // 发布温度变化事件
        await PublishAsync(new TemperatureChangedEvent
        {
            ThermostatId = State.ThermostatId,
            CurrentTemperature = State.CurrentTemperature,
            TargetTemperature = State.TargetTemperature,
            ChangedAt = State.LastChangeAt
        });
    }

    [EventHandler]
    public async Task HandleChangeModeCommand(ChangeModeCommand command)
    {
        if (command.ThermostatId != State.ThermostatId && !string.IsNullOrEmpty(command.ThermostatId))
        {
            Logger.LogDebug("Ignoring ChangeModeCommand for different thermostat: {TargetId}", command.ThermostatId);
            return;
        }

        Logger.LogInformation("Changing thermostat {ThermostatId} mode to {Mode}", 
            State.ThermostatId, command.Mode);
        await SetModeAsync(command.Mode);
        
        // 发布模式变化事件
        await PublishAsync(new ModeChangedEvent
        {
            ThermostatId = State.ThermostatId,
            Mode = State.Mode,
            ChangedAt = State.LastChangeAt
        });
    }

    #endregion

    #region State Transitions

    protected override void GAgentTransitionState(ThermostatState state, StateLogEventBase<ThermostatStateLogEvent> @event)
    {
        switch (@event)
        {
            case TargetTemperatureSetLogEvent e:
                state.TargetTemperature = e.NewTemperature;
                state.LastChangeAt = e.Timestamp;
                Logger.LogDebug("Thermostat {ThermostatId} target temperature changed from {Old}°C to {New}°C", 
                    state.ThermostatId, e.OldTemperature, e.NewTemperature);
                break;
                
            case CurrentTemperatureUpdatedLogEvent e:
                state.CurrentTemperature = e.Temperature;
                Logger.LogDebug("Thermostat {ThermostatId} current temperature updated to {Temperature}°C", 
                    state.ThermostatId, e.Temperature);
                break;
                
            case ModeChangedLogEvent e:
                state.Mode = e.NewMode;
                state.LastChangeAt = e.Timestamp;
                Logger.LogDebug("Thermostat {ThermostatId} mode changed from {Old} to {New}", 
                    state.ThermostatId, e.OldMode, e.NewMode);
                break;
        }
    }

    #endregion
}

#endregion 