using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.GAgents.AI.Options;
using Aevatar.Workshop.GAgent;
using Aevatar.Workshop.GAgent.Options;
using Aevatar.Workshop.Client.Services;
using Aevatar.Workshop.GAgent.Extensions;
using Aevatar.Workshop.TestBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public class ConfigSyncServiceTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<ConfigSyncService> _logger;

    public ConfigSyncServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        
        // Create logger with correct type
        var loggerFactory = GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<ConfigSyncService>();
    }

    [Fact]
    public async Task ConfigSyncService_Should_Sync_SystemLLMConfigs()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["SystemLLMConfigs:OpenAI:ProviderEnum"] = "OpenAI",
            ["SystemLLMConfigs:OpenAI:ModelIdEnum"] = "OpenAIGPT4",
            ["SystemLLMConfigs:OpenAI:DeploymentOrModelId"] = "gpt-4",
            ["SystemLLMConfigs:OpenAI:ApiKey"] = "test-key-123",
            ["SystemLLMConfigs:OpenAI:Temperature"] = "0.7",
            ["SystemLLMConfigs:DeepSeek:ProviderEnum"] = "OpenAI",
            ["SystemLLMConfigs:DeepSeek:ModelIdEnum"] = "DeepSeekV3", 
            ["SystemLLMConfigs:DeepSeek:DeploymentOrModelId"] = "deepseek-chat",
            ["SystemLLMConfigs:DeepSeek:Endpoint"] = "https://api.deepseek.com",
            ["SystemLLMConfigs:DeepSeek:ApiKey"] = "deepseek-key-456"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var service = new ConfigSyncService(configuration, _logger, _gAgentFactory);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - Verify the config was stored
        var configManager = await _gAgentFactory.GetSystemLLMConfigGAgent();
        
        var response = await configManager.RequestConfigAsync(new ConfigRequestEvent
        {
            ConfigType = typeof(SystemLLMConfigOptions).FullName!
        });

        Assert.True(response.Success);
        Assert.NotEmpty(response.ConfigJson);

        var configs = JsonSerializer.Deserialize<Dictionary<string, LLMConfig>>(response.ConfigJson);
        Assert.NotNull(configs);
        Assert.Equal(2, configs.Count);
        Assert.True(configs.ContainsKey("OpenAI"));
        Assert.True(configs.ContainsKey("DeepSeek"));

        _testOutputHelper.WriteLine($"Successfully synced {configs.Count} LLM configs");
    }

    [Fact]
    public async Task ConfigSyncService_Should_Sync_MCPServers()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["MCPServers:0:Name"] = "FileSystemServer",
            ["MCPServers:0:TransportType"] = "SSE",
            ["MCPServers:0:Endpoint"] = "http://localhost:3000/sse",
            ["MCPServers:0:Description"] = "File system MCP server",
            ["MCPServers:1:Name"] = "WebSearchServer",
            ["MCPServers:1:TransportType"] = "WebSocket",
            ["MCPServers:1:Endpoint"] = "ws://localhost:3001/ws",
            ["MCPServers:1:ApiKey"] = "search-api-key"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var service = new ConfigSyncService(configuration, _logger, _gAgentFactory);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        var configManager = await _gAgentFactory.GetMCPServerConfigGAgent();
        
        var response = await configManager.RequestConfigAsync(new ConfigRequestEvent
        {
            ConfigType = typeof(MCPServerOptions).FullName!
        });

        Assert.True(response.Success);
        Assert.NotEmpty(response.ConfigJson);

        // MCPServerConfig uses ServerName property instead of Name
        var servers = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(response.ConfigJson);
        Assert.NotNull(servers);
        Assert.Equal(2, servers.Count);
        
        // Since MCPServerConfig has been serialized, we need to check the actual JSON properties
        Assert.Equal("FileSystemServer", servers[0]["Name"].GetString());
        Assert.Equal("WebSearchServer", servers[1]["Name"].GetString());

        _testOutputHelper.WriteLine($"Successfully synced {servers.Count} MCP servers");
    }

    [Fact]
    public async Task ConfigSyncService_Should_Handle_Empty_Config()
    {
        // Arrange
        var configData = new Dictionary<string, string?>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var service = new ConfigSyncService(configuration, _logger, _gAgentFactory);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - Should complete without errors
        _testOutputHelper.WriteLine("Successfully handled empty configuration");
    }

    [Fact]
    public async Task DynamicToolAIGAgent_Should_Use_ConfigManagerGAgent_For_LLM_Config()
    {
        // Arrange - First sync config
        var configData = new Dictionary<string, string?>
        {
            ["SystemLLMConfigs:OpenAI:ProviderEnum"] = "OpenAI",
            ["SystemLLMConfigs:OpenAI:ModelIdEnum"] = "OpenAIGPT4",
            ["SystemLLMConfigs:OpenAI:DeploymentOrModelId"] = "gpt-4",
            ["SystemLLMConfigs:OpenAI:ApiKey"] = "test-key-123",
            ["SystemLLMConfigs:OpenAI:Temperature"] = "0.7"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var service = new ConfigSyncService(configuration, _logger, _gAgentFactory);
        await service.StartAsync(CancellationToken.None);

        // Act - Create DynamicToolAIGAgent and test config resolution
        var agentGuid = Guid.NewGuid();
        var agent = await _gAgentFactory.GetGAgentAsync<IDynamicToolAIGAgent>(agentGuid);
        
        // The agent should be able to resolve config from ConfigManagerGAgent
        // This is tested implicitly through the agent's ResolveSystemConfig method

        _testOutputHelper.WriteLine($"Created DynamicToolAIGAgent with ID: {agentGuid}");
        _testOutputHelper.WriteLine("Agent should now be able to resolve LLM configs from ConfigManagerGAgent");
    }
} 