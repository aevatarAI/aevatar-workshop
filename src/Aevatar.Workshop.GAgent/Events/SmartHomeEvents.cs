using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Workshop.GAgent.Events;

#region AI Intent Events

/// <summary>
/// 设备控制意图事件 - AI理解的用户意图
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
/// 场景激活意图事件 - AI识别的场景请求
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
/// 开灯命令
/// </summary>
[GenerateSerializer]
public class TurnOnLightCommand : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("灯光设备的唯一标识符")]
    public string LightId { get; set; } = string.Empty;
}

/// <summary>
/// 关灯命令
/// </summary>
[GenerateSerializer]
public class TurnOffLightCommand : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("灯光设备的唯一标识符")]
    public string LightId { get; set; } = string.Empty;
}

/// <summary>
/// 设置亮度命令
/// </summary>
[GenerateSerializer]
public class SetBrightnessCommand : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("灯光设备的唯一标识符")]
    public string LightId { get; set; } = string.Empty;
    
    [Id(1)] 
    [System.ComponentModel.Description("目标亮度值，范围 0-100，0 表示最暗，100 表示最亮")]
    public int Brightness { get; set; } // 0-100
}

/// <summary>
/// 灯光状态变化事件
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
/// 设置温度命令
/// </summary>
[GenerateSerializer]
public class SetTemperatureCommand : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("恒温器设备的唯一标识符")]
    public string ThermostatId { get; set; } = string.Empty;
    
    [Id(1)] 
    [System.ComponentModel.Description("目标温度，单位：摄氏度，建议范围 16-30")]
    public double TargetTemperature { get; set; }
}

/// <summary>
/// 改变模式命令
/// </summary>
[GenerateSerializer]
public class ChangeModeCommand : EventBase
{
    [Id(0)] public string ThermostatId { get; set; } = string.Empty;
    [Id(1)] public ThermostatMode Mode { get; set; }
}

/// <summary>
/// 温度变化事件
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
/// 模式变化事件
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
/// 布防命令
/// </summary>
[GenerateSerializer]
public class ArmSecurityCommand : EventBase
{
    [Id(0)] public string SecuritySystemId { get; set; } = string.Empty;
}

/// <summary>
/// 撤防命令
/// </summary>
[GenerateSerializer]
public class DisarmSecurityCommand : EventBase
{
    [Id(0)] public string SecuritySystemId { get; set; } = string.Empty;
}

/// <summary>
/// 安防状态变化事件
/// </summary>
[GenerateSerializer]
public class SecurityStateChangedEvent : EventBase
{
    [Id(0)] public string SecuritySystemId { get; set; } = string.Empty;
    [Id(1)] public bool IsArmed { get; set; }
    [Id(2)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 检测到移动事件
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
/// 场景执行命令
/// </summary>
[GenerateSerializer]
public class ExecuteSceneCommand : EventBase
{
    [Id(0)] public string SceneName { get; set; } = string.Empty;
    [Id(1)] public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 场景执行完成事件
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