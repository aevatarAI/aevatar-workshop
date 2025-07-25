using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.Workshop.GAgent.Events;
using Aevatar.Workshop.GAgent.GAgents.SmartHome;
using Aevatar.Workshop.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests.SmartHomeDemo;

[Collection(ClusterCollection.Name)]
public class SmartHomeDemoTest : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public SmartHomeDemoTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task SmartHomeDemo_BasicDeviceControl_ShouldWork()
    {
        // Arrange
        var lightId = Guid.NewGuid();
        var thermostatId = Guid.NewGuid(); 
        var securityId = Guid.NewGuid();
        var coordinatorId = Guid.NewGuid();
        
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(lightId);
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(thermostatId);
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(securityId);
        var coordinator = await _gAgentFactory.GetGAgentAsync<IHomeCoordinatorGAgent>(coordinatorId);

        // Subscribe devices to coordinator for event communication
        await light.SubscribeToAsync(coordinator);
        await thermostat.SubscribeToAsync(coordinator);
        await security.SubscribeToAsync(coordinator);

        // Register devices with coordinator
        await coordinator.RegisterDeviceAsync("light", light.GetGrainId().ToString());
        await coordinator.RegisterDeviceAsync("thermostat", thermostat.GetGrainId().ToString());
        await coordinator.RegisterDeviceAsync("security", security.GetGrainId().ToString());

        // Act & Assert - Test Light Control
        await light.TurnOnAsync();
        Assert.True(await light.IsOnAsync());
        Assert.Equal(100, await light.GetBrightnessAsync());

        await light.SetBrightnessAsync(50);
        Assert.Equal(50, await light.GetBrightnessAsync());

        await light.TurnOffAsync();
        Assert.False(await light.IsOnAsync());

        // Act & Assert - Test Thermostat Control
        await thermostat.SetTargetTemperatureAsync(25);
        Assert.Equal(25, await thermostat.GetTargetTemperatureAsync());

        await thermostat.SetModeAsync(ThermostatMode.Heating);
        Assert.Equal(ThermostatMode.Heating, await thermostat.GetModeAsync());

        // Act & Assert - Test Security Control
        await security.ArmAsync();
        Assert.True(await security.IsArmedAsync());

        await security.DisarmAsync();
        Assert.False(await security.IsArmedAsync());

        _testOutputHelper.WriteLine("Basic device control tests passed!");
    }

    [Fact]
    public async Task SmartHomeDemo_SceneExecution_ShouldWork()
    {
        // Arrange
        var coordinatorId = Guid.NewGuid();
        var coordinator = await _gAgentFactory.GetGAgentAsync<IHomeCoordinatorGAgent>(coordinatorId);

        // Register devices
        var lightId = Guid.NewGuid();
        var thermostatId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(lightId);
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(thermostatId);
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(securityId);

        // Subscribe devices to coordinator for event communication
        await light.SubscribeToAsync(coordinator);
        await thermostat.SubscribeToAsync(coordinator);
        await security.SubscribeToAsync(coordinator);

        await coordinator.RegisterDeviceAsync("light", light.GetGrainId().ToString());
        await coordinator.RegisterDeviceAsync("thermostat", thermostat.GetGrainId().ToString());
        await coordinator.RegisterDeviceAsync("security", security.GetGrainId().ToString());

        // Act - Execute "Good Night" scene
        await coordinator.ExecuteSceneAsync("晚安");

        // Wait for scene execution to complete
        await Task.Delay(500);

        // Assert - Verify scene effects
        Assert.False(await light.IsOnAsync());
        Assert.Equal(20, await thermostat.GetTargetTemperatureAsync());
        Assert.True(await security.IsArmedAsync());

        _testOutputHelper.WriteLine("Scene '晚安' executed successfully");
    }

    [Fact(Skip = "Requires AI service configuration")]
    public async Task SmartHomeDemo_AIControl_ShouldWork()
    {
        // Arrange
        var aiId = Guid.NewGuid();
        var coordinatorId = Guid.NewGuid();
        var lightId = Guid.NewGuid();
        var thermostatId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        
        var aiAgent = await _gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(aiId);
        var coordinator = await _gAgentFactory.GetGAgentAsync<IHomeCoordinatorGAgent>(coordinatorId);
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(lightId);
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(thermostatId);
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(securityId);

        // Register devices
        await coordinator.RegisterDeviceAsync("light", light.GetGrainId().ToString());
        await coordinator.RegisterDeviceAsync("thermostat", thermostat.GetGrainId().ToString());
        await coordinator.RegisterDeviceAsync("security", security.GetGrainId().ToString());

        // Subscribe AI agent to coordinator
        await aiAgent.SubscribeToAsync(coordinator);

        // Initialize AI with system LLM
        var initResult = await aiAgent.InitializeAsync("MockOpenAI");
        Assert.True(initResult);

        // Act - Send natural language command
        var response = await aiAgent.ProcessCommandAsync("打开客厅的灯并设置温度到22度");

        // Assert
        Assert.NotNull(response);
        Assert.Contains("已", response.Response); // Chinese response expected

        // Wait for command processing
        await Task.Delay(500);

        // Verify effects
        Assert.True(await light.IsOnAsync());
        Assert.Equal(22, await thermostat.GetTargetTemperatureAsync());

        _testOutputHelper.WriteLine($"AI processed command successfully: {response}");
    }

    [Fact]
    public async Task SmartHomeDemo_EventFlow_ShouldWork()
    {
        // Arrange
        var coordinatorId = Guid.NewGuid();
        var lightId = Guid.NewGuid();
        
        var coordinator = await _gAgentFactory.GetGAgentAsync<IHomeCoordinatorGAgent>(coordinatorId);
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(lightId);

        await coordinator.RegisterDeviceAsync("light", light.GetGrainId().ToString());

        // Subscribe light to coordinator for event flow
        await light.SubscribeToAsync(coordinator);

        // Act - Process device control intent through coordinator
        await coordinator.ProcessDeviceControlIntentAsync(new DeviceControlIntentEvent
        {
            DeviceType = "light",
            Action = "turn_on",
            Parameters = new() { ["brightness"] = 75 }
        });

        // Wait for event processing
        await Task.Delay(200);

        // Assert
        Assert.True(await light.IsOnAsync());
        Assert.Equal(75, await light.GetBrightnessAsync());

        _testOutputHelper.WriteLine("Event flow test passed - device control intent processed successfully");
    }
} 