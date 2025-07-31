using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.Workshop.GAgent;
using Aevatar.Workshop.GAgent.Events;
using Aevatar.Workshop.GAgent.Extensions;
using Aevatar.Workshop.GAgent.GAgents.SmartHome;
using Aevatar.Workshop.GuideGAgents.GAgents;
using Aevatar.Workshop.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public sealed class SmartHomeDemoTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    // Fixed GUIDs for consistent testing
    private static readonly Guid LIGHT_ID = "light agent".ToGuid();
    private static readonly Guid THERMOSTAT_ID = "thermostat agent".ToGuid();
    private static readonly Guid SECURITY_ID = "security agent".ToGuid();
    private static readonly Guid CURTAIN_ID = "curtain agent".ToGuid();
    private static readonly Guid AI_AGENT_ID = "ai agent".ToGuid();

    public SmartHomeDemoTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    #region LightGAgent Tests

    [Fact]
    public async Task LightGAgent_TurnOnOff_ShouldChangeState()
    {
        // Arrange
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Test Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);

        // Act & Assert - Turn On
        await light.TurnOnAsync();
        var isOn = await light.IsOnAsync();
        isOn.ShouldBeTrue();

        // Act & Assert - Turn Off
        await light.TurnOffAsync();
        isOn = await light.IsOnAsync();
        isOn.ShouldBeFalse();
    }

    [Fact]
    public async Task LightGAgent_SetBrightness_ShouldUpdateBrightness()
    {
        // Arrange
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Test Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);

        // Act
        await light.SetBrightnessAsync(75);

        // Assert
        var brightness = await light.GetBrightnessAsync();
        brightness.ShouldBe(75);
    }

    [Fact]
    public async Task LightGAgent_SetInvalidBrightness_ShouldThrowException()
    {
        // Arrange
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Test Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => light.SetBrightnessAsync(-10));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => light.SetBrightnessAsync(150));
    }

    [Fact]
    public async Task LightGAgent_HandleTurnOnCommand_ShouldTurnOnLight()
    {
        // Arrange
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Test Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);

        // Act
        await light.TurnOnAsync();

        // Assert
        var isOn = await light.IsOnAsync();
        isOn.ShouldBeTrue();
    }

    [Fact]
    public async Task LightGAgent_HandleSetBrightnessCommand_ShouldSetBrightness()
    {
        // Arrange
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Test Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);

        // Act
        await light.SetBrightnessAsync(50);

        // Assert
        var brightness = await light.GetBrightnessAsync();
        brightness.ShouldBe(50);
    }

    #endregion

    #region ThermostatGAgent Tests

    [Fact]
    public async Task ThermostatGAgent_SetTargetTemperature_ShouldUpdateTemperature()
    {
        // Arrange
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);

        // Act
        await thermostat.SetTargetTemperatureAsync(25.5);

        // Assert
        var targetTemp = await thermostat.GetTargetTemperatureAsync();
        targetTemp.ShouldBe(25.5);
    }

    [Fact]
    public async Task ThermostatGAgent_GetCurrentTemperature_ShouldReturnValue()
    {
        // Arrange
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);

        // Act
        var currentTemp = await thermostat.GetCurrentTemperatureAsync();

        // Assert
        currentTemp.ShouldBeGreaterThan(0); // Should have a reasonable temperature value
    }

    [Fact]
    public async Task ThermostatGAgent_GetMode_ShouldReturnCurrentMode()
    {
        // Arrange
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);

        // Act
        var mode = await thermostat.GetModeAsync();

        // Assert
        Enum.IsDefined(typeof(ThermostatMode), mode).ShouldBeTrue();
    }

    [Fact]
    public async Task ThermostatGAgent_SetTemperatureRange_ShouldAcceptValidRange()
    {
        // Arrange
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);

        // Act & Assert - Test various valid temperatures
        await thermostat.SetTargetTemperatureAsync(16.0); // Minimum
        var temp1 = await thermostat.GetTargetTemperatureAsync();
        temp1.ShouldBe(16.0);

        await thermostat.SetTargetTemperatureAsync(30.0); // Maximum
        var temp2 = await thermostat.GetTargetTemperatureAsync();
        temp2.ShouldBe(30.0);

        await thermostat.SetTargetTemperatureAsync(22.5); // Normal
        var temp3 = await thermostat.GetTargetTemperatureAsync();
        temp3.ShouldBe(22.5);
    }

    #endregion

    #region SecurityGAgent Tests

    [Fact]
    public async Task SecurityGAgent_ArmDisarm_ShouldChangeArmedState()
    {
        // Arrange
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);

        // Act & Assert - Arm system
        await security.ArmAsync();
        var isArmed = await security.IsArmedAsync();
        isArmed.ShouldBeTrue();

        // Act & Assert - Disarm system
        await security.DisarmAsync();
        isArmed = await security.IsArmedAsync();
        isArmed.ShouldBeFalse();
    }

    [Fact]
    public async Task SecurityGAgent_SimulateMotion_ShouldDetectMotion()
    {
        // Arrange
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
        await security.ArmAsync(); // Must be armed to detect motion

        // Act
        await security.SimulateMotionAsync("Front Door");

        // Assert
        var lastMotionLocation = await security.GetLastMotionLocationAsync();
        var lastMotionTime = await security.GetLastMotionTimeAsync();

        lastMotionLocation.ShouldBe("Front Door");
        lastMotionTime.ShouldBeGreaterThan(DateTime.MinValue);
    }

    [Fact]
    public async Task SecurityGAgent_MotionWhenDisarmed_ShouldNotDetect()
    {
        // Arrange
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
        await security.DisarmAsync(); // Ensure disarmed

        // Act
        await security.SimulateMotionAsync("Living Room");

        // Wait for movement to complete
        await Task.Delay(2000);

        // Assert
        var lastMotionLocation = await security.GetLastMotionLocationAsync();
        lastMotionLocation.ShouldBeEmpty();
    }

    #endregion

    #region CurtainGAgent Tests

    [Fact]
    public async Task CurtainGAgent_SetPosition_ShouldChangePosition()
    {
        // Arrange
        var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

        // Act
        await curtain.SetPositionAsync(75);

        // Wait for movement to complete
        await Task.Delay(2000);

        // Assert
        var state = await curtain.GetStateAsync();
        state.CurrentPosition.ShouldBeLessThan(100);
        state.TargetPosition.ShouldBe(75);
    }

    [Fact]
    public async Task CurtainGAgent_OpenClose_ShouldSetCorrectPositions()
    {
        // Arrange
        var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

        // Act & Assert - Open curtain (100%)
        await curtain.OpenAsync();
        await Task.Delay(1000); // Allow time for movement
        var state = await curtain.GetStateAsync();
        state.TargetPosition.ShouldBe(100);

        // Act & Assert - Close curtain (0%)
        await curtain.CloseAsync();
        await Task.Delay(1000); // Allow time for movement
        state = await curtain.GetStateAsync();
        state.TargetPosition.ShouldBe(0);
    }

    [Fact]
    public async Task CurtainGAgent_SetInvalidPosition_ShouldClampToValidRange()
    {
        // Arrange
        var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

        // Act - Test boundary values
        await curtain.SetPositionAsync(-10); // Should clamp to 0
        await Task.Delay(500);
        var state1 = await curtain.GetStateAsync();
        state1.TargetPosition.ShouldBeGreaterThanOrEqualTo(0);

        await curtain.SetPositionAsync(150); // Should clamp to 100
        await Task.Delay(500);
        var state2 = await curtain.GetStateAsync();
        state2.TargetPosition.ShouldBeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task CurtainGAgent_Stop_ShouldStopMovement()
    {
        // Arrange
        var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

        // Act - Start movement and then stop
        await curtain.SetPositionAsync(80);
        await Task.Delay(100); // Let movement start
        await curtain.StopAsync();

        // Assert
        var state = await curtain.GetStateAsync();
        state.IsMoving.ShouldBeFalse();
    }

    #endregion

    #region HomeAIGAgent Tests

    [Fact]
    public async Task HomeAIGAgent_Initialize_ShouldSetInitializedState()
    {
        // Arrange
        await ConfigLLMAsync();
        var aiAgent = await _gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(AI_AGENT_ID);

        // Act
        var result = await aiAgent.InitializeAsync("OpenAI");

        // Assert
        result.ShouldBeTrue();
        var isInitialized = await aiAgent.IsInitializedAsync();
        isInitialized.ShouldBeTrue();
    }

    [Fact]
    public async Task HomeAIGAgent_ProcessCommand_ShouldReturnResponse()
    {
        // Arrange
        await ConfigLLMAsync();
        var aiAgent = await _gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(AI_AGENT_ID);
        await aiAgent.InitializeAsync("OpenAI");

        // Act
        var response = await aiAgent.ProcessCommandAsync("Hello, how are you?");

        // Assert
        response.ShouldNotBeNull();
        response.Response.ShouldNotBeEmpty();
        response.ToolCalls.ShouldNotBeNull();
    }

    [Fact]
    public async Task HomeAIGAgent_GetChatHistory_ShouldReturnHistory()
    {
        // Arrange
        await ConfigLLMAsync();
        var aiAgent = await _gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(AI_AGENT_ID);
        await aiAgent.InitializeAsync("OpenAI");

        // Act
        await aiAgent.ProcessCommandAsync("Test message");
        var history = await aiAgent.GetChatHistoryAsync();

        // Assert
        history.ShouldNotBeNull();
        history.ShouldNotBeEmpty();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task SmartHomeSystem_CompleteSetup_ShouldInitializeAllDevices()
    {
        // Arrange
        var aiAgent = await _gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(AI_AGENT_ID);

        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Living Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
        var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

        // Act - Setup system
        await aiAgent.RegisterAsync(light);
        await aiAgent.RegisterAsync(thermostat);
        await aiAgent.RegisterAsync(security);
        await aiAgent.RegisterAsync(curtain);

        await ConfigLLMAsync();
        var initResult = await aiAgent.InitializeAsync("OpenAI");

        // Assert
        initResult.ShouldBeTrue();

        // Verify all devices are accessible
        (await light.GetStateAsync()).ShouldNotBeNull();
        (await thermostat.GetStateAsync()).ShouldNotBeNull();
        (await security.GetStateAsync()).ShouldNotBeNull();
        (await curtain.GetStateAsync()).ShouldNotBeNull();

        _testOutputHelper.WriteLine("Smart Home system setup completed successfully");
    }

    [Fact]
    public async Task SmartHomeSystem_GoodMorningScene_ShouldConfigureAllDevices()
    {
        // Arrange - Setup all devices
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Living Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
        var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

        // Act - Execute "Good Morning" scene
        await light.TurnOnAsync();
        await light.SetBrightnessAsync(100);
        await thermostat.SetTargetTemperatureAsync(22.0);
        await security.DisarmAsync();
        await curtain.OpenAsync();

        // Wait for curtain movement
        await Task.Delay(2000);

        // Assert - Verify scene configuration
        var lightState = await light.GetStateAsync();
        var thermostatState = await thermostat.GetStateAsync();
        var securityState = await security.GetStateAsync();
        var curtainState = await curtain.GetStateAsync();

        lightState.IsOn.ShouldBeTrue();
        lightState.Brightness.ShouldBe(100);
        thermostatState.TargetTemperature.ShouldBe(22.0);
        securityState.IsArmed.ShouldBeFalse();
        curtainState.TargetPosition.ShouldBe(100);

        _testOutputHelper.WriteLine("Good Morning scene executed successfully");
    }

    [Fact]
    public async Task SmartHomeSystem_GoodNightScene_ShouldConfigureAllDevices()
    {
        // Arrange - Setup all devices
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Living Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
        var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

        // Act - Execute "Good Night" scene
        await light.TurnOffAsync();
        await thermostat.SetTargetTemperatureAsync(20.0);
        await security.ArmAsync();
        await curtain.CloseAsync();

        // Wait for curtain movement
        await Task.Delay(2000);

        // Assert - Verify scene configuration
        var lightState = await light.GetStateAsync();
        var thermostatState = await thermostat.GetStateAsync();
        var securityState = await security.GetStateAsync();
        var curtainState = await curtain.GetStateAsync();

        lightState.IsOn.ShouldBeFalse();
        thermostatState.TargetTemperature.ShouldBe(20.0);
        securityState.IsArmed.ShouldBeTrue();
        curtainState.TargetPosition.ShouldBe(0);

        _testOutputHelper.WriteLine("Good Night scene executed successfully");
    }

    [Fact]
    public async Task SmartHomeSystem_MovieScene_ShouldConfigureDevicesForMovieWatching()
    {
        // Arrange - Setup all devices
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Living Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
        var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

        // Act - Execute "Movie" scene
        await light.TurnOnAsync();
        await light.SetBrightnessAsync(20); // Dim lighting
        await thermostat.SetTargetTemperatureAsync(21.0);
        await security.DisarmAsync(); // Disable alerts during movie
        await curtain.CloseAsync(); // Block outside light

        // Wait for curtain movement
        await Task.Delay(2000);

        // Assert - Verify scene configuration
        var lightState = await light.GetStateAsync();
        var thermostatState = await thermostat.GetStateAsync();
        var securityState = await security.GetStateAsync();
        var curtainState = await curtain.GetStateAsync();

        lightState.IsOn.ShouldBeTrue();
        lightState.Brightness.ShouldBe(20); // Dim for movie watching
        thermostatState.TargetTemperature.ShouldBe(21.0);
        securityState.IsArmed.ShouldBeFalse();
        curtainState.TargetPosition.ShouldBe(0); // Closed for darkness

        _testOutputHelper.WriteLine("Movie scene executed successfully");
    }

    [Fact]
    public async Task SmartHomeSystem_DeviceStateConsistency_ShouldMaintainStatesAcrossOperations()
    {
        // Arrange
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Consistency Test Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);

        // Act - Perform multiple operations
        await light.TurnOnAsync();
        await light.SetBrightnessAsync(75);
        await light.TurnOffAsync();
        await light.TurnOnAsync();

        // Assert - Verify state consistency
        var finalState = await light.GetStateAsync();
        finalState.IsOn.ShouldBeTrue();
        finalState.Brightness.ShouldBe(75); // Brightness should be preserved
        finalState.Location.ShouldBe("Consistency Test Room");
        finalState.LastChangeAt.ShouldNotBe(DateTime.MinValue);

        _testOutputHelper.WriteLine($"Device state consistency verified: {finalState.LightId}");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task SmartHomeSystem_ConcurrentOperations_ShouldHandleMultipleDevices()
    {
        // Arrange
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Performance Test Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);
        var thermostat = await _gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
        var security = await _gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
        var curtain = await _gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

        // Act - Perform concurrent operations
        var tasks = new List<Task>
        {
            light.TurnOnAsync(),
            light.SetBrightnessAsync(80),
            thermostat.SetTargetTemperatureAsync(23.0),
            security.ArmAsync(),
            curtain.SetPositionAsync(60)
        };

        await Task.WhenAll(tasks);

        // Assert - Verify all operations completed successfully
        var lightState = await light.GetStateAsync();
        var thermostatState = await thermostat.GetStateAsync();
        var securityState = await security.GetStateAsync();
        var curtainState = await curtain.GetStateAsync();

        lightState.IsOn.ShouldBeTrue();
        lightState.Brightness.ShouldBe(80);
        thermostatState.TargetTemperature.ShouldBe(23.0);
        securityState.IsArmed.ShouldBeTrue();

        // Note: Curtain might still be moving, so we check target position
        curtainState.TargetPosition.ShouldBe(60);

        _testOutputHelper.WriteLine("Concurrent operations completed successfully");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SmartHomeSystem_InvalidOperations_ShouldHandleGracefully()
    {
        // Arrange
        var lightConfig = new LightConfiguration
        {
            LightId = LIGHT_ID.ToString("N"),
            Location = "Error Test Room"
        };
        var light = await _gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);

        // Act & Assert - Test boundary conditions
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => light.SetBrightnessAsync(-1));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => light.SetBrightnessAsync(101));

        // Test multiple on/off operations (should not throw)
        await light.TurnOnAsync();
        await light.TurnOnAsync(); // Should handle gracefully
        await light.TurnOffAsync();
        await light.TurnOffAsync(); // Should handle gracefully

        // Verify final state is consistent
        var isOn = await light.IsOnAsync();
        isOn.ShouldBeFalse();

        _testOutputHelper.WriteLine("Error handling verified successfully");
    }

    #endregion

    private async Task ConfigLLMAsync()
    {
        var configManager = await _gAgentFactory.GetSystemLLMConfigGAgent();
        await configManager.UpdateConfigAsync(new ConfigUpdateEvent
        {
            ConfigType = typeof(SystemLLMConfigOptions).FullName!,
            ConfigJson = JsonSerializer.Serialize(new Dictionary<string, LLMConfig>
            {
                ["OpenAI"] = new()
                {
                    ProviderEnum = LLMProviderEnum.OpenAI,
                    ModelIdEnum = ModelIdEnum.OpenAI,
                    ApiKey = "test-api-key",
                    Endpoint = "https://api.test-endpoint.com",
                    ModelName = "test-model-name",
                    NetworkTimeoutInSeconds = 100
                }
            })
        });
    }
}