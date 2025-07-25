using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Workshop.GAgent.GAgents.SmartHome;

#region State and Events

/// <summary>
/// 安防系统状态
/// </summary>
[GenerateSerializer]
public class SecurityState : StateBase
{
    [Id(0)] public string SecuritySystemId { get; set; } = string.Empty;
    [Id(1)] public bool IsArmed { get; set; }
    [Id(2)] public DateTime LastMotionDetected { get; set; } = DateTime.MinValue;
    [Id(3)] public string LastMotionLocation { get; set; } = string.Empty;
    [Id(4)] public int MotionDetectionCount { get; set; }
    [Id(5)] public string Location { get; set; } = "Home";
    [Id(6)] public DateTime LastChangeAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 状态日志事件基类
/// </summary>
[GenerateSerializer]
public class SecurityStateLogEvent : StateLogEventBase<SecurityStateLogEvent>;

/// <summary>
/// 系统布防事件
/// </summary>
[GenerateSerializer]
public class SystemArmedLogEvent : SecurityStateLogEvent
{
    [Id(0)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// 系统撤防事件
/// </summary>
[GenerateSerializer]
public class SystemDisarmedLogEvent : SecurityStateLogEvent
{
    [Id(0)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// 检测到移动日志事件
/// </summary>
[GenerateSerializer]
public class MotionDetectedLogEvent : SecurityStateLogEvent
{
    [Id(0)] public string Location { get; set; } = string.Empty;
    [Id(1)] public DateTime Timestamp { get; set; }
}

#endregion

#region Interface

/// <summary>
/// 安防系统控制接口
/// </summary>
public interface ISecurityGAgent : IStateGAgent<SecurityState>
{
    Task ArmAsync();
    Task DisarmAsync();
    Task<bool> IsArmedAsync();
    Task<DateTime> GetLastMotionTimeAsync();
    Task<string> GetLastMotionLocationAsync();
    Task SimulateMotionAsync(string location); // 模拟移动检测
}

#endregion

#region Implementation

/// <summary>
/// 安防系统控制智能体
/// </summary>
[GAgent("security", "smarthome")]
public class SecurityGAgent : GAgentBase<SecurityState, SecurityStateLogEvent>, ISecurityGAgent
{
    private IDisposable? _motionSimulationTimer;
    private readonly Random _random = new Random();
    private readonly string[] _locations = { "Front Door", "Living Room", "Kitchen", "Bedroom", "Backyard" };
    
    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // 初始化安防系统ID
        if (string.IsNullOrEmpty(State.SecuritySystemId))
        {
            State.SecuritySystemId = this.GetGrainId().Key.ToString() ?? "default-security";
        }
        
        // 启动移动检测模拟定时器（仅在布防时检测）
        _motionSimulationTimer = this.RegisterGrainTimer(
            async (token) => await SimulateRandomMotionAsync(),
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(45),
                Period = TimeSpan.FromSeconds(45),
                Interleave = true
            }
        );
        
        return base.OnGAgentActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _motionSimulationTimer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public override Task<string> GetDescriptionAsync()
    {
        var status = State.IsArmed ? "已布防" : "已撤防";
        var lastMotion = State.LastMotionDetected == DateTime.MinValue 
            ? "未检测到移动" 
            : $"最后移动：{State.LastMotionDetected:HH:mm:ss} 在 {State.LastMotionLocation}";
        
        return Task.FromResult(
            $"【智能安防系统】监控{State.Location}的安全设备。\n" +
            $"当前状态：{status}，{lastMotion}\n\n" +
            $"可用命令：\n" +
            $"• ArmSecurityCommand - 启动布防\n" +
            $"  参数：SecuritySystemId (string) - 设备ID\n" +
            $"• DisarmSecurityCommand - 解除布防\n" +
            $"  参数：SecuritySystemId (string) - 设备ID\n\n" +
            $"使用示例：\n" +
            $"- 用户说'启动安防' → 使用ArmSecurityCommand\n" +
            $"- 用户说'关闭警报' → 使用DisarmSecurityCommand\n" +
            $"- 用户说'布防' → 使用ArmSecurityCommand");
    }

    #region Public Methods

    public Task ArmAsync()
    {
        if (!State.IsArmed)
        {
            RaiseEvent(new SystemArmedLogEvent { Timestamp = DateTime.UtcNow });
            return ConfirmEvents();
        }
        
        Logger.LogDebug("Security system {SecuritySystemId} is already armed", State.SecuritySystemId);
        return Task.CompletedTask;
    }

    public Task DisarmAsync()
    {
        if (State.IsArmed)
        {
            RaiseEvent(new SystemDisarmedLogEvent { Timestamp = DateTime.UtcNow });
            return ConfirmEvents();
        }
        
        Logger.LogDebug("Security system {SecuritySystemId} is already disarmed", State.SecuritySystemId);
        return Task.CompletedTask;
    }

    public Task<bool> IsArmedAsync() => Task.FromResult(State.IsArmed);

    public Task<DateTime> GetLastMotionTimeAsync() => Task.FromResult(State.LastMotionDetected);

    public Task<string> GetLastMotionLocationAsync() => Task.FromResult(State.LastMotionLocation);

    public async Task SimulateMotionAsync(string location)
    {
        if (State.IsArmed)
        {
            RaiseEvent(new MotionDetectedLogEvent
            {
                Location = location,
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            // 发布移动检测事件
            await PublishAsync(new MotionDetectedEvent
            {
                SecuritySystemId = State.SecuritySystemId,
                Location = location,
                DetectedAt = State.LastMotionDetected
            });
            
            Logger.LogWarning("Motion detected at {Location} while system is armed!", location);
        }
    }

    private async Task SimulateRandomMotionAsync()
    {
        // 仅在布防状态下，有20%的概率检测到移动
        if (State.IsArmed && _random.Next(100) < 20)
        {
            var location = _locations[_random.Next(_locations.Length)];
            await SimulateMotionAsync(location);
        }
    }

    #endregion

    #region Event Handlers

    [EventHandler]
    public async Task HandleArmCommand(ArmSecurityCommand command)
    {
        if (command.SecuritySystemId != State.SecuritySystemId && !string.IsNullOrEmpty(command.SecuritySystemId))
        {
            Logger.LogDebug("Ignoring ArmCommand for different security system: {TargetId}", command.SecuritySystemId);
            return;
        }

        Logger.LogInformation("Arming security system {SecuritySystemId}", State.SecuritySystemId);
        await ArmAsync();
        
        // 发布状态变化事件
        await PublishAsync(new SecurityStateChangedEvent
        {
            SecuritySystemId = State.SecuritySystemId,
            IsArmed = State.IsArmed,
            ChangedAt = State.LastChangeAt
        });
    }

    [EventHandler]
    public async Task HandleDisarmCommand(DisarmSecurityCommand command)
    {
        if (command.SecuritySystemId != State.SecuritySystemId && !string.IsNullOrEmpty(command.SecuritySystemId))
        {
            Logger.LogDebug("Ignoring DisarmCommand for different security system: {TargetId}", command.SecuritySystemId);
            return;
        }

        Logger.LogInformation("Disarming security system {SecuritySystemId}", State.SecuritySystemId);
        await DisarmAsync();
        
        // 发布状态变化事件
        await PublishAsync(new SecurityStateChangedEvent
        {
            SecuritySystemId = State.SecuritySystemId,
            IsArmed = State.IsArmed,
            ChangedAt = State.LastChangeAt
        });
    }

    #endregion

    #region State Transitions

    protected override void GAgentTransitionState(SecurityState state, StateLogEventBase<SecurityStateLogEvent> @event)
    {
        switch (@event)
        {
            case SystemArmedLogEvent e:
                state.IsArmed = true;
                state.LastChangeAt = e.Timestamp;
                Logger.LogDebug("Security system {SecuritySystemId} armed at {Timestamp}", 
                    state.SecuritySystemId, e.Timestamp);
                break;
                
            case SystemDisarmedLogEvent e:
                state.IsArmed = false;
                state.LastChangeAt = e.Timestamp;
                Logger.LogDebug("Security system {SecuritySystemId} disarmed at {Timestamp}", 
                    state.SecuritySystemId, e.Timestamp);
                break;
                
            case MotionDetectedLogEvent e:
                state.LastMotionDetected = e.Timestamp;
                state.LastMotionLocation = e.Location;
                state.MotionDetectionCount++;
                Logger.LogDebug("Motion detected at {Location} on {Timestamp}", e.Location, e.Timestamp);
                break;
        }
    }

    #endregion
}

#endregion 