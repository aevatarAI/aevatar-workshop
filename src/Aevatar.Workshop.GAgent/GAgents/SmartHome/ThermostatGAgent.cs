using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Workshop.GAgent.GAgents.SmartHome;

#region State and Events

/// <summary>
/// Thermostat state
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
/// State log event base class
/// </summary>
[GenerateSerializer]
public class ThermostatStateLogEvent : StateLogEventBase<ThermostatStateLogEvent>;

/// <summary>
/// Target temperature set event
/// </summary>
[GenerateSerializer]
public class TargetTemperatureSetLogEvent : ThermostatStateLogEvent
{
    [Id(0)] public double OldTemperature { get; set; }
    [Id(1)] public double NewTemperature { get; set; }
    [Id(2)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// Current temperature update event
/// </summary>
[GenerateSerializer]
public class CurrentTemperatureUpdatedLogEvent : ThermostatStateLogEvent
{
    [Id(0)] public double Temperature { get; set; }
    [Id(1)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// Mode change event
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
/// Thermostat control interface
/// </summary>
public interface IThermostatGAgent : IStateGAgent<ThermostatState>
{
    Task SetTargetTemperatureAsync(double temperature);
    Task SetModeAsync(ThermostatMode mode);
    Task<double> GetCurrentTemperatureAsync();
    Task<double> GetTargetTemperatureAsync();
    Task<ThermostatMode> GetModeAsync();
    Task SimulateTemperatureChangeAsync(); // Simulate temperature change
}

#endregion

#region Implementation

/// <summary>
/// Thermostat control GAgent
/// </summary>
[GAgent("thermostat", "smarthome")]
public class ThermostatGAgent : GAgentBase<ThermostatState, ThermostatStateLogEvent>, IThermostatGAgent
{
    private IDisposable? _temperatureSimulationTimer;
    
    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Initialize thermostat ID
        if (string.IsNullOrEmpty(State.ThermostatId))
        {
            State.ThermostatId = this.GetGrainId().Key.ToString() ?? "default-thermostat";
        }
        
        // Start temperature simulation timer
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
            $"[Smart Thermostat] Controls temperature devices at {State.Location}.\n" +
            $"Current status: Room temperature {State.CurrentTemperature:F1}°C, Target temperature {State.TargetTemperature:F1}°C, Mode {State.Mode}\n\n" +
            $"Available commands:\n" +
            $"• SetTemperatureCommand - Set target temperature\n" +
            $"  Parameters: ThermostatId (string) - Device ID, Temperature (double) - Temperature value (16-30°C)\n" +
            $"• ChangeModeCommand - Switch working mode\n" +
            $"  Parameters: ThermostatId (string) - Device ID, Mode (string) - Mode (Off/Heating/Cooling/Auto)\n\n" +
            $"Usage examples:\n" +
            $"- User says 'set temperature to 22 degrees' → Use SetTemperatureCommand, Temperature=22.0\n" +
            $"- User says 'turn on cooling' → Use ChangeModeCommand, Mode='Cooling'\n" +
            $"- User says 'turn off air conditioning' → Use ChangeModeCommand, Mode='Off'");

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
        // Simulate temperature gradually approaching target temperature
        var diff = State.TargetTemperature - State.CurrentTemperature;
        if (Math.Abs(diff) > 0.1)
        {
            var change = diff * 0.1; // Change by 10% of difference each time
            var newTemp = State.CurrentTemperature + change;
            
            RaiseEvent(new CurrentTemperatureUpdatedLogEvent
            {
                Temperature = Math.Round(newTemp, 1),
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            // Publish temperature change event
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
        
        // Publish temperature change event
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
        
        // Publish mode change event
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