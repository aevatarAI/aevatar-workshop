using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events;
using Aevatar.Workshop.GAgent.GAgents.SmartHome;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.Client.Controllers
{
    [ApiController]
    [Route("api/smarthome")]
    public class SmartHomeDemoController : ControllerBase
    {
        private readonly ILogger<SmartHomeDemoController> _logger;
        private readonly IGAgentFactory _gAgentFactory;
        private readonly IServiceProvider _serviceProvider;

        // Agent IDs - 使用固定的 Guid 以确保一致性
        private static readonly Guid AI_AGENT_ID = "ai agent".ToGuid();
        private static readonly Guid LIGHT_ID = "light agent".ToGuid();
        private static readonly Guid THERMOSTAT_ID = "thermostat agent".ToGuid();
        private static readonly Guid SECURITY_ID = "security agent".ToGuid();
        private static readonly Guid CURTAIN_ID = "curtain agent".ToGuid();

        public SmartHomeDemoController(
            ILogger<SmartHomeDemoController> logger,
            IGAgentFactory gAgentFactory,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _gAgentFactory = gAgentFactory;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 初始化智能家居系统
        /// </summary>
        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] InitializeRequest request)
        {
            try
            {
                // 创建所有智能体
                var aiAgent = await _gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(AI_AGENT_ID);
                
                // Create light with proper configuration
                var lightConfig = new LightConfiguration
                {
                    LightId = LIGHT_ID.ToString("N"),
                    Location = "客厅"
                };
                var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);
                _logger.LogInformation($"Light GAgent GrainId: {light.GetGrainId().ToString()}");
                var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
                var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
                var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

                // 设置 AI 为所有设备的父级，以便接收事件
                await aiAgent.RegisterAsync(light);
                await aiAgent.RegisterAsync(thermostat);
                await aiAgent.RegisterAsync(security);
                await aiAgent.RegisterAsync(curtain);

                // 初始化 AI 并自动注册 GAgent tools
                var aiInitialized = await aiAgent.InitializeAsync(request.SystemLLM);

                // 获取初始设备状态
                var deviceStates = await GetDeviceStatesInternal();

                return Ok(new
                {
                    success = true,
                    message = "智能家居系统初始化成功",
                    aiInitialized,
                    deviceStates,
                    deviceIds = new
                    {
                        light = LIGHT_ID.ToString(),
                        thermostat = THERMOSTAT_ID.ToString(),
                        security = SECURITY_ID.ToString(),
                        curtain = CURTAIN_ID.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化智能家居系统失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 处理自然语言命令
        /// </summary>
        [HttpPost("command")]
        public async Task<IActionResult> ProcessCommand([FromBody] CommandRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing command: {request.Command}");

                var aiAgent = await _gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(AI_AGENT_ID);
                var result = await aiAgent.ProcessCommandAsync(request.Command);

                // 延迟一下让设备状态更新
                await Task.Delay(500);

                // 获取所有设备的当前状态
                var deviceStates = await GetDeviceStatesInternal();

                // 格式化工具调用信息
                var toolCallsInfo = result.ToolCalls?.Select(tc => new
                {
                    functionName = tc.ToolName,
                    parameters = tc.Arguments,
                    result = tc.Result,
                    durationMs = tc.DurationMs,
                    timestamp = tc.Timestamp
                }).ToList();

                _logger.LogInformation($"Processed command: {request.Command}, Result: {JsonSerializer.Serialize(toolCallsInfo)}");

                return Ok(new
                {
                    success = true,
                    response = result.Response,
                    deviceUpdates = deviceStates,
                    toolCalls = toolCallsInfo,
                    totalDurationMs = result.TotalDurationMs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理命令失败: {Command}", request.Command);
                return Ok(new
                {
                    success = false,
                    response = "抱歉，处理您的请求时出现了错误。",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 控制灯光
        /// </summary>
        [HttpPost("device/light")]
        public async Task<IActionResult> ControlLight([FromBody] LightControlRequest request)
        {
            try
            {
                var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID);
                
                if (request.Action == "on")
                    await light.TurnOnAsync();
                else
                    await light.TurnOffAsync();

                var state = await light.GetStateAsync();
                
                return Ok(new
                {
                    success = true,
                    state = new
                    {
                        isOn = state.IsOn,
                        brightness = state.Brightness
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "控制灯光失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 调整灯光亮度
        /// </summary>
        [HttpPost("device/light/brightness")]
        public async Task<IActionResult> SetBrightness([FromBody] BrightnessRequest request)
        {
            try
            {
                var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID);
                await light.SetBrightnessAsync(request.Brightness);
                
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调整亮度失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 设置温度
        /// </summary>
        [HttpPost("device/thermostat/temperature")]
        public async Task<IActionResult> SetTemperature([FromBody] TemperatureRequest request)
        {
            try
            {
                var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
                await thermostat.SetTargetTemperatureAsync(request.Temperature);
                
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置温度失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 设置恒温器模式
        /// </summary>
        [HttpPost("device/thermostat/mode")]
        public async Task<IActionResult> SetThermostatMode([FromBody] ModeRequest request)
        {
            try
            {
                var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
                
                // 将 string 转换为 ThermostatMode 枚举
                if (!Enum.TryParse<ThermostatMode>(request.Mode, true, out var mode))
                {
                    return BadRequest(new { success = false, message = $"无效的模式: {request.Mode}" });
                }
                
                await thermostat.SetModeAsync(mode);
                
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置模式失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 控制安防系统
        /// </summary>
        [HttpPost("device/security")]
        public async Task<IActionResult> ControlSecurity([FromBody] SecurityControlRequest request)
        {
            try
            {
                var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
                
                if (request.Action == "arm")
                    await security.ArmAsync();
                else
                    await security.DisarmAsync();

                var state = await security.GetStateAsync();
                
                return Ok(new
                {
                    success = true,
                    state = new
                    {
                        isArmed = state.IsArmed,
                        lastActivity = state.LastMotionDetected == DateTime.MinValue 
                            ? "无" 
                            : $"{state.LastMotionDetected:yyyy-MM-dd HH:mm:ss} - {state.LastMotionLocation}"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "控制安防系统失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取所有设备状态
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetAllDeviceStates()
        {
            try
            {
                var states = await GetDeviceStatesInternal();
                return Ok(new
                {
                    success = true,
                    devices = states
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备状态失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 控制窗帘
        /// </summary>
        [HttpPost("device/curtain")]
        public async Task<IActionResult> ControlCurtain([FromBody] CurtainControlRequest request)
        {
            try
            {
                var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);
                await curtain.SetPositionAsync(request.Level);

                var state = await curtain.GetStateAsync();
                return Ok(new
                {
                    success = true,
                    currentPosition = state.CurrentPosition,
                    isMoving = state.IsMoving,
                    targetPosition = state.TargetPosition
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "控制窗帘失败: {Level}", request.Level);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task<Dictionary<string, object>> GetDeviceStatesInternal()
        {
            var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID);
            var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
            var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
            var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

            var lightState = await light.GetStateAsync();
            var thermostatState = await thermostat.GetStateAsync();
            var securityState = await security.GetStateAsync();
            var curtainState = await curtain.GetStateAsync();

            return new Dictionary<string, object>
            {
                ["light"] = new
                {
                    isOn = lightState.IsOn,
                    brightness = lightState.Brightness
                },
                ["thermostat"] = new
                {
                    temperature = thermostatState.CurrentTemperature,
                    targetTemperature = thermostatState.TargetTemperature,
                    mode = thermostatState.Mode.ToString().ToLower() // 将枚举转换为小写字符串
                },
                ["security"] = new
                {
                    isArmed = securityState.IsArmed,
                    lastActivity = securityState.LastMotionDetected == DateTime.MinValue 
                        ? "无" 
                        : $"{securityState.LastMotionDetected:yyyy-MM-dd HH:mm:ss} - {securityState.LastMotionLocation}"
                },
                ["curtain"] = new
                {
                    openLevel = curtainState.CurrentPosition,
                    isMoving = curtainState.IsMoving,
                    targetPosition = curtainState.TargetPosition
                }
            };
        }

        // Request DTOs
        public class InitializeRequest
        {
            public string SystemLLM { get; set; } = "OpenAI";
        }

        public class CommandRequest
        {
            public string Command { get; set; } = string.Empty;
        }

        public class SceneRequest
        {
            public string Scene { get; set; } = string.Empty;
        }

        public class LightControlRequest
        {
            public string Action { get; set; } = string.Empty;
            public string DeviceId { get; set; } = string.Empty;
        }

        public class BrightnessRequest
        {
            public int Brightness { get; set; }
            public string DeviceId { get; set; } = string.Empty;
        }

        public class TemperatureRequest
        {
            public int Temperature { get; set; }
        }

        public class ModeRequest
        {
            public string Mode { get; set; } = string.Empty;
        }

        public class SecurityControlRequest
        {
            public string Action { get; set; } = string.Empty;
        }

        public class CurtainControlRequest
        {
            public int Level { get; set; }
        }
        
        /// <summary>
        /// 获取 GAgent 活动历史
        /// </summary>
        [HttpGet("gagent-activities")]
        public async Task<IActionResult> GetGAgentActivities()
        {
        try
        {
            var activities = new List<object>();
            
            // 获取各个 GAgent 的描述和状态
            var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID);
            var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
            var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
            var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);
            
            // 获取描述
            activities.Add(new
            {
                gagent = "LightGAgent",
                description = await light.GetDescriptionAsync(),
                timestamp = DateTime.UtcNow
            });
            
            activities.Add(new
            {
                gagent = "ThermostatGAgent",
                description = await thermostat.GetDescriptionAsync(),
                timestamp = DateTime.UtcNow
            });
            
            activities.Add(new
            {
                gagent = "SecurityGAgent",
                description = await security.GetDescriptionAsync(),
                timestamp = DateTime.UtcNow
            });
            
            activities.Add(new
            {
                gagent = "CurtainGAgent",
                description = await curtain.GetDescriptionAsync(),
                timestamp = DateTime.UtcNow
            });
            
            // 获取 AI 聊天历史
            var aiAgent = await _gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(AI_AGENT_ID);
            var chatHistory = await aiAgent.GetChatHistoryAsync();
            
            activities.Add(new
            {
                gagent = "HomeAIGAgent",
                chatHistory = chatHistory.TakeLast(10),
                timestamp = DateTime.UtcNow
            });
            
            return Ok(new
            {
                success = true,
                activities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 GAgent 活动失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
    }
} 