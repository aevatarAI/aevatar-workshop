using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent;

/// <summary>
/// State for ConfigManagerGAgent
/// </summary>
[GenerateSerializer]
public class ConfigManagerGAgentState : StateBase
{
    [Id(0)] public DateTime LastUpdated { get; set; }
    [Id(1)] public Dictionary<string, DateTime> ConfigUpdateTimes { get; set; } = new();
    [Id(2)] public int TotalUpdates { get; set; }
}

/// <summary>
/// State log events for ConfigManagerGAgent
/// </summary>
[GenerateSerializer]
public class ConfigManagerStateLogEvent : StateLogEventBase<ConfigManagerStateLogEvent>;

[GenerateSerializer]
public class ConfigUpdatedLogEvent : ConfigManagerStateLogEvent
{
    [Id(0)] public string ConfigType { get; set; } = string.Empty;
    [Id(1)] public bool Success { get; set; }
    [Id(2)] public string? ErrorMessage { get; set; }
    [Id(3)] public DateTime Timestamp { get; set; }
}

public interface IConfigManagerGAgent : IStateGAgent<ConfigManagerGAgentState>
{
    Task<List<string>> GetSupportedConfigTypesAsync();
}

/// <summary>
/// GAgent responsible for managing configuration updates
/// This GAgent uses IConfigurationHandler which is implemented in the Host project
/// </summary>
[GAgent("config", "aevatar")]
public class ConfigManagerGAgent : GAgentBase<ConfigManagerGAgentState, ConfigManagerStateLogEvent>,
    IConfigManagerGAgent
{
    private IConfigurationHandler? _configHandler;

    protected IConfigurationHandler ConfigHandler
    {
        get
        {
            if (_configHandler == null)
            {
                // Try to get the handler from DI
                _configHandler = ServiceProvider.GetService<IConfigurationHandler>();
                if (_configHandler == null)
                {
                    throw new InvalidOperationException(
                        "IConfigurationHandler is not registered. Make sure it's registered in the Host module.");
                }
            }

            return _configHandler;
        }
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Configuration manager GAgent for updating runtime configurations");
    }

    /// <summary>
    /// Handle configuration update events
    /// </summary>
    [EventHandler]
    public async Task<ConfigResponseEvent> HandleEventAsync(ConfigUpdateEvent updateEvent)
    {
        Logger.LogInformation("Received configuration update request for type: {ConfigType}", updateEvent.ConfigType);
        
        // Debug log
        Logger.LogInformation("ConfigHandler instance: {HandlerType}, HashCode: {HashCode}", 
            ConfigHandler.GetType().Name, ConfigHandler.GetHashCode());

        try
        {
            var (success, errorMessage) = await ConfigHandler.UpdateConfigurationAsync(
                updateEvent.ConfigType,
                updateEvent.ConfigJson,
                updateEvent.ConfigKey);

            Logger.LogInformation("UpdateConfigurationAsync returned: Success={Success}, Error={Error}", 
                success, errorMessage);

            // Log the update
            RaiseEvent(new ConfigUpdatedLogEvent
            {
                ConfigType = updateEvent.ConfigType,
                Success = success,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            });

            await ConfirmEvents();

            // Send response
            var response = new ConfigResponseEvent
            {
                ConfigType = updateEvent.ConfigType,
                ConfigJson = updateEvent.ConfigJson,
                Success = success,
                ErrorMessage = errorMessage
            };

            Logger.LogInformation("Configuration update completed. Type: {Type}, Success: {Success}",
                updateEvent.ConfigType, success);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating configuration for type: {ConfigType}", updateEvent.ConfigType);

            RaiseEvent(new ConfigUpdatedLogEvent
            {
                ConfigType = updateEvent.ConfigType,
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            });

            await ConfirmEvents();

            // Send error response
            var response = new ConfigResponseEvent
            {
                ConfigType = updateEvent.ConfigType,
                ConfigJson = string.Empty,
                Success = false,
                ErrorMessage = ex.Message
            };

            return response;
        }
    }

    /// <summary>
    /// Handle configuration get events
    /// </summary>
    [EventHandler]
    public async Task<ConfigResponseEvent> HandleEventAsync(ConfigRequestEvent configRequest)
    {
        Logger.LogInformation("Received configuration get request for type: {ConfigType}", configRequest.ConfigType);

        try
        {
            var (success, configJson, errorMessage) = await ConfigHandler.GetConfigurationAsync(
                configRequest.ConfigType,
                configRequest.ConfigKey);

            var response = new ConfigResponseEvent
            {
                ConfigType = configRequest.ConfigType,
                ConfigJson = configJson,
                Success = success,
                ErrorMessage = errorMessage
            };

            Logger.LogInformation("Configuration get completed. Type: {Type}, Success: {Success}",
                configRequest.ConfigType, success);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting configuration for type: {ConfigType}", configRequest.ConfigType);

            var response = new ConfigResponseEvent
            {
                ConfigType = configRequest.ConfigType,
                ConfigJson = string.Empty,
                Success = false,
                ErrorMessage = ex.Message
            };

            return response;
        }
    }

    /// <summary>
    /// Get supported configuration types
    /// </summary>
    public async Task<List<string>> GetSupportedConfigTypesAsync()
    {
        try
        {
            return await ConfigHandler.GetSupportedConfigTypesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting supported configuration types");
            return [];
        }
    }

    protected override void GAgentTransitionState(ConfigManagerGAgentState state,
        StateLogEventBase<ConfigManagerStateLogEvent> @event)
    {
        switch (@event)
        {
            case ConfigUpdatedLogEvent updatedEvent:
                state.LastUpdated = updatedEvent.Timestamp;
                state.ConfigUpdateTimes[updatedEvent.ConfigType] = updatedEvent.Timestamp;
                if (updatedEvent.Success)
                {
                    state.TotalUpdates++;
                }

                break;
        }
    }
}