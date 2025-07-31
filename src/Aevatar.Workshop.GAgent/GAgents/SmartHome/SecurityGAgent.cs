using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Workshop.GAgent.GAgents.SmartHome;

#region State and Events

/// <summary>
/// Security system state
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
/// State log event base class
/// </summary>
[GenerateSerializer]
public class SecurityStateLogEvent : StateLogEventBase<SecurityStateLogEvent>;

/// <summary>
/// System armed event
/// </summary>
[GenerateSerializer]
public class SystemArmedLogEvent : SecurityStateLogEvent
{
    [Id(0)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// System disarmed event
/// </summary>
[GenerateSerializer]
public class SystemDisarmedLogEvent : SecurityStateLogEvent
{
    [Id(0)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// Motion detected log event
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
/// Security system control interface
/// </summary>
public interface ISecurityGAgent : IStateGAgent<SecurityState>
{
    Task ArmAsync();
    Task DisarmAsync();
    Task<bool> IsArmedAsync();
    Task<DateTime> GetLastMotionTimeAsync();
    Task<string> GetLastMotionLocationAsync();
    Task SimulateMotionAsync(string location); // Simulate motion detection
}

#endregion

#region Implementation

/// <summary>
/// Security system control GAgent
/// </summary>
[GAgent("security", "smarthome")]
public class SecurityGAgent : GAgentBase<SecurityState, SecurityStateLogEvent>, ISecurityGAgent
{
    private IDisposable? _motionSimulationTimer;
    private readonly Random _random = new Random();
    private readonly string[] _locations = { "Front Door", "Living Room", "Kitchen", "Bedroom", "Backyard" };

    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Initialize security system ID
        if (string.IsNullOrEmpty(State.SecuritySystemId))
        {
            State.SecuritySystemId = this.GetGrainId().Key.ToString() ?? "default-security";
        }

        // Start motion detection simulation timer (only detect when armed)
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
        var status = State.IsArmed ? "Armed" : "Disarmed";
        var lastMotion = State.LastMotionDetected == DateTime.MinValue
            ? "No motion detected"
            : $"Last motion: {State.LastMotionDetected:HH:mm:ss} at {State.LastMotionLocation}";

        return Task.FromResult(
            $"[Smart Security System] Monitors security devices at {State.Location}.\n" +
            $"Current status: {status}, {lastMotion}\n\n" +
            $"Available commands:\n" +
            $"• ArmSecurityCommand - Activate arming\n" +
            $"  Parameters: SecuritySystemId (string) - Device ID\n" +
            $"• DisarmSecurityCommand - Deactivate arming\n" +
            $"  Parameters: SecuritySystemId (string) - Device ID\n\n" +
            $"Usage examples:\n" +
            $"- User says 'activate security' → Use ArmSecurityCommand\n" +
            $"- User says 'turn off alarm' → Use DisarmSecurityCommand\n" +
            $"- User says 'arm system' → Use ArmSecurityCommand");
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

            // Publish motion detection event
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
        // Only in armed state, 20% chance to detect motion
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

        // Publish state change event
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
            Logger.LogDebug("Ignoring DisarmCommand for different security system: {TargetId}",
                command.SecuritySystemId);
            return;
        }

        Logger.LogInformation("Disarming security system {SecuritySystemId}", State.SecuritySystemId);
        await DisarmAsync();

        // Publish state change event
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