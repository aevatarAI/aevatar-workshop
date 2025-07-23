namespace Aevatar.Workshop.GAgent;

/// <summary>
/// Interface for handling configuration updates
/// This will be implemented in the Host project to avoid circular dependencies
/// </summary>
public interface IConfigurationHandler
{
    /// <summary>
    /// Update configuration based on type
    /// </summary>
    Task<(bool success, string? errorMessage)> UpdateConfigurationAsync(string configType, string configJson,
        string? configKey = null);

    /// <summary>
    /// Get configuration based on type
    /// </summary>
    Task<(bool success, string configJson, string? errorMessage)> GetConfigurationAsync(string configType,
        string? configKey = null);

    /// <summary>
    /// Get supported configuration types
    /// </summary>
    Task<List<string>> GetSupportedConfigTypesAsync();
}