using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent.GAgents.SmartHome;

#region State and Events

/// <summary>
/// Light state
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
/// State log event base class
/// </summary>
[GenerateSerializer]
public class LightStateLogEvent : StateLogEventBase<LightStateLogEvent>;

/// <summary>
/// Light turned on event
/// </summary>
[GenerateSerializer]
public class LightTurnedOnLogEvent : LightStateLogEvent
{
    [Id(0)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// Light turned off event
/// </summary>
[GenerateSerializer]
public class LightTurnedOffLogEvent : LightStateLogEvent
{
    [Id(0)] public DateTime Timestamp { get; set; }
}

/// <summary>
/// Brightness changed event
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
/// Light control interface
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
/// Light control GAgent
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
        // Initialize state using events
        RaiseEvent(new SetLightInitialConfigurationLogEvent
        {
            LightId = configuration.LightId,
            Location = configuration.Location
        });
        
        await ConfirmEvents();
    }

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult(
            $"【Smart Light Controller】Controls lighting devices in {State.Location}.\n" +
            $"Current status: {(State.IsOn ? "On" : "Off")}, Brightness: {State.Brightness}%\n\n" +
            $"Available commands:\n" +
            $"• TurnOnLightCommand - Turn on light\n" +
            $"  Parameters: LightId (string) - Device ID\n" +
            $"• TurnOffLightCommand - Turn off light\n" + 
            $"  Parameters: LightId (string) - Device ID\n" +
            $"• SetBrightnessCommand - Adjust brightness\n" +
            $"  Parameters: LightId (string) - Device ID, Brightness (int) - Brightness value (0-100)\n\n" +
            $"Usage examples:\n" +
            $"- User says 'turn on light' → Use TurnOnLightCommand\n" +
            $"- User says 'set light to 20%' → Use SetBrightnessCommand, Brightness=20\n" +
            $"- User says 'dim the light' → Use SetBrightnessCommand, decrease current brightness value");

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
            return;
        }

        Logger.LogInformation("Turning on light {LightId}", State.LightId);
        await TurnOnAsync();
        
        // Publish state change event
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
            return;
        }

        Logger.LogInformation("Turning off light {LightId}", State.LightId);
        await TurnOffAsync();
        
        // Publish state change event
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
        
        // Publish state change event
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