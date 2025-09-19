using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Workshop.GAgent.Events;

#region AI Intent Events

/// <summary>
/// Device control intent event - AI understood user intent
/// </summary>
[GenerateSerializer]
public class DeviceControlIntentEvent : EventBase
{
    [Id(0)] public string DeviceType { get; set; } = string.Empty; // "light", "thermostat", "security"
    [Id(1)] public string Action { get; set; } = string.Empty; // "turn_on", "turn_off", "set_brightness", etc.
    [Id(2)] public Dictionary<string, object> Parameters { get; set; } = new();
    [Id(3)] public string OriginalCommand { get; set; } = string.Empty;
    [Id(4)] public string? Location { get; set; } // "living room", "bedroom", etc.
}

/// <summary>
/// Scene activation intent event - AI recognized scene request
/// </summary>
[GenerateSerializer]
public class SceneActivationIntentEvent : EventBase
{
    [Id(0)] public string SceneName { get; set; } = string.Empty; // "goodnight", "goodmorning", "away"
    [Id(1)] public string OriginalCommand { get; set; } = string.Empty;
}

#endregion

#region Light Events

/// <summary>
/// Turn on light command
/// </summary>
[GenerateSerializer]
public class TurnOnLightCommand : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("Unique identifier for light device")]
    public string LightId { get; set; } = string.Empty;
}

/// <summary>
/// Turn off light command
/// </summary>
[GenerateSerializer]
public class TurnOffLightCommand : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("Unique identifier for light device")]
    public string LightId { get; set; } = string.Empty;
}

/// <summary>
/// Set brightness command
/// </summary>
[GenerateSerializer]
public class SetBrightnessCommand : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("Unique identifier for light device")]
    public string LightId { get; set; } = string.Empty;
    
    [Id(1)] 
    [System.ComponentModel.Description("Target brightness value, range 0-100, 0 means darkest, 100 means brightest")]
    public int Brightness { get; set; } // 0-100
}

/// <summary>
/// Light state change event
/// </summary>
[GenerateSerializer]
public class LightStateChangedEvent : EventBase
{
    [Id(0)] public string LightId { get; set; } = string.Empty;
    [Id(1)] public bool IsOn { get; set; }
    [Id(2)] public int Brightness { get; set; }
    [Id(3)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region Thermostat Events

/// <summary>
/// Set temperature command
/// </summary>
[GenerateSerializer]
public class SetTemperatureCommand : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("Unique identifier for thermostat device")]
    public string ThermostatId { get; set; } = string.Empty;
    
    [Id(1)] 
    [System.ComponentModel.Description("Target temperature, unit: Celsius, recommended range 16-30")]
    public double TargetTemperature { get; set; }
}

/// <summary>
/// Change mode command
/// </summary>
[GenerateSerializer]
public class ChangeModeCommand : EventBase
{
    [Id(0)] public string ThermostatId { get; set; } = string.Empty;
    [Id(1)] public ThermostatMode Mode { get; set; }
}

/// <summary>
/// Temperature change event
/// </summary>
[GenerateSerializer]
public class TemperatureChangedEvent : EventBase
{
    [Id(0)] public string ThermostatId { get; set; } = string.Empty;
    [Id(1)] public double CurrentTemperature { get; set; }
    [Id(2)] public double TargetTemperature { get; set; }
    [Id(3)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Mode change event
/// </summary>
[GenerateSerializer]
public class ModeChangedEvent : EventBase
{
    [Id(0)] public string ThermostatId { get; set; } = string.Empty;
    [Id(1)] public ThermostatMode Mode { get; set; }
    [Id(2)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

[GenerateSerializer]
public enum ThermostatMode
{
    Off,
    Heating,
    Cooling,
    Auto
}

#endregion

#region Security Events

/// <summary>
/// Arm command
/// </summary>
[GenerateSerializer]
public class ArmSecurityCommand : EventBase
{
    [Id(0)] public string SecuritySystemId { get; set; } = string.Empty;
}

/// <summary>
/// Disarm command
/// </summary>
[GenerateSerializer]
public class DisarmSecurityCommand : EventBase
{
    [Id(0)] public string SecuritySystemId { get; set; } = string.Empty;
}

/// <summary>
/// Security state change event
/// </summary>
[GenerateSerializer]
public class SecurityStateChangedEvent : EventBase
{
    [Id(0)] public string SecuritySystemId { get; set; } = string.Empty;
    [Id(1)] public bool IsArmed { get; set; }
    [Id(2)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Motion detected event
/// </summary>
[GenerateSerializer]
public class MotionDetectedEvent : EventBase
{
    [Id(0)] public string SecuritySystemId { get; set; } = string.Empty;
    [Id(1)] public string Location { get; set; } = string.Empty;
    [Id(2)] public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region Coordinator Events

/// <summary>
/// Execute scene command
/// </summary>
[GenerateSerializer]
public class ExecuteSceneCommand : EventBase
{
    [Id(0)] public string SceneName { get; set; } = string.Empty;
    [Id(1)] public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Scene execution completed event
/// </summary>
[GenerateSerializer]
public class SceneExecutedEvent : EventBase
{
    [Id(0)] public string SceneName { get; set; } = string.Empty;
    [Id(1)] public bool Success { get; set; }
    [Id(2)] public string? ErrorMessage { get; set; }
    [Id(3)] public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

#endregion 