using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.GAgents.Executor;
using Aevatar.Workshop.GAgent;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Basic.BasicGEvent;
using Aevatar.Workshop.GAgent.Extensions;
using Aevatar.Workshop.GAgent.Options;

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
        _logger.LogInformation("=== ConfigSyncService Starting ===");

        try
        {
            await SyncSystemLLMConfigsAsync();
            await SyncMCPServersAsync();

            _logger.LogInformation("=== ConfigSyncService Completed Successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration sync");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task SyncSystemLLMConfigsAsync()
    {
        try
        {
            _logger.LogInformation("Starting SystemLLMConfigs sync");

            // Read SystemLLMConfigs from configuration
            var systemLLMConfigs = new SystemLLMConfigOptions();
            var systemLLMConfigsSection = _configuration.GetSection("SystemLLMConfigs");
            systemLLMConfigs.SystemLLMConfigs = systemLLMConfigsSection.Get<Dictionary<string, LLMConfig>>() ??
                                                new Dictionary<string, LLMConfig>();

            if (systemLLMConfigs.SystemLLMConfigs != null && systemLLMConfigs.SystemLLMConfigs.Any())
            {
                _logger.LogInformation("Found {Count} SystemLLMConfigs to sync: {Keys}",
                    systemLLMConfigs.SystemLLMConfigs.Count,
                    string.Join(", ", systemLLMConfigs.SystemLLMConfigs.Keys));

                // Get the ConfigManagerGAgent instance for SystemLLMConfigOptions
                var configManager = await _gAgentFactory.GetSystemLLMConfigGAgent();

                // Create update event - serialize the entire dictionary
                var updateEvent = new ConfigUpdateEvent
                {
                    ConfigType = typeof(SystemLLMConfigOptions).FullName!,
                    ConfigJson = JsonSerializer.Serialize(systemLLMConfigs.SystemLLMConfigs)
                };

                var response = await configManager.UpdateConfigAsync(updateEvent);

                if (response.Success)
                {
                    _logger.LogInformation("Successfully synced SystemLLMConfigs to ConfigManagerGAgent");
                }
                else
                {
                    _logger.LogError("Failed to sync SystemLLMConfigs: {ResponseErrorMessage}", response.ErrorMessage);
                }
            }
            else
            {
                _logger.LogWarning("No SystemLLMConfigs found in configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing SystemLLMConfigs");
            throw;
        }
    }

    private async Task SyncMCPServersAsync()
    {
        try
        {
            _logger.LogInformation("Starting MCPServers sync");

            // Read MCPServers from configuration
            var mcpServerOptions = new MCPServerOptions();
            var mcpServersSection = _configuration.GetSection("MCPServers");
            mcpServerOptions.MCPServers = mcpServersSection.Get<Dictionary<string, MCPServerConfig>>() ??
                                          new Dictionary<string, MCPServerConfig>();

            if (mcpServerOptions.MCPServers != null && mcpServerOptions.MCPServers.Any())
            {
                _logger.LogInformation("Found {Count} MCPServers to sync", mcpServerOptions.MCPServers.Count);

                // Get the ConfigManagerGAgent instance for MCPServerOptions
                var configManager = await _gAgentFactory.GetMCPServerConfigGAgent();

                // Create update event - serialize the entire dictionary
                var updateEvent = new ConfigUpdateEvent
                {
                    ConfigType = typeof(MCPServerOptions).FullName!,
                    ConfigJson = JsonSerializer.Serialize(mcpServerOptions.MCPServers)
                };

                // Update configuration
                var response = await configManager.UpdateConfigAsync(updateEvent);

                if (response.Success)
                {
                    _logger.LogInformation("Successfully synced MCPServers to ConfigManagerGAgent");
                }
                else
                {
                    _logger.LogError("Failed to sync MCPServers: {ErrorMessage}", response.ErrorMessage);
                }
            }
            else
            {
                _logger.LogWarning("No MCPServers found in configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing MCPServers");
            throw;
        }
    }
}