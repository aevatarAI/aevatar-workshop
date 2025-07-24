using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using Aevatar.GAgents.AI.Options;
using Aevatar.Workshop.Host.Options;

namespace Aevatar.Workshop.Host.Services;

/// <summary>
/// Custom configuration source that supports runtime updates
/// </summary>
public class RuntimeConfigurationSource : IConfigurationSource
{
    public RuntimeConfigurationProvider? Provider { get; set; }
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return Provider ?? new RuntimeConfigurationProvider();
    }
}

/// <summary>
/// Configuration provider that supports runtime updates
/// </summary>
public class RuntimeConfigurationProvider : ConfigurationProvider
{
    private readonly ConcurrentDictionary<string, string> _data = new();
    private ConfigurationReloadToken _reloadToken = new();

    public override void Load()
    {
        // Initial load can be empty, data will be added at runtime
        Data = _data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Update SystemLLMConfig at runtime
    /// </summary>
    public void UpdateSystemLLMConfig(string configKey, LLMConfig config)
    {
        var prefix = $"SystemLLMConfigs:{configKey}";
        
        _data[$"{prefix}:ProviderEnum"] = config.ProviderEnum.ToString();
        _data[$"{prefix}:ModelIdEnum"] = config.ModelIdEnum.ToString();
        _data[$"{prefix}:ModelName"] = config.ModelName;
        _data[$"{prefix}:Endpoint"] = config.Endpoint;
        _data[$"{prefix}:ApiKey"] = config.ApiKey;
            
        // Update the configuration data
        Data = _data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        // Trigger configuration reload
        OnReload();
    }

    /// <summary>
    /// Update all SystemLLMConfigs at runtime
    /// </summary>
    public void UpdateSystemLLMConfigs(Dictionary<string, LLMConfig> configs)
    {
        // Clear existing SystemLLMConfigs
        var keysToRemove = _data.Keys.Where(k => k.StartsWith("SystemLLMConfigs:")).ToList();
        foreach (var key in keysToRemove)
        {
            _data.TryRemove(key, out _);
        }
        
        // Add new configs
        foreach (var kvp in configs)
        {
            UpdateSystemLLMConfig(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Update MCPServerConfig at runtime
    /// </summary>
    public void UpdateMCPServerConfig(string serverName, MCPServerConfig config)
    {
        var prefix = $"MCPServers:{serverName}";
        
        _data[$"{prefix}:Command"] = config.Command;
        
        if (config.Args != null && config.Args.Any())
        {
            for (int i = 0; i < config.Args.Count; i++)
            {
                _data[$"{prefix}:Args:{i}"] = config.Args[i];
            }
        }
        
        if (!string.IsNullOrEmpty(config.Description))
            _data[$"{prefix}:Description"] = config.Description;
            
        _data[$"{prefix}:Enabled"] = config.Enabled.ToString();
        
        // Update the configuration data
        Data = _data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        // Trigger configuration reload
        OnReload();
    }

    /// <summary>
    /// Trigger configuration reload
    /// </summary>
    private void OnReload()
    {
        var previousToken = Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken());
        previousToken.OnReload();
    }
} 