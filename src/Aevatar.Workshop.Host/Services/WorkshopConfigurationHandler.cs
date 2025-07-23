using Aevatar.GAgents.AI.Options;
using Aevatar.Workshop.GAgent;
using Aevatar.Workshop.Host.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Aevatar.Workshop.Host.Services;

/// <summary>
/// Implementation of IConfigurationHandler for Workshop Host
/// </summary>
public class WorkshopConfigurationHandler : IConfigurationHandler
{
    private readonly IOptionsMonitor<SystemLLMConfigOptions> _systemLLMOptions;
    private readonly IOptionsMonitor<MCPServerOptions> _mcpServerOptions;
    private readonly IConfigurationUpdateService _updateService;
    private readonly ILogger<WorkshopConfigurationHandler> _logger;

    public WorkshopConfigurationHandler(
        IOptionsMonitor<SystemLLMConfigOptions> systemLLMOptions,
        IOptionsMonitor<MCPServerOptions> mcpServerOptions,
        IConfigurationUpdateService updateService,
        ILogger<WorkshopConfigurationHandler> logger)
    {
        _systemLLMOptions = systemLLMOptions;
        _mcpServerOptions = mcpServerOptions;
        _updateService = updateService;
        _logger = logger;
    }

    public async Task<(bool success, string? errorMessage)> UpdateConfigurationAsync(
        string configType,
        string configJson,
        string? configKey = null)
    {
        try
        {
            return configType.ToLowerInvariant() switch
            {
                "systemllmconfigs" => await UpdateSystemLLMConfigsAsync(configJson, configKey),
                "mcpservers" => await UpdateMCPServersAsync(configJson, configKey),
                _ => (false, $"Unknown configuration type: {configType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration for type: {ConfigType}", configType);
            return (false, ex.Message);
        }
    }

    public async Task<(bool success, string configJson, string? errorMessage)> GetConfigurationAsync(
        string configType,
        string? configKey = null)
    {
        try
        {
            switch (configType.ToLowerInvariant())
            {
                case "systemllmconfigs":
                    var llmConfigs = await _updateService.GetSystemLLMConfigsAsync();
                    if (!string.IsNullOrEmpty(configKey) && llmConfigs.ContainsKey(configKey))
                    {
                        var singleConfig = new Dictionary<string, LLMConfig> { { configKey, llmConfigs[configKey] } };
                        return (true, JsonSerializer.Serialize(singleConfig), null);
                    }

                    return (true, JsonSerializer.Serialize(llmConfigs), null);

                case "mcpservers":
                    var mcpServers = await _updateService.GetMCPServerConfigsAsync();
                    if (!string.IsNullOrEmpty(configKey) && mcpServers.ContainsKey(configKey))
                    {
                        var singleServer = new Dictionary<string, MCPServerConfig>
                            { { configKey, mcpServers[configKey] } };
                        return (true, JsonSerializer.Serialize(singleServer), null);
                    }

                    return (true, JsonSerializer.Serialize(mcpServers), null);

                default:
                    return (false, string.Empty, $"Unknown configuration type: {configType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration for type: {ConfigType}", configType);
            return (false, string.Empty, ex.Message);
        }
    }

    public Task<List<string>> GetSupportedConfigTypesAsync()
    {
        return Task.FromResult(new List<string>
        {
            "SystemLLMConfigs",
            "MCPServers"
        });
    }

    private async Task<(bool success, string? errorMessage)> UpdateSystemLLMConfigsAsync(string configJson,
        string? configKey)
    {
        try
        {
            if (!string.IsNullOrEmpty(configKey))
            {
                // Update single config
                var config = JsonSerializer.Deserialize<LLMConfig>(configJson);
                if (config != null)
                {
                    var success = await _updateService.UpdateSystemLLMConfigAsync(configKey, config);
                    return (success, success ? null : "Failed to update configuration");
                }
            }
            else
            {
                // Update all configs
                var configs = JsonSerializer.Deserialize<Dictionary<string, LLMConfig>>(configJson);
                if (configs != null)
                {
                    var success = await _updateService.UpdateSystemLLMConfigsAsync(configs);
                    return (success, success ? null : "Failed to update configurations");
                }
            }

            return (false, "Invalid configuration data");
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid JSON format: {ex.Message}");
        }
    }

    private async Task<(bool success, string? errorMessage)> UpdateMCPServersAsync(string configJson,
        string? serverName)
    {
        try
        {
            if (!string.IsNullOrEmpty(serverName))
            {
                // Update single server
                var config = JsonSerializer.Deserialize<MCPServerConfig>(configJson);
                if (config != null)
                {
                    var success = await _updateService.UpdateMCPServerConfigAsync(serverName, config);
                    return (success, success ? null : "Failed to update MCP server configuration");
                }

                return (false, "Invalid MCP server configuration data");
            }

            // Update all servers
            var configs = JsonSerializer.Deserialize<Dictionary<string, MCPServerConfig>>(configJson);
            if (configs != null)
            {
                var success = await _updateService.UpdateMCPServerConfigsAsync(configs);
                return (success, success ? null : "Failed to update MCP server configurations");
            }

            return (false, "Invalid MCP server configuration data");
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid JSON format: {ex.Message}");
        }
    }
}