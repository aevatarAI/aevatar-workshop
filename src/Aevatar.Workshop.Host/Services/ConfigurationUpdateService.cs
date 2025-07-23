using Aevatar.GAgents.AI.Options;
using Aevatar.Workshop.Host.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Aevatar.Workshop.Host.Services;

/// <summary>
/// Implementation of configuration update service using IOptionsMonitor
/// </summary>
public class ConfigurationUpdateService : IConfigurationUpdateService
{
    private readonly IOptionsMonitor<SystemLLMConfigOptions> _systemLLMOptions;
    private readonly IOptionsMonitor<MCPServerOptions> _mcpServerOptions;
    private readonly ILogger<ConfigurationUpdateService> _logger;
    
    // In-memory storage for runtime updates (in production, you might want to persist these)
    private readonly Dictionary<string, LLMConfig> _runtimeLLMConfigs = new();
    private readonly Dictionary<string, MCPServerConfig> _runtimeMCPConfigs = new();
    private readonly object _lockObject = new();

    public ConfigurationUpdateService(
        IOptionsMonitor<SystemLLMConfigOptions> systemLLMOptions,
        IOptionsMonitor<MCPServerOptions> mcpServerOptions,
        ILogger<ConfigurationUpdateService> logger)
    {
        _systemLLMOptions = systemLLMOptions;
        _mcpServerOptions = mcpServerOptions;
        _logger = logger;
        
        // Initialize runtime configs from current options
        lock (_lockObject)
        {
            if (_systemLLMOptions.CurrentValue.SystemLLMConfigs != null)
            {
                foreach (var kvp in _systemLLMOptions.CurrentValue.SystemLLMConfigs)
                {
                    _runtimeLLMConfigs[kvp.Key] = kvp.Value;
                }
            }
            
            if (_mcpServerOptions.CurrentValue.MCPServers != null)
            {
                foreach (var kvp in _mcpServerOptions.CurrentValue.MCPServers)
                {
                    _runtimeMCPConfigs[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    public Task<bool> UpdateSystemLLMConfigAsync(string configKey, LLMConfig config)
    {
        try
        {
            lock (_lockObject)
            {
                _runtimeLLMConfigs[configKey] = config;
            }
            
            _logger.LogInformation("Updated SystemLLMConfig for key: {Key}", configKey);
            
            // Note: In a real implementation, you might want to:
            // 1. Save to appsettings.json or database
            // 2. Notify dependent services about the change
            // 3. Use IOptionsMonitor.OnChange to react to changes
            
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
            lock (_lockObject)
            {
                _runtimeLLMConfigs.Clear();
                foreach (var kvp in configs)
                {
                    _runtimeLLMConfigs[kvp.Key] = kvp.Value;
                }
            }
            
            _logger.LogInformation("Updated all SystemLLMConfigs, total count: {Count}", configs.Count);
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
        lock (_lockObject)
        {
            return Task.FromResult(new Dictionary<string, LLMConfig>(_runtimeLLMConfigs));
        }
    }

    public Task<bool> UpdateMCPServerConfigAsync(string serverName, MCPServerConfig config)
    {
        try
        {
            lock (_lockObject)
            {
                _runtimeMCPConfigs[serverName] = config;
            }
            
            _logger.LogInformation("Updated MCPServerConfig for server: {ServerName}", serverName);
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
            lock (_lockObject)
            {
                _runtimeMCPConfigs.Clear();
                foreach (var kvp in configs)
                {
                    _runtimeMCPConfigs[kvp.Key] = kvp.Value;
                }
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
        lock (_lockObject)
        {
            return Task.FromResult(new Dictionary<string, MCPServerConfig>(_runtimeMCPConfigs));
        }
    }
} 