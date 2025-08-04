using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events;
using Aevatar.Workshop.GAgent.GAgents.SmartHome;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Aevatar.Workshop.Client.Services;

namespace Aevatar.Workshop.Client.Controllers
{
    [ApiController]
    [Route("api/smarthome")]
    public class SmartHomeDemoController : ControllerBase
    {
        private readonly ILogger<SmartHomeDemoController> _logger;
        private readonly IGAgentFactory _gAgentFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILocalizationService _localizationService;

        // Agent IDs - using fixed GUIDs to ensure consistency
        private static readonly Guid AI_AGENT_ID = "ai agent".ToGuid();
        private static readonly Guid LIGHT_ID = "light agent".ToGuid();
        private static readonly Guid THERMOSTAT_ID = "thermostat agent".ToGuid();
        private static readonly Guid SECURITY_ID = "security agent".ToGuid();
        private static readonly Guid CURTAIN_ID = "curtain agent".ToGuid();

        public SmartHomeDemoController(
            ILogger<SmartHomeDemoController> logger,
            IGAgentFactory gAgentFactory,
            IServiceProvider serviceProvider,
            ILocalizationService localizationService)
        {
            _logger = logger;
            _gAgentFactory = gAgentFactory;
            _serviceProvider = serviceProvider;
            _localizationService = localizationService;
        }

