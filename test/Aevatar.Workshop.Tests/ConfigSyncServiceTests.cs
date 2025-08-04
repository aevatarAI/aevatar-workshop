using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Basic.BasicGEvent;
using Aevatar.Workshop.GAgent;
using Aevatar.Workshop.GAgent.Options;
using Aevatar.Workshop.Client.Services;
using Aevatar.Workshop.GAgent.Extensions;
using Aevatar.Workshop.TestBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public sealed class ConfigSyncServiceTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
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
            ["SystemLLMConfigs:OpenAI:ModelIdEnum"] = "OpenAI",
            ["SystemLLMConfigs:OpenAI:ModelName"] = "gpt-4",
            ["SystemLLMConfigs:OpenAI:ApiKey"] = "test-key-123",
            ["SystemLLMConfigs:OpenAI:NetworkTimeoutInSeconds"] = "100",
            ["SystemLLMConfigs:DeepSeek:ProviderEnum"] = "DeepSeek",
            ["SystemLLMConfigs:DeepSeek:ModelIdEnum"] = "DeepSeek",
            ["SystemLLMConfigs:DeepSeek:ModelName"] = "deepseek-chat",
            ["SystemLLMConfigs:DeepSeek:Endpoint"] = "https://api.deepseek.com",
            ["SystemLLMConfigs:DeepSeek:ApiKey"] = "deepseek-key-456",
            ["SystemLLMConfigs:DeepSeek:NetworkTimeoutInSeconds"] = "100"
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

        response.Success.ShouldBeTrue($"Config sync failed: {response.ErrorMessage}");
        response.ConfigJson.ShouldNotBeEmpty();

        var configs = JsonSerializer.Deserialize<Dictionary<string, LLMConfig>>(response.ConfigJson);
        configs.ShouldNotBeNull();
        configs.Count.ShouldBe(2);
        configs.ContainsKey("OpenAI").ShouldBeTrue();
        configs.ContainsKey("DeepSeek").ShouldBeTrue();

        _testOutputHelper.WriteLine($"Successfully synced {configs.Count} LLM configs");
    }

    [Fact]
    public async Task ConfigSyncService_Should_Sync_MCPServers()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["MCPServers:FileSystemServer:Command"] = "npx",
            ["MCPServers:FileSystemServer:Args:0"] = "-y",
            ["MCPServers:FileSystemServer:Args:1"] = "@modelcontextprotocol/server-filesystem",
            ["MCPServers:FileSystemServer:Args:2"] = "/path/to/files",
            ["MCPServers:FileSystemServer:Description"] = "File system MCP server",
            ["MCPServers:FileSystemServer:Enabled"] = "true",
            ["MCPServers:WebSearchServer:Command"] = "npx",
            ["MCPServers:WebSearchServer:Args:0"] = "-y",
            ["MCPServers:WebSearchServer:Args:1"] = "@modelcontextprotocol/server-brave-search",
            ["MCPServers:WebSearchServer:Env:BRAVE_API_KEY"] = "search-api-key",
            ["MCPServers:WebSearchServer:Description"] = "Web search MCP server",
            ["MCPServers:WebSearchServer:Enabled"] = "true"
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

        response.Success.ShouldBeTrue();
        response.ConfigJson.ShouldNotBeEmpty();

        // Deserialize as Dictionary<string, MCPServerConfig>
        var servers = JsonSerializer.Deserialize<Dictionary<string, MCPServerConfig>>(response.ConfigJson);
        servers.ShouldNotBeNull();
        servers.Count.ShouldBe(2);

        servers.ContainsKey("FileSystemServer").ShouldBeTrue();
        servers.ContainsKey("WebSearchServer").ShouldBeTrue();

        var fileSystemServer = servers["FileSystemServer"];
        fileSystemServer.Command.ShouldBe("npx");
        fileSystemServer.Description.ShouldBe("File system MCP server");
        fileSystemServer.Enabled.ShouldBeTrue();

        var webSearchServer = servers["WebSearchServer"];
        webSearchServer.Command.ShouldBe("npx");
        webSearchServer.Description.ShouldBe("Web search MCP server");
        webSearchServer.Enabled.ShouldBeTrue();
        webSearchServer.Env.ShouldNotBeNull();
        webSearchServer.Env.ContainsKey("BRAVE_API_KEY").ShouldBeTrue();

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
}