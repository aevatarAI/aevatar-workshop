using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Executor;
using Aevatar.Workshop.GAgent;
using Aevatar.Workshop.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public class ConfigSyncIntegrationTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentExecutor _gAgentExecutor;

    public ConfigSyncIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _gAgentExecutor = GetRequiredService<IGAgentExecutor>();
    }

    [Fact]
    public async Task Should_Sync_SystemLLMConfigs_From_Client_To_Host()
    {
        // Arrange
        MockConfigurationHandler.ResetGlobalCounters(); // Reset global counters
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>();

        _testOutputHelper.WriteLine($"Initial GlobalUpdateCallCount: {MockConfigurationHandler.GlobalUpdateCallCount}");

        var llmConfigs = new Dictionary<string, LLMConfig>
        {
            ["TestOpenAI"] = new()
            {
                ProviderEnum = LLMProviderEnum.OpenAI,
                ModelIdEnum = ModelIdEnum.OpenAI,
                ModelName = "gpt-4",
                Endpoint = "https://api.openai.com",
                ApiKey = "test-key-123"
            },
            ["TestDeepSeek"] = new()
            {
                ProviderEnum = LLMProviderEnum.DeepSeek,
                ModelIdEnum = ModelIdEnum.DeepSeek,
                ModelName = "deepseek-reasoner",
                Endpoint = "https://api.deepseek.com",
                ApiKey = "test-key-456"
            }
        };

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "SystemLLMConfigs",
            ConfigJson = JsonSerializer.Serialize(llmConfigs)
        };

        // Act
        _testOutputHelper.WriteLine("Sending configuration update event...");

        try
        {
            await _gAgentExecutor.ExecuteGAgentEventHandler(configManager, updateEvent);
            _testOutputHelper.WriteLine("Event handler executed successfully");
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"Event handler execution failed: {ex.Message}");
            _testOutputHelper.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        // Allow time for event processing
        await Task.Delay(1000);

        // Debug: Check if GlobalUpdateCallCount changed
        _testOutputHelper.WriteLine(
            $"After execution GlobalUpdateCallCount: {MockConfigurationHandler.GlobalUpdateCallCount}");

        // Assert using global counters
        Assert.Equal(1, MockConfigurationHandler.GlobalUpdateCallCount);
        Assert.Contains(("SystemLLMConfigs", null), MockConfigurationHandler.GlobalUpdateCalls);

        // If we have a current instance, verify it too
        if (MockConfigurationHandler.CurrentInstance != null)
        {
            var storedConfig = MockConfigurationHandler.CurrentInstance.GetStoredConfiguration("SystemLLMConfigs");
            Assert.NotNull(storedConfig);

            var deserializedConfig = JsonSerializer.Deserialize<Dictionary<string, LLMConfig>>(storedConfig);
            Assert.NotNull(deserializedConfig);
            Assert.Equal(2, deserializedConfig.Count);
            Assert.True(deserializedConfig.ContainsKey("TestOpenAI"));
            Assert.True(deserializedConfig.ContainsKey("TestDeepSeek"));

            _testOutputHelper.WriteLine(
                $"Configuration successfully synced. GlobalUpdateCallCount: {MockConfigurationHandler.GlobalUpdateCallCount}");
            _testOutputHelper.WriteLine($"Stored config: {storedConfig}");
        }
        else
        {
            _testOutputHelper.WriteLine(
                "Warning: MockConfigurationHandler.CurrentInstance is null, but global counter shows updates occurred");
        }
    }

    [Fact]
    public async Task Should_Sync_MCPServers_From_Client_To_Host()
    {
        // Arrange
        MockConfigurationHandler.ResetGlobalCounters(); // Reset global counters
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>();

        var mcpServers = new Dictionary<string, TestMCPServerConfig>
        {
            ["test-server"] = new()
            {
                Command = "npx",
                Args = ["-y", "@test/mcp-server"],
                Description = "Test MCP Server",
                Enabled = true,
                InitialDelayMs = 1000,
                MaxRetries = 3
            }
        };

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "MCPServers",
            ConfigJson = JsonSerializer.Serialize(mcpServers)
        };

        // Act
        _testOutputHelper.WriteLine("Sending MCP servers configuration update...");
        await _gAgentExecutor.ExecuteGAgentEventHandler(configManager, updateEvent);

        // Allow time for event processing
        await Task.Delay(1000);

        // Assert using global counters
        Assert.Equal(1, MockConfigurationHandler.GlobalUpdateCallCount);
        Assert.Contains(("MCPServers", null), MockConfigurationHandler.GlobalUpdateCalls);

        _testOutputHelper.WriteLine(
            $"MCP servers configuration synced. GlobalUpdateCallCount: {MockConfigurationHandler.GlobalUpdateCallCount}");
    }

    [Fact]
    public async Task Should_Get_Configuration_From_Host()
    {
        // Arrange
        MockConfigurationHandler.ResetGlobalCounters(); // Reset global counters
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>();

        var getEvent = new ConfigRequestEvent
        {
            ConfigType = "SystemLLMConfigs"
        };

        // Act
        _testOutputHelper.WriteLine("Requesting configuration from host...");

        // Execute the event handler directly
        await _gAgentExecutor.ExecuteGAgentEventHandler(configManager, getEvent);

        // Allow time for processing
        await Task.Delay(1000);

        // Assert using global counters
        Assert.Equal(1, MockConfigurationHandler.GlobalGetCallCount);
        Assert.Contains(("SystemLLMConfigs", null), MockConfigurationHandler.GlobalGetCalls);

        _testOutputHelper.WriteLine("Configuration get operation verified");
        _testOutputHelper.WriteLine($"GlobalGetCallCount: {MockConfigurationHandler.GlobalGetCallCount}");
    }

    [Fact]
    public async Task Should_Handle_Invalid_Configuration_Gracefully()
    {
        // Arrange
        MockConfigurationHandler.ResetGlobalCounters(); // Reset global counters
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>();

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "SystemLLMConfigs",
            ConfigJson = "{ invalid json }" // Invalid JSON
        };

        // Act
        _testOutputHelper.WriteLine("Sending invalid configuration update...");

        // Execute the event handler directly
        await _gAgentExecutor.ExecuteGAgentEventHandler(configManager, updateEvent);

        // Allow time for processing
        await Task.Delay(1000);

        // Assert using global counters - The handler should have been called
        Assert.Equal(1, MockConfigurationHandler.GlobalUpdateCallCount);

        _testOutputHelper.WriteLine(
            $"Error handling verified - GlobalUpdateCallCount: {MockConfigurationHandler.GlobalUpdateCallCount}");
    }

    [Fact]
    public async Task Should_Track_Configuration_Updates_In_GAgent_State()
    {
        // Arrange
        var configManager = await _gAgentFactory.GetGAgentAsync<IConfigManagerGAgent>();

        // Get initial state
        var initialState = await configManager.GetStateAsync();
        var initialUpdateCount = initialState.TotalUpdates;

        var testConfig = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "TestConfig",
            ConfigJson = JsonSerializer.Serialize(testConfig)
        };

        // Act
        _testOutputHelper.WriteLine("Updating configuration and tracking state changes...");
        await _gAgentExecutor.ExecuteGAgentEventHandler(configManager, updateEvent);

        // Allow time for state update
        await Task.Delay(1000);

        // Get updated state
        var updatedState = await configManager.GetStateAsync();

        // Assert
        Assert.True(updatedState.LastUpdated > initialState.LastUpdated);
        Assert.Equal(initialUpdateCount + 1, updatedState.TotalUpdates);
        Assert.True(updatedState.ConfigUpdateTimes.ContainsKey("TestConfig"));

        _testOutputHelper.WriteLine($"State tracking verified:");
        _testOutputHelper.WriteLine($"  - Initial updates: {initialUpdateCount}");
        _testOutputHelper.WriteLine($"  - Current updates: {updatedState.TotalUpdates}");
        _testOutputHelper.WriteLine($"  - Last updated: {updatedState.LastUpdated}");
        _testOutputHelper.WriteLine(
            $"  - Config types updated: {string.Join(", ", updatedState.ConfigUpdateTimes.Keys)}");
    }
}