        /// <summary>
        /// Initialize smart home system
        /// </summary>
        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] InitializeRequest request)
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            
            try
            {
                // Create all smart agents
                var aiAgent = await _gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(AI_AGENT_ID);
                
                // Create light with proper configuration
                var lightConfig = new LightConfiguration
                {
                    LightId = LIGHT_ID.ToString("N"),
                    Location = "Living Room"
                };
                var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);
                _logger.LogInformation("Light GAgent created with GrainId: {GrainId}", light.GetGrainId().ToString());
                
                var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
                var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
                var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

                // Set AI as parent for all devices to receive events
                await aiAgent.RegisterAsync(light);
                await aiAgent.RegisterAsync(thermostat);
                await aiAgent.RegisterAsync(security);
                await aiAgent.RegisterAsync(curtain);

                // Check initialization status
                var wasInitialized = await aiAgent.IsInitializedAsync();
                _logger.LogInformation("AI Agent initialization status before: {WasInitialized}", wasInitialized);
                
                // Initialize AI and automatically register GAgent tools
                var aiInitialized = await aiAgent.InitializeAsync(request.SystemLLM);
                _logger.LogInformation("AI Agent initialization result: {AiInitialized}", aiInitialized);
                
                // Check initialization status again
                var isNowInitialized = await aiAgent.IsInitializedAsync();
                _logger.LogInformation("AI Agent initialization status after: {IsNowInitialized}", isNowInitialized);

                // Get initial device states
                var deviceStates = await GetDeviceStatesInternal();

                return Ok(new
                {
                    success = true,
                    message = _localizationService.GetText("system_initialized", language),
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
                _logger.LogError(ex, "Failed to initialize smart home system");
                return StatusCode(500, new { 
                    success = false, 
                    message = _localizationService.GetText("system_init_failed", language),
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Process natural language commands
        /// </summary>
        [HttpPost("command")]
        public async Task<IActionResult> ProcessCommand([FromBody] CommandRequest request)
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            
            try
            {
                _logger.LogInformation("Processing command: {Command}", request.Command);

                var aiAgent = await _gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(AI_AGENT_ID);
                var result = await aiAgent.ProcessCommandAsync(request.Command);

                // Delay to allow device state updates
                await Task.Delay(500);

                // Get current states of all devices
                var deviceStates = await GetDeviceStatesInternal();

                // Format tool call information
                var toolCallsInfo = result.ToolCalls?.Select(tc => new
                {
                    functionName = tc.ToolName,
                    parameters = tc.Arguments,
                    result = tc.Result,
                    durationMs = tc.DurationMs,
                    timestamp = tc.Timestamp
                }).ToList();

                _logger.LogInformation("Command processed: {Command}, Tool calls: {ToolCalls}", 
                    request.Command, JsonSerializer.Serialize(toolCallsInfo));

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
                _logger.LogError(ex, "Failed to process command: {Command}", request.Command);
                return Ok(new
                {
                    success = false,
                    response = _localizationService.GetText("sorry_error_occurred", language),
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Control light device
        /// </summary>
        [HttpPost("device/light")]
        public async Task<IActionResult> ControlLight([FromBody] LightControlRequest request)
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            
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
                _logger.LogError(ex, "Failed to control light");
                return StatusCode(500, new { 
                    success = false, 
                    message = _localizationService.GetText("device_control_failed", language)
                });
            }
        }

        /// <summary>
        /// Adjust light brightness
        /// </summary>
        [HttpPost("device/light/brightness")]
        public async Task<IActionResult> SetBrightness([FromBody] BrightnessRequest request)
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            
            try
            {
                var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID);
                await light.SetBrightnessAsync(request.Brightness);
                
                return Ok(new { 
                    success = true,
                    message = _localizationService.GetText("brightness_set", language)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set brightness");
                return StatusCode(500, new { 
                    success = false, 
                    message = _localizationService.GetText("device_control_failed", language)
                });
            }
        }

        /// <summary>
        /// Set temperature
        /// </summary>
        [HttpPost("device/thermostat/temperature")]
        public async Task<IActionResult> SetTemperature([FromBody] TemperatureRequest request)
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            
            try
            {
                var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
                await thermostat.SetTargetTemperatureAsync(request.Temperature);
                
                return Ok(new { 
                    success = true,
                    message = _localizationService.GetText("temperature_set", language)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set temperature");
                return StatusCode(500, new { 
                    success = false, 
                    message = _localizationService.GetText("device_control_failed", language)
                });
            }
        }

        /// <summary>
        /// Set thermostat mode
        /// </summary>
        [HttpPost("device/thermostat/mode")]
        public async Task<IActionResult> SetThermostatMode([FromBody] ModeRequest request)
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            
            try
            {
                var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
                
                // Convert string to ThermostatMode enum
                if (!Enum.TryParse<ThermostatMode>(request.Mode, true, out var mode))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = _localizationService.GetText("invalid_mode", new object[] { request.Mode }, language)
                    });
                }
                
                await thermostat.SetModeAsync(mode);
                
                return Ok(new { 
                    success = true,
                    message = _localizationService.GetText("mode_set", language)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set thermostat mode");
                return StatusCode(500, new { 
                    success = false, 
                    message = _localizationService.GetText("device_control_failed", language)
                });
            }
        }

        /// <summary>
        /// Control security system
        /// </summary>
        [HttpPost("device/security")]
        public async Task<IActionResult> ControlSecurity([FromBody] SecurityControlRequest request)
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            
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
                            ? _localizationService.GetText("no_motion", language)
                            : _localizationService.GetText("motion_detected_at", 
                                new object[] { state.LastMotionDetected.ToString("yyyy-MM-dd HH:mm:ss"), state.LastMotionLocation }, 
                                language)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to control security system");
                return StatusCode(500, new { 
                    success = false, 
                    message = _localizationService.GetText("device_control_failed", language)
                });
            }
        }

        /// <summary>
        /// Get all device states
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetAllDeviceStates()
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            
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
                _logger.LogError(ex, "Failed to get device states");
                return StatusCode(500, new { 
                    success = false, 
                    message = _localizationService.GetText("get_states_failed", language)
                });
            }
        }

        /// <summary>
        /// Control curtain
        /// </summary>
        [HttpPost("device/curtain")]
        public async Task<IActionResult> ControlCurtain([FromBody] CurtainControlRequest request)
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            
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
                _logger.LogError(ex, "Failed to control curtain: {Level}", request.Level);
                return StatusCode(500, new { 
                    success = false, 
                    message = _localizationService.GetText("device_control_failed", language)
                });
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

            var language = _localizationService.GetCurrentLanguage(HttpContext);

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
                    mode = thermostatState.Mode.ToString().ToLower() // Convert enum to lowercase string
                },
                ["security"] = new
                {
                    isArmed = securityState.IsArmed,
                    lastActivity = securityState.LastMotionDetected == DateTime.MinValue 
                        ? _localizationService.GetText("no_motion", language)
                        : _localizationService.GetText("motion_detected_at", 
                            new object[] { securityState.LastMotionDetected.ToString("yyyy-MM-dd HH:mm:ss"), securityState.LastMotionLocation }, 
                            language)
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
            public string SystemLLM { get; set; } = "NewKey";
        }

        public class CommandRequest
        {
            public string Command { get; set; } = string.Empty;
            public string SystemLLM { get; set; } = string.Empty;
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
        /// Get GAgent activity history
        /// </summary>
        [HttpGet("gagent-activities")]
        public async Task<IActionResult> GetGAgentActivities()
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            
            try
            {
                var activities = new List<object>();
                
                // Get descriptions and states of various GAgents
                var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID);
                var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
                var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
                var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);
                
                // Get descriptions
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
                
                // Get AI chat history
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
                _logger.LogError(ex, "Failed to get GAgent activities");
                return StatusCode(500, new { 
                    success = false, 
                    message = _localizationService.GetText("get_activities_failed", language)
                });
            }
        }
    }
} 