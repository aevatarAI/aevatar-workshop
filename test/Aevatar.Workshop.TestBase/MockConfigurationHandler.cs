using System.Collections.Concurrent;
using System.Text.Json;
using Aevatar.Workshop.GAgent;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.Tests;

/// <summary>
/// Mock implementation of IConfigurationHandler for testing
/// </summary>
public class MockConfigurationHandler : IConfigurationHandler
{
    private readonly ILogger<MockConfigurationHandler> _logger;
    private readonly ConcurrentDictionary<string, string> _configurations = new();
    
    // Static instance for testing to ensure we use the same instance
    public static MockConfigurationHandler? CurrentInstance { get; private set; }
    
    // Global static counters to track all calls across all instances
    public static int GlobalUpdateCallCount { get; private set; }
    public static int GlobalGetCallCount { get; private set; }
    public static readonly List<(string configType, string? configKey)> GlobalUpdateCalls = new();
    public static readonly List<(string configType, string? configKey)> GlobalGetCalls = new();

    // Track method calls for verification
    public int UpdateCallCount { get; private set; }
    public int GetCallCount { get; private set; }
    public List<(string configType, string? configKey)> UpdateCalls { get; } = new();
    public List<(string configType, string? configKey)> GetCalls { get; } = new();

    public MockConfigurationHandler(ILogger<MockConfigurationHandler> logger)
    {
        _logger = logger;
        CurrentInstance = this; // Set static instance
        
        // Log to console for debugging
        Console.WriteLine($"[MockConfigurationHandler] Instance created with HashCode: {this.GetHashCode()}");
        _logger.LogInformation("MockConfigurationHandler instance created with HashCode: {HashCode}", this.GetHashCode());

        // Initialize with some default configurations
        _configurations["SystemLLMConfigs"] = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["DefaultLLM"] = new
            {
                ProviderEnum = "OpenAI",
                ModelIdEnum = "OpenAI",
                ModelName = "gpt-3.5-turbo",
                Endpoint = "https://api.openai.com",
                ApiKey = "mock-api-key"
            }
        });

        _configurations["MCPServers"] = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["filesystem"] = new
            {
                command = "npx",
                args = new[] { "-y", "@modelcontextprotocol/server-filesystem" },
                description = "File system access",
                enabled = true
            }
        });
    }
    
    public static void ResetGlobalCounters()
    {
        GlobalUpdateCallCount = 0;
        GlobalGetCallCount = 0;
        GlobalUpdateCalls.Clear();
        GlobalGetCalls.Clear();
    }

    public Task<(bool success, string? errorMessage)> UpdateConfigurationAsync(
        string configType, 
        string configJson, 
        string? configKey = null)
    {
        UpdateCallCount++;
        UpdateCalls.Add((configType, configKey));
        
        // Update global counters
        GlobalUpdateCallCount++;
        GlobalUpdateCalls.Add((configType, configKey));
        
        _logger.LogInformation("Mock updating configuration: Type={Type}, Key={Key}", configType, configKey);
        _logger.LogInformation("MockConfigurationHandler HashCode: {HashCode}, UpdateCallCount: {Count}, GlobalUpdateCallCount: {GlobalCount}", 
            this.GetHashCode(), UpdateCallCount, GlobalUpdateCallCount);
        
        try
        {
            // Validate JSON
            JsonDocument.Parse(configJson);
            
            // Store configuration
            var key = string.IsNullOrEmpty(configKey) ? configType : $"{configType}:{configKey}";
            _configurations[key] = configJson;
            
            _logger.LogInformation("Configuration updated successfully");
            return Task.FromResult((true, (string?)null));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in configuration update");
            return Task.FromResult((false, $"Invalid JSON: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            return Task.FromResult((false, ex.Message));
        }
    }

    public Task<(bool success, string configJson, string? errorMessage)> GetConfigurationAsync(
        string configType, 
        string? configKey = null)
    {
        GetCallCount++;
        GetCalls.Add((configType, configKey));
        
        // Update global counters
        GlobalGetCallCount++;
        GlobalGetCalls.Add((configType, configKey));
        
        _logger.LogInformation("Mock getting configuration: Type={Type}, Key={Key}", configType, configKey);
        _logger.LogInformation("MockConfigurationHandler HashCode: {HashCode}, GetCallCount: {Count}, GlobalGetCallCount: {GlobalCount}", 
            this.GetHashCode(), GetCallCount, GlobalGetCallCount);
        
        try
        {
            var key = string.IsNullOrEmpty(configKey) ? configType : $"{configType}:{configKey}";
            
            if (_configurations.TryGetValue(key, out var configJson))
            {
                _logger.LogInformation("Configuration retrieved successfully");
                return Task.FromResult((true, configJson, (string?)null));
            }
            else
            {
                _logger.LogWarning("Configuration not found for type: {Type}", configType);
                return Task.FromResult((false, string.Empty, $"Configuration not found for type: {configType}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration");
            return Task.FromResult((false, string.Empty, ex.Message));
        }
    }

    public Task<List<string>> GetSupportedConfigTypesAsync()
    {
        return Task.FromResult(new List<string>
        {
            "SystemLLMConfigs",
            "MCPServers",
            "TestConfig"
        });
    }

    // Helper method to get stored configuration for testing
    public string? GetStoredConfiguration(string configType, string? configKey = null)
    {
        var key = string.IsNullOrEmpty(configKey) ? configType : $"{configType}:{configKey}";
        return _configurations.TryGetValue(key, out var value) ? value : null;
    }
}