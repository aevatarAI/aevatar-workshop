using Aevatar.GAgents.AI.Options;
using Aevatar.Workshop.Host.Options;

namespace Aevatar.Workshop.Host.Services;

/// <summary>
/// Service for updating configurations at runtime
/// </summary>
public interface IConfigurationUpdateService
{
    /// <summary>
    /// Update SystemLLMConfigOptions
    /// </summary>
    Task<bool> UpdateSystemLLMConfigAsync(string configKey, LLMConfig config);
    
    /// <summary>
    /// Update the entire SystemLLMConfigOptions
    /// </summary>
    Task<bool> UpdateSystemLLMConfigsAsync(Dictionary<string, LLMConfig> configs);
    
    /// <summary>
    /// Get current SystemLLMConfigOptions
    /// </summary>
    Task<Dictionary<string, LLMConfig>> GetSystemLLMConfigsAsync();
    
    /// <summary>
    /// Update MCPServerOptions
    /// </summary>
    Task<bool> UpdateMCPServerConfigAsync(string serverName, MCPServerConfig config);
    
    /// <summary>
    /// Update the entire MCPServerOptions
    /// </summary>
    Task<bool> UpdateMCPServerConfigsAsync(Dictionary<string, MCPServerConfig> configs);
    
    /// <summary>
    /// Get current MCPServerOptions
    /// </summary>
    Task<Dictionary<string, MCPServerConfig>> GetMCPServerConfigsAsync();
} 