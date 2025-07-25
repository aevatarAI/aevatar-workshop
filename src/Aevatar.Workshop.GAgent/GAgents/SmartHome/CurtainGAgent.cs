using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent.GAgents.SmartHome;

#region Interface
/// <summary>
/// 智能窗帘控制接口
/// </summary>
public interface ICurtainGAgent : IStateGAgent<CurtainState>
{
    /// <summary>
    /// 设置窗帘开合度
    /// </summary>
    /// <param name="position">开合度 (0-100)</param>
    Task SetPositionAsync(int position);
    
    /// <summary>
    /// 完全打开窗帘
    /// </summary>
    Task OpenAsync();
    
    /// <summary>
    /// 完全关闭窗帘
    /// </summary>
    Task CloseAsync();
    
    /// <summary>
    /// 停止窗帘移动
    /// </summary>
    Task StopAsync();
}
#endregion

#region State
/// <summary>
/// 窗帘状态
/// </summary>
[GenerateSerializer]
public class CurtainState : StateBase
{
    /// <summary>
    /// 窗帘ID
    /// </summary>
    [Id(0)] public string CurtainId { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前开合度 (0-100, 0=完全关闭, 100=完全打开)
    /// </summary>
    [Id(1)] public int CurrentPosition { get; set; } = 100;
    
    /// <summary>
    /// 目标开合度
    /// </summary>
    [Id(2)] public int TargetPosition { get; set; } = 100;
    
    /// <summary>
    /// 是否正在移动
    /// </summary>
    [Id(3)] public bool IsMoving { get; set; } = false;
    
    /// <summary>
    /// 最后操作时间
    /// </summary>
    [Id(4)] public DateTime LastOperationTime { get; set; } = DateTime.UtcNow;
}
#endregion

#region State Log Events
/// <summary>
/// 窗帘状态日志事件基类
/// </summary>
[GenerateSerializer]
public abstract class CurtainStateLogEvent : StateLogEventBase<CurtainStateLogEvent> { }

/// <summary>
/// 窗帘位置变更事件
/// </summary>
[GenerateSerializer]
public class CurtainPositionChangedLogEvent : CurtainStateLogEvent
{
    [Id(0)] public int OldPosition { get; set; }
    [Id(1)] public int NewPosition { get; set; }
    [Id(2)] public DateTime OperationTime { get; set; }
}

/// <summary>
/// 窗帘移动状态变更事件
/// </summary>
[GenerateSerializer]
public class CurtainMovementChangedLogEvent : CurtainStateLogEvent
{
    [Id(0)] public bool IsMoving { get; set; }
    [Id(1)] public int TargetPosition { get; set; }
}

/// <summary>
/// 窗帘停止事件
/// </summary>
[GenerateSerializer]
public class CurtainStoppedLogEvent : CurtainStateLogEvent
{
    [Id(0)] public int StoppedAtPosition { get; set; }
}
#endregion

#region Events
/// <summary>
/// 设置窗帘位置命令
/// </summary>
[GenerateSerializer]
public class SetCurtainPositionCommand : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("窗帘设备的唯一标识符")]
    public string CurtainId { get; set; } = string.Empty;
    
    [Id(1)] 
    [System.ComponentModel.Description("目标开合度，范围 0-100，0 表示完全关闭，100 表示完全打开")]
    public int Position { get; set; }
}

/// <summary>
/// 打开窗帘命令
/// </summary>
[GenerateSerializer]
public class OpenCurtainCommand : EventBase
{
    [Id(0)] public string CurtainId { get; set; } = string.Empty;
}

/// <summary>
/// 关闭窗帘命令
/// </summary>
[GenerateSerializer]
public class CloseCurtainCommand : EventBase
{
    [Id(0)] public string CurtainId { get; set; } = string.Empty;
}

/// <summary>
/// 停止窗帘命令
/// </summary>
[GenerateSerializer]
public class StopCurtainCommand : EventBase
{
    [Id(0)] public string CurtainId { get; set; } = string.Empty;
}

/// <summary>
/// 窗帘状态变更事件
/// </summary>
[GenerateSerializer]
public class CurtainStateChangedEvent : EventBase
{
    [Id(0)] public string CurtainId { get; set; } = string.Empty;
    [Id(1)] public int CurrentPosition { get; set; }
    [Id(2)] public bool IsMoving { get; set; }
}
#endregion

#region Implementation
/// <summary>
/// 智能窗帘 GAgent 实现
/// </summary>
[GAgent("curtain", "smarthome")]
public class CurtainGAgent : GAgentBase<CurtainState, CurtainStateLogEvent>, ICurtainGAgent
{
    private IGrainTimer? _movementTimer;
    private const int MovementSpeed = 10; // 每秒移动的百分比
    
