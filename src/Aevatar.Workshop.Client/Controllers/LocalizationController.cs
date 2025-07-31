using Microsoft.AspNetCore.Mvc;
using Aevatar.Workshop.Client.Services;

namespace Aevatar.Workshop.Client.Controllers
{
    [ApiController]
    [Route("api/localization")]
    public class LocalizationController : ControllerBase
    {
        private readonly ILocalizationService _localizationService;

        public LocalizationController(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        /// <summary>
        /// Get current language preference based on request headers or parameters
        /// </summary>
        [HttpGet("current-language")]
        public IActionResult GetCurrentLanguage()
        {
            var language = _localizationService.GetCurrentLanguage(HttpContext);
            return Ok(new { language });
        }

        /// <summary>
        /// Get localized resources for the frontend
        /// </summary>
        [HttpGet("resources")]
        public IActionResult GetResources([FromQuery] string language = null)
        {
            language ??= _localizationService.GetCurrentLanguage(HttpContext);
            
            var resources = new Dictionary<string, string>
            {
                // Smart Home Demo UI Resources
                ["smart_home_demo"] = _localizationService.GetText("smart_home_demo", language),
                ["initialize_system"] = _localizationService.GetText("initialize_system", language),
                ["send_command"] = _localizationService.GetText("send_command", language),
                ["device_status"] = _localizationService.GetText("device_status", language),
                ["light_control"] = _localizationService.GetText("light_control", language),
                ["thermostat_control"] = _localizationService.GetText("thermostat_control", language),
                ["security_control"] = _localizationService.GetText("security_control", language),
                ["curtain_control"] = _localizationService.GetText("curtain_control", language),
                ["turn_on"] = _localizationService.GetText("turn_on", language),
                ["turn_off"] = _localizationService.GetText("turn_off", language),
                ["set_brightness"] = _localizationService.GetText("set_brightness", language),
                ["set_temperature"] = _localizationService.GetText("set_temperature", language),
                ["set_mode"] = _localizationService.GetText("set_mode", language),
                ["arm_security"] = _localizationService.GetText("arm_security", language),
                ["disarm_security"] = _localizationService.GetText("disarm_security", language),
                ["open_curtain"] = _localizationService.GetText("open_curtain", language),
                ["close_curtain"] = _localizationService.GetText("close_curtain", language),
                ["enter_command"] = _localizationService.GetText("enter_command", language),
                ["processing"] = _localizationService.GetText("processing", language),
                ["response"] = _localizationService.GetText("response", language),
                ["device_states"] = _localizationService.GetText("device_states", language),
                ["tool_calls"] = _localizationService.GetText("tool_calls", language),
                ["brightness"] = _localizationService.GetText("brightness", language),
                ["temperature"] = _localizationService.GetText("temperature", language),
                ["mode"] = _localizationService.GetText("mode", language),
                ["armed"] = _localizationService.GetText("armed", language),
                ["disarmed"] = _localizationService.GetText("disarmed", language),
                ["position"] = _localizationService.GetText("position", language),
                ["moving"] = _localizationService.GetText("moving", language),
                ["stopped"] = _localizationService.GetText("stopped", language)
            };

            return Ok(new { language, resources });
        }

        /// <summary>
        /// Get supported languages
        /// </summary>
        [HttpGet("supported-languages")]
        public IActionResult GetSupportedLanguages()
        {
            var languages = new[]
            {
                new { code = "en", name = "English" },
                new { code = "zh", name = "Chinese" }
            };

            return Ok(new { languages });
        }
    }
} 