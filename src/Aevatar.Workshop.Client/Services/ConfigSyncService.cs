using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Executor;
using Aevatar.Workshop.GAgent;

namespace Aevatar.Workshop.Client.Services;

/// <summary>
/// Service to sync configuration from client to host on startup using ConfigManagerGAgent
/// </summary>
public class ConfigSyncService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigSyncService> _logger;
    private readonly IGAgentFactory _gAgentFactory;

    public ConfigSyncService(
        IConfiguration configuration,
        ILogger<ConfigSyncService> logger,
        IGAgentFactory gAgentFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _gAgentFactory = gAgentFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting configuration sync to host using ConfigManagerGAgent");

            // Wait a bit for Orleans to be ready
            await Task.Delay(3000, cancellationToken);

            // Get ConfigManagerGAgent
            var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>();
            var executorGAgent = await _gAgentFactory.GetGAgentAsync<IEventHandlerExecutorGAgent>();

            // Sync SystemLLMConfigs
            var llmConfigs = GetConfigurationJson("SystemLLMConfigs");
            if (!string.IsNullOrEmpty(llmConfigs) && llmConfigs != "{}")
            {
                _logger.LogInformation("Syncing SystemLLMConfigs to host");

                var updateEvent = new ConfigUpdateEvent
                {
                    ConfigType = "SystemLLMConfigs",
                    ConfigJson = llmConfigs
                };

                await executorGAgent.ExecuteGAgentEventHandler(configManager, updateEvent);
            }

            // Sync MCPServers
            var mcpServers = GetConfigurationJson("MCPServers");
            if (!string.IsNullOrEmpty(mcpServers) && mcpServers != "{}")
            {
                _logger.LogInformation("Syncing MCPServers to host");

                var updateEvent = new ConfigUpdateEvent
                {
                    ConfigType = "MCPServers",
                    ConfigJson = mcpServers
                };

                await executorGAgent.ExecuteGAgentEventHandler(configManager, updateEvent);
            }

            _logger.LogInformation("Configuration sync completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync configuration to host");
            // Don't throw - allow the application to continue even if config sync fails
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private string GetConfigurationJson(string sectionName)
    {
        var section = _configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            _logger.LogWarning("Configuration section '{Section}' not found", sectionName);
            return "{}";
        }

        // Convert IConfigurationSection to dictionary
        var dict = new Dictionary<string, object>();
        foreach (var child in section.GetChildren())
        {
            if (child.GetChildren().Any())
            {
                // This is a complex object
                var childDict = GetSectionAsDictionary(child);
                dict[child.Key] = ConvertArrayRepresentation(childDict);
            }
            else
            {
                // This is a simple value
                dict[child.Key] = child.Value ?? string.Empty;
            }
        }

        return JsonSerializer.Serialize(dict);
    }

    private object ConvertArrayRepresentation(Dictionary<string, object> dict)
    {
        // Check if this dictionary represents an array
        if (dict.ContainsKey("_isArray") && dict.ContainsKey("_items") && 
            dict["_isArray"] is bool isArray && isArray)
        {
            var items = dict["_items"] as List<object>;
            if (items != null)
            {
                // Convert any nested dictionaries that might also be arrays
                var convertedItems = new List<object>();
                foreach (var item in items)
                {
                    if (item is Dictionary<string, object> itemDict)
                    {
                        convertedItems.Add(ConvertArrayRepresentation(itemDict));
                    }
                    else
                    {
                        convertedItems.Add(item);
                    }
                }
                return convertedItems;
            }
        }

        // Not an array, process nested dictionaries
        var result = new Dictionary<string, object>();
        foreach (var kvp in dict)
        {
            if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                result[kvp.Key] = ConvertArrayRepresentation(nestedDict);
            }
            else
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        return result;
    }

    private Dictionary<string, object> GetSectionAsDictionary(IConfigurationSection section)
    {
        var dict = new Dictionary<string, object>();
        var children = section.GetChildren().ToList();
        
        // Check if this section represents an array
        if (children.Any() && children.All(child => child.Key.All(char.IsDigit)))
        {
            // This is an array - return it as a list
            var array = new List<object>();
            foreach (var child in children.OrderBy(c => int.Parse(c.Key)))
            {
                if (child.GetChildren().Any())
                {
                    array.Add(GetSectionAsDictionary(child));
                }
                else
                {
                    array.Add(child.Value ?? string.Empty);
                }
            }
            // Return a dictionary with the array marked
            dict["_isArray"] = true;
            dict["_items"] = array;
            return dict;
        }

        // Process as regular object
        foreach (var child in children)
        {
            if (child.GetChildren().Any())
            {
                // This is a nested object or array
                dict[child.Key] = GetSectionAsDictionary(child);
            }
            else
            {
                // This is a simple value
                dict[child.Key] = child.Value ?? string.Empty;
            }
        }

        return dict;
    }
}