    public override Task<string> GetDescriptionAsync()
    {
        var status = State.IsMoving ? "移动中" : "静止";
        return Task.FromResult(
            $"【智能窗帘控制器】控制主卧的窗帘设备。\n" +
            $"当前状态：开合度 {State.CurrentPosition}%，{status}\n\n" +
            $"可用命令：\n" +
            $"• OpenCurtainCommand - 完全打开窗帘\n" +
            $"  参数：CurtainId (string) - 设备ID\n" +
            $"• CloseCurtainCommand - 完全关闭窗帘\n" +
            $"  参数：CurtainId (string) - 设备ID\n" +
            $"• SetCurtainPositionCommand - 设置窗帘开合度\n" +
            $"  参数：CurtainId (string) - 设备ID, Position (int) - 开合度(0-100)\n" +
            $"• StopCurtainCommand - 停止窗帘移动\n" +
            $"  参数：CurtainId (string) - 设备ID\n\n" +
            $"使用示例：\n" +
            $"- 用户说'打开窗帘' → 使用OpenCurtainCommand\n" +
            $"- 用户说'把窗帘调到50%' → 使用SetCurtainPositionCommand，Position=50\n" +
            $"- 用户说'关闭窗帘' → 使用CloseCurtainCommand");
    }
    
    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(State.CurtainId))
        {
            State.CurtainId = this.GetGrainId().Key.ToString() ?? "default-curtain";
        }
        
        return base.OnGAgentActivateAsync(cancellationToken);
    }
    
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _movementTimer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
    
    public async Task SetPositionAsync(int position)
    {
        position = Math.Clamp(position, 0, 100);
        
        if (State.CurrentPosition == position)
        {
            Logger.LogInformation("窗帘已在目标位置 {Position}%", position);
            return;
        }
        
        // 记录目标位置和开始移动
        RaiseEvent(new CurtainMovementChangedLogEvent
        {
            IsMoving = true,
            TargetPosition = position
        });
        
        await ConfirmEvents();
        
        // 启动移动模拟
        StartMovementSimulation();
        
        // 发布状态变更事件
        await PublishAsync(new CurtainStateChangedEvent
        {
            CurtainId = State.CurtainId,
            CurrentPosition = State.CurrentPosition,
            IsMoving = true
        });
        
        Logger.LogInformation("窗帘开始移动到 {Position}%", position);
    }
    
    public Task OpenAsync()
    {
        return SetPositionAsync(100);
    }
    
    public Task CloseAsync()
    {
        return SetPositionAsync(0);
    }
    
    public async Task StopAsync()
    {
        if (!State.IsMoving)
        {
            Logger.LogInformation("窗帘当前未在移动");
            return;
        }
        
        _movementTimer?.Dispose();
        
        RaiseEvent(new CurtainStoppedLogEvent
        {
            StoppedAtPosition = State.CurrentPosition
        });
        
        await ConfirmEvents();
        
        await PublishAsync(new CurtainStateChangedEvent
        {
            CurtainId = State.CurtainId,
            CurrentPosition = State.CurrentPosition,
            IsMoving = false
        });
        
        Logger.LogInformation("窗帘已停止在 {Position}%", State.CurrentPosition);
    }
    
    protected override void GAgentTransitionState(CurtainState state, StateLogEventBase<CurtainStateLogEvent> @event)
    {
        switch (@event)
        {
            case CurtainPositionChangedLogEvent e:
                state.CurrentPosition = e.NewPosition;
                state.LastOperationTime = e.OperationTime;
                break;
                
            case CurtainMovementChangedLogEvent e:
                state.IsMoving = e.IsMoving;
                state.TargetPosition = e.TargetPosition;
                break;
                
            case CurtainStoppedLogEvent e:
                state.IsMoving = false;
                state.CurrentPosition = e.StoppedAtPosition;
                state.TargetPosition = e.StoppedAtPosition;
                break;
        }
    }
    
    // 事件处理器
    [EventHandler]
    public Task HandleSetPositionCommand(SetCurtainPositionCommand command)
    {
        if (command.CurtainId != State.CurtainId)
        {
            return Task.CompletedTask;
        }
        
        return SetPositionAsync(command.Position);
    }
    
    [EventHandler]
    public Task HandleOpenCommand(OpenCurtainCommand command)
    {
        if (command.CurtainId != State.CurtainId)
        {
            return Task.CompletedTask;
        }
        
        return OpenAsync();
    }
    
    [EventHandler]
    public Task HandleCloseCommand(CloseCurtainCommand command)
    {
        if (command.CurtainId != State.CurtainId)
        {
            return Task.CompletedTask;
        }
        
        return CloseAsync();
    }
    
    [EventHandler]
    public Task HandleStopCommand(StopCurtainCommand command)
    {
        if (command.CurtainId != State.CurtainId)
        {
            return Task.CompletedTask;
        }
        
        return StopAsync();
    }
    
    private void StartMovementSimulation()
    {
        _movementTimer?.Dispose();
        
        _movementTimer = this.RegisterGrainTimer(
            async (cancellationToken) => await SimulateMovement(),
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(1),
                Period = TimeSpan.FromSeconds(1),
                Interleave = true
            }
        );
    }
    
    private async Task SimulateMovement()
    {
        if (!State.IsMoving)
        {
            _movementTimer?.Dispose();
            return;
        }
        
        var oldPosition = State.CurrentPosition;
        var newPosition = State.CurrentPosition;
        
        if (State.CurrentPosition < State.TargetPosition)
        {
            // 向上移动
            newPosition = Math.Min(State.CurrentPosition + MovementSpeed, State.TargetPosition);
        }
        else if (State.CurrentPosition > State.TargetPosition)
        {
            // 向下移动
            newPosition = Math.Max(State.CurrentPosition - MovementSpeed, State.TargetPosition);
        }
        
        RaiseEvent(new CurtainPositionChangedLogEvent
        {
            OldPosition = oldPosition,
            NewPosition = newPosition,
            OperationTime = DateTime.UtcNow
        });
        
        // 检查是否到达目标位置
        if (newPosition == State.TargetPosition)
        {
            RaiseEvent(new CurtainMovementChangedLogEvent
            {
                IsMoving = false,
                TargetPosition = State.TargetPosition
            });
            
            _movementTimer?.Dispose();
        }
        
        await ConfirmEvents();
        
        // 发布状态更新事件
        await PublishAsync(new CurtainStateChangedEvent
        {
            CurtainId = State.CurtainId,
            CurrentPosition = State.CurrentPosition,
            IsMoving = State.IsMoving
        });
    }
}
#endregion 