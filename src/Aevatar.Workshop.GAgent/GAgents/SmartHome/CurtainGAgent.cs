using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent.GAgents.SmartHome;

#region Interface
/// <summary>
/// Smart curtain control interface
/// </summary>
public interface ICurtainGAgent : IStateGAgent<CurtainState>
{
    /// <summary>
    /// Set curtain opening position
    /// </summary>
    /// <param name="position">Opening position (0-100)</param>
    Task SetPositionAsync(int position);
    
    /// <summary>
    /// Fully open curtain
    /// </summary>
    Task OpenAsync();
    
    /// <summary>
    /// Fully close curtain
    /// </summary>
    Task CloseAsync();
    
    /// <summary>
    /// Stop curtain movement
    /// </summary>
    Task StopAsync();
}
#endregion

#region State
/// <summary>
/// Curtain state
/// </summary>
[GenerateSerializer]
public class CurtainState : StateBase
{
    /// <summary>
    /// Curtain ID
    /// </summary>
    [Id(0)] public string CurtainId { get; set; } = string.Empty;
    
    /// <summary>
    /// Current opening position (0-100, 0=fully closed, 100=fully open)
    /// </summary>
    [Id(1)] public int CurrentPosition { get; set; } = 100;
    
    /// <summary>
    /// Target opening position
    /// </summary>
    [Id(2)] public int TargetPosition { get; set; } = 100;
    
    /// <summary>
    /// Is currently moving
    /// </summary>
    [Id(3)] public bool IsMoving { get; set; } = false;
    
    /// <summary>
    /// Last operation time
    /// </summary>
    [Id(4)] public DateTime LastOperationTime { get; set; } = DateTime.UtcNow;
}
#endregion

#region State Log Events
/// <summary>
/// Curtain state log event base class
/// </summary>
[GenerateSerializer]
public abstract class CurtainStateLogEvent : StateLogEventBase<CurtainStateLogEvent> { }

/// <summary>
/// Curtain position change event
/// </summary>
[GenerateSerializer]
public class CurtainPositionChangedLogEvent : CurtainStateLogEvent
{
    [Id(0)] public int OldPosition { get; set; }
    [Id(1)] public int NewPosition { get; set; }
    [Id(2)] public DateTime OperationTime { get; set; }
}

/// <summary>
/// Curtain movement status change event
/// </summary>
[GenerateSerializer]
public class CurtainMovementChangedLogEvent : CurtainStateLogEvent
{
    [Id(0)] public bool IsMoving { get; set; }
    [Id(1)] public int TargetPosition { get; set; }
}

/// <summary>
/// Curtain stop event
/// </summary>
[GenerateSerializer]
public class CurtainStoppedLogEvent : CurtainStateLogEvent
{
    [Id(0)] public int StoppedAtPosition { get; set; }
}
#endregion

#region Events
/// <summary>
/// Set curtain position command
/// </summary>
[GenerateSerializer]
public class SetCurtainPositionCommand : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("Unique identifier for curtain device")]
    public string CurtainId { get; set; } = string.Empty;
    
    [Id(1)] 
    [System.ComponentModel.Description("Target opening position, range 0-100, 0 means fully closed, 100 means fully open")]
    public int Position { get; set; }
}

/// <summary>
/// Open curtain command
/// </summary>
[GenerateSerializer]
public class OpenCurtainCommand : EventBase
{
    [Id(0)] public string CurtainId { get; set; } = string.Empty;
}

/// <summary>
/// Close curtain command
/// </summary>
[GenerateSerializer]
public class CloseCurtainCommand : EventBase
{
    [Id(0)] public string CurtainId { get; set; } = string.Empty;
}

/// <summary>
/// Stop curtain command
/// </summary>
[GenerateSerializer]
public class StopCurtainCommand : EventBase
{
    [Id(0)] public string CurtainId { get; set; } = string.Empty;
}

/// <summary>
/// Curtain state change event
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
/// Smart curtain GAgent implementation
/// </summary>
[GAgent("curtain", "smarthome")]
public class CurtainGAgent : GAgentBase<CurtainState, CurtainStateLogEvent>, ICurtainGAgent
{
    private IGrainTimer? _movementTimer;
    private const int MovementSpeed = 10; // Percentage movement per second
    
    public override Task<string> GetDescriptionAsync()
    {
        var status = State.IsMoving ? "Moving" : "Stationary";
        return Task.FromResult(
            $"[Smart Curtain Controller] Controls curtain devices in the master bedroom.\n" +
            $"Current status: Opening position {State.CurrentPosition}%, {status}\n\n" +
            $"Available commands:\n" +
            $"• OpenCurtainCommand - Fully open curtain\n" +
            $"  Parameters: CurtainId (string) - Device ID\n" +
            $"• CloseCurtainCommand - Fully close curtain\n" +
            $"  Parameters: CurtainId (string) - Device ID\n" +
            $"• SetCurtainPositionCommand - Set curtain opening position\n" +
            $"  Parameters: CurtainId (string) - Device ID, Position (int) - Opening position (0-100)\n" +
            $"• StopCurtainCommand - Stop curtain movement\n" +
            $"  Parameters: CurtainId (string) - Device ID\n\n" +
            $"Usage examples:\n" +
            $"- User says 'open curtain' → Use OpenCurtainCommand\n" +
            $"- User says 'set curtain to 50%' → Use SetCurtainPositionCommand, Position=50\n" +
            $"- User says 'close curtain' → Use CloseCurtainCommand");
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
            Logger.LogInformation("Curtain already at target position {Position}%", position);
            return;
        }
        
        // Record target position and start movement
        RaiseEvent(new CurtainMovementChangedLogEvent
        {
            IsMoving = true,
            TargetPosition = position
        });
        
        await ConfirmEvents();
        
        // Start movement simulation
        StartMovementSimulation();
        
        // Publish state change event
        await PublishAsync(new CurtainStateChangedEvent
        {
            CurtainId = State.CurtainId,
            CurrentPosition = State.CurrentPosition,
            IsMoving = true
        });
        
        Logger.LogInformation("Curtain started moving to {Position}%", position);
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
            Logger.LogInformation("Curtain is not currently moving");
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
        
        Logger.LogInformation("Curtain stopped at {Position}%", State.CurrentPosition);
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
    
    // Event handlers
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
            // Move upward
            newPosition = Math.Min(State.CurrentPosition + MovementSpeed, State.TargetPosition);
        }
        else if (State.CurrentPosition > State.TargetPosition)
        {
            // Move downward
            newPosition = Math.Max(State.CurrentPosition - MovementSpeed, State.TargetPosition);
        }
        
        RaiseEvent(new CurtainPositionChangedLogEvent
        {
            OldPosition = oldPosition,
            NewPosition = newPosition,
            OperationTime = DateTime.UtcNow
        });
        
        // Check if target position reached
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
        
        // Publish state update event
        await PublishAsync(new CurtainStateChangedEvent
        {
            CurtainId = State.CurtainId,
            CurrentPosition = State.CurrentPosition,
            IsMoving = State.IsMoving
        });
    }
}
#endregion 