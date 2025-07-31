using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Aevatar.Workshop.Client.Services
{
    /// <summary>
    /// Implementation of localization service
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private string _currentLanguage = "en";
        
        // Language resources dictionary
        private readonly Dictionary<string, Dictionary<string, string>> _resources = new()
        {
            ["en"] = new Dictionary<string, string>
            {
                // System messages
                ["system_initialized"] = "Smart home system initialized successfully",
                ["system_init_failed"] = "Failed to initialize smart home system",
                ["command_processed"] = "Command processed successfully",
                ["command_failed"] = "Failed to process command",
                ["device_control_failed"] = "Failed to control device",
                ["get_states_failed"] = "Failed to get device states",
                ["get_activities_failed"] = "Failed to get GAgent activities",
                
                // User responses
                ["sorry_error_occurred"] = "Sorry, an error occurred while processing your request",
                ["invalid_mode"] = "Invalid mode: {0}",
                ["brightness_set"] = "Brightness set successfully",
                ["temperature_set"] = "Temperature set successfully",
                ["mode_set"] = "Mode set successfully",
                
                // Device states
                ["no_motion"] = "None",
                ["motion_detected_at"] = "{0} - {1}",
                
                // General
                ["success"] = "Success",
                ["failed"] = "Failed",
                ["loading"] = "Loading...",
                ["error"] = "Error",
                
                // UI Resources
                ["smart_home_demo"] = "Smart Home Demo",
                ["initialize_system"] = "Initialize System",
                ["send_command"] = "Send Command",
                ["device_status"] = "Device Status",
                ["light_control"] = "Light Control",
                ["thermostat_control"] = "Thermostat Control",
                ["security_control"] = "Security Control",
                ["curtain_control"] = "Curtain Control",
                ["turn_on"] = "Turn On",
                ["turn_off"] = "Turn Off",
                ["set_brightness"] = "Set Brightness",
                ["set_temperature"] = "Set Temperature",
                ["set_mode"] = "Set Mode",
                ["arm_security"] = "Arm Security",
                ["disarm_security"] = "Disarm Security",
                ["open_curtain"] = "Open Curtain",
                ["close_curtain"] = "Close Curtain",
                ["enter_command"] = "Enter your command here...",
                ["processing"] = "Processing...",
                ["response"] = "Response",
                ["device_states"] = "Device States",
                ["tool_calls"] = "Tool Calls",
                ["brightness"] = "Brightness",
                ["temperature"] = "Temperature",
                ["mode"] = "Mode",
                ["armed"] = "Armed",
                ["disarmed"] = "Disarmed",
                ["position"] = "Position",
                ["moving"] = "Moving",
                ["stopped"] = "Stopped"
            },
            ["zh"] = new Dictionary<string, string>
            {
                // System messages  
                ["system_initialized"] = "Smart home system initialized successfully",
                ["system_init_failed"] = "Smart home system initialization failed",
                ["command_processed"] = "Command processed successfully",
                ["command_failed"] = "Command processing failed",
                ["device_control_failed"] = "Device control failed",
                ["get_states_failed"] = "Failed to get device states", 
                ["get_activities_failed"] = "Failed to get GAgent activities",
                
                // User responses
                ["sorry_error_occurred"] = "Sorry, an error occurred while processing your request",
                ["invalid_mode"] = "Invalid mode: {0}",
                ["brightness_set"] = "Brightness set successfully",
                ["temperature_set"] = "Temperature set successfully", 
                ["mode_set"] = "Mode set successfully",
                
                // Device states
                ["no_motion"] = "None",
                ["motion_detected_at"] = "{0} - {1}",
                
                // General
                ["success"] = "Success",
                ["failed"] = "Failed", 
                ["loading"] = "Loading...",
                ["error"] = "Error",
                
                // UI Resources
                ["smart_home_demo"] = "Smart Home Demo",
                ["initialize_system"] = "Initialize System",
                ["send_command"] = "Send Command",
                ["device_status"] = "Device Status",
                ["light_control"] = "Light Control",
                ["thermostat_control"] = "Thermostat Control",
                ["security_control"] = "Security Control",
                ["curtain_control"] = "Curtain Control",
                ["turn_on"] = "Turn On",
                ["turn_off"] = "Turn Off",
                ["set_brightness"] = "Set Brightness",
                ["set_temperature"] = "Set Temperature",
                ["set_mode"] = "Set Mode",
                ["arm_security"] = "Arm Security",
                ["disarm_security"] = "Disarm Security",
                ["open_curtain"] = "Open Curtain",
                ["close_curtain"] = "Close Curtain",
                ["enter_command"] = "Enter your command here...",
                ["processing"] = "Processing...",
                ["response"] = "Response",
                ["device_states"] = "Device States",
                ["tool_calls"] = "Tool Calls",
                ["brightness"] = "Brightness",
                ["temperature"] = "Temperature",
                ["mode"] = "Mode",
                ["armed"] = "Armed",
                ["disarmed"] = "Disarmed",
                ["position"] = "Position",
                ["moving"] = "Moving",
                ["stopped"] = "Stopped"
            }
        };

        public string GetCurrentLanguage(HttpContext context)
        {
            // Check URL query parameter first
            if (context.Request.Query.ContainsKey("lang"))
            {
                var langParam = context.Request.Query["lang"].ToString().ToLower();
                if (IsValidLanguage(langParam))
                {
                    return langParam;
                }
            }

            // Check Accept-Language header
            var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                var languages = acceptLanguage.Split(',')
                    .Select(x => x.Split(';')[0].Trim().ToLower())
                    .ToArray();

                foreach (var lang in languages)
                {
                    if (lang.StartsWith("zh"))
                        return "zh";
                    if (lang.StartsWith("en"))
                        return "en";
                }
            }

            // Default to English
            return "en";
        }

        public string GetText(string key, string language = null)
        {
            language ??= _currentLanguage;
            
            if (_resources.ContainsKey(language) && _resources[language].ContainsKey(key))
            {
                return _resources[language][key];
            }

            // Fallback to English
            if (language != "en" && _resources["en"].ContainsKey(key))
            {
                return _resources["en"][key];
            }

            // Return key if translation not found
            return key;
        }

        public string GetText(string key, object[] parameters, string language = null)
        {
            var text = GetText(key, language);
            try
            {
                return string.Format(text, parameters);
            }
            catch
            {
                return text;
            }
        }

        public void SetCurrentLanguage(string language)
        {
            if (IsValidLanguage(language))
            {
                _currentLanguage = language;
            }
        }

        private bool IsValidLanguage(string language)
        {
            return _resources.ContainsKey(language.ToLower());
        }
    }
} 