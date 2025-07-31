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
                ["system_initialized"] = "智能家居系统初始化成功",
                ["system_init_failed"] = "智能家居系统初始化失败",
                ["command_processed"] = "命令处理成功",
                ["command_failed"] = "命令处理失败",
                ["device_control_failed"] = "设备控制失败",
                ["get_states_failed"] = "获取设备状态失败", 
                ["get_activities_failed"] = "获取 GAgent 活动失败",
                
                // User responses
                ["sorry_error_occurred"] = "抱歉，处理您的请求时出现了错误",
                ["invalid_mode"] = "无效的模式: {0}",
                ["brightness_set"] = "亮度设置成功",
                ["temperature_set"] = "温度设置成功", 
                ["mode_set"] = "模式设置成功",
                
                // Device states
                ["no_motion"] = "无",
                ["motion_detected_at"] = "{0} - {1}",
                
                // General
                ["success"] = "成功",
                ["failed"] = "失败", 
                ["loading"] = "加载中...",
                ["error"] = "错误",
                
                // UI Resources
                ["smart_home_demo"] = "智能家居演示",
                ["initialize_system"] = "初始化系统",
                ["send_command"] = "发送命令",
                ["device_status"] = "设备状态",
                ["light_control"] = "灯光控制",
                ["thermostat_control"] = "恒温器控制",
                ["security_control"] = "安防控制",
                ["curtain_control"] = "窗帘控制",
                ["turn_on"] = "开启",
                ["turn_off"] = "关闭",
                ["set_brightness"] = "设置亮度",
                ["set_temperature"] = "设置温度",
                ["set_mode"] = "设置模式",
                ["arm_security"] = "启动安防",
                ["disarm_security"] = "关闭安防",
                ["open_curtain"] = "打开窗帘",
                ["close_curtain"] = "关闭窗帘",
                ["enter_command"] = "请在此输入您的命令...",
                ["processing"] = "处理中...",
                ["response"] = "响应",
                ["device_states"] = "设备状态",
                ["tool_calls"] = "工具调用",
                ["brightness"] = "亮度",
                ["temperature"] = "温度",
                ["mode"] = "模式",
                ["armed"] = "已启动",
                ["disarmed"] = "已关闭",
                ["position"] = "位置",
                ["moving"] = "移动中",
                ["stopped"] = "已停止"
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