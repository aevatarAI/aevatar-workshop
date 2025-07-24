using Aevatar.GAgents.AI.Options;
using Aevatar.Workshop.Host.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Aevatar.Workshop.Host.Services;

/// <summary>
/// Implementation of configuration update service using runtime configuration provider
/// </summary>
public class ConfigurationUpdateService : IConfigurationUpdateService
{
    private readonly IOptionsMonitor<SystemLLMConfigOptions> _systemLLMOptions;
    private readonly IOptionsMonitor<MCPServerOptions> _mcpServerOptions;
    private readonly RuntimeConfigurationProvider _runtimeConfigProvider;
    private readonly ILogger<ConfigurationUpdateService> _logger;
    
    public ConfigurationUpdateService(
        IOptionsMonitor<SystemLLMConfigOptions> systemLLMOptions,
        IOptionsMonitor<MCPServerOptions> mcpServerOptions,
        RuntimeConfigurationProvider runtimeConfigProvider,
        ILogger<ConfigurationUpdateService> logger)
    {
        _systemLLMOptions = systemLLMOptions;
        _mcpServerOptions = mcpServerOptions;
        _runtimeConfigProvider = runtimeConfigProvider;
        _logger = logger;
    }

    public Task<bool> UpdateSystemLLMConfigAsync(string configKey, LLMConfig config)
    {
        try
        {
            // Update the runtime configuration provider
            _runtimeConfigProvider.UpdateSystemLLMConfig(configKey, config);
            
            _logger.LogInformation("Updated SystemLLMConfig for key: {Key}", configKey);
            
            // The configuration change will automatically trigger IOptionsMonitor to reload
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update SystemLLMConfig for key: {Key}", configKey);
            return Task.FromResult(false);
        }
    }

    public Task<bool> UpdateSystemLLMConfigsAsync(Dictionary<string, LLMConfig> configs)
    {
        try
        {
            // Update all configs in the runtime configuration provider
            _runtimeConfigProvider.UpdateSystemLLMConfigs(configs);
            
            _logger.LogInformation("Updated all SystemLLMConfigs, total count: {Count}", configs.Count);
            
            // The configuration change will automatically trigger IOptionsMonitor to reload
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update SystemLLMConfigs");
            return Task.FromResult(false);
        }
    }

    public Task<Dictionary<string, LLMConfig>> GetSystemLLMConfigsAsync()
    {
        // Get current value from IOptionsMonitor which includes runtime updates
        var currentConfigs = _systemLLMOptions.CurrentValue.SystemLLMConfigs ?? new Dictionary<string, LLMConfig>();
        return Task.FromResult(new Dictionary<string, LLMConfig>(currentConfigs));
    }

    public Task<bool> UpdateMCPServerConfigAsync(string serverName, MCPServerConfig config)
    {
        try
        {
            // Update the runtime configuration provider
            _runtimeConfigProvider.UpdateMCPServerConfig(serverName, config);
            
            _logger.LogInformation("Updated MCPServerConfig for server: {ServerName}", serverName);
            
            // The configuration change will automatically trigger IOptionsMonitor to reload
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update MCPServerConfig for server: {ServerName}", serverName);
            return Task.FromResult(false);
        }
    }

    public Task<bool> UpdateMCPServerConfigsAsync(Dictionary<string, MCPServerConfig> configs)
    {
        try
        {
            // Update all MCP server configs
            foreach (var kvp in configs)
            {
                _runtimeConfigProvider.UpdateMCPServerConfig(kvp.Key, kvp.Value);
            }
            
            _logger.LogInformation("Updated all MCPServerConfigs, total count: {Count}", configs.Count);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update MCPServerConfigs");
            return Task.FromResult(false);
        }
    }

    public Task<Dictionary<string, MCPServerConfig>> GetMCPServerConfigsAsync()
    {
        // Get current value from IOptionsMonitor which includes runtime updates
        var currentConfigs = _mcpServerOptions.CurrentValue.MCPServers ?? new Dictionary<string, MCPServerConfig>();
        return Task.FromResult(new Dictionary<string, MCPServerConfig>(currentConfigs));
    }
} 