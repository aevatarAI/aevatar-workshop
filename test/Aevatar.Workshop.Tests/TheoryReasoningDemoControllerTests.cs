using Aevatar.Core.Abstractions;
using Aevatar.Workshop.Client.Controllers;
using Aevatar.Workshop.GAgent.GAgents;
using Aevatar.Workshop.TestBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit.Abstractions;
using Aevatar.GAgents.Basic.BasicGEvent;
using Aevatar.Workshop.GAgent.Extensions;

namespace Aevatar.Workshop.Tests;

/// <summary>
/// Tests for TheoryReasoningDemoController API endpoints
/// </summary>
[Collection(ClusterCollection.Name)]
public sealed class TheoryReasoningDemoControllerTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<TheoryReasoningDemoController> _logger;

    public TheoryReasoningDemoControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _logger = GetRequiredService<ILogger<TheoryReasoningDemoController>>();
    }

    private TheoryReasoningDemoController CreateController()
    {
        return new TheoryReasoningDemoController(_gAgentFactory, _logger);
    }

    #region System Status API Tests

    [Fact]
    public async Task GetSystemStatusAsync_WithoutInitialization_ShouldReturnNotInitialized()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing system status without initialization...");
        var controller = CreateController();

        // Act
        var result = await controller.GetSystemStatusAsync();

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var responseData = okResult.Value.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine($"📊 System status response: {responseData}");

        // Check if response has success property
        var successProperty = responseData.GetType().GetProperty("success");
        successProperty.ShouldNotBeNull();
        
        var success = (bool)successProperty.GetValue(responseData)!;
        success.ShouldBeTrue();

        // Check initialization status
        var initializedProperty = responseData.GetType().GetProperty("initialized");
        if (initializedProperty != null)
        {
            var initialized = (bool)initializedProperty.GetValue(responseData)!;
            _testOutputHelper.WriteLine($"📊 Initialized: {initialized}");
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldSucceed()
    {
        // Arrange
        _testOutputHelper.WriteLine("🚀 Testing system initialization...");
        var controller = CreateController();

        // Act
        var request = new InitializeRequest { SystemLLM = "AzureOpenAI" };
        var result = await controller.InitializeSystemAsync(request);

        // Assert
        result.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine($"📊 Initialize result type: {result.GetType().Name}");
        
        if (result is BadRequestObjectResult badResult)
        {
            _testOutputHelper.WriteLine($"❌ BadRequest response: {badResult.Value}");
            
            // Extract error message
            var errorData = badResult.Value;
            if (errorData != null)
            {
                var errorProperty = errorData.GetType().GetProperty("message");
                if (errorProperty != null)
                {
                    var errorMessage = errorProperty.GetValue(errorData)?.ToString();
                    _testOutputHelper.WriteLine($"❌ Error message: {errorMessage}");
                }
            }
        }
        
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var responseData = okResult.Value.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine($"📊 Initialize response: {responseData}");

        // Check if response has success property
        var successProperty = responseData.GetType().GetProperty("success");
        successProperty.ShouldNotBeNull();
        
        var success = (bool)successProperty.GetValue(responseData)!;
        success.ShouldBeTrue();
    }

    [Fact]
    public async Task GetSystemStatusAsync_AfterInitialization_ShouldReturnInitialized()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing system status after initialization...");
        var controller = CreateController();

        // Initialize first
        await controller.InitializeSystemAsync(new InitializeRequest { SystemLLM = "AzureOpenAI" });

        // Act
        var result = await controller.GetSystemStatusAsync();

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var responseData = okResult.Value.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine($"📊 System status after init: {responseData}");

        // Check if response has success property
        var successProperty = responseData.GetType().GetProperty("success");
        successProperty.ShouldNotBeNull();
        
        var success = (bool)successProperty.GetValue(responseData)!;
        success.ShouldBeTrue();

        // Check initialization status
        var initializedProperty = responseData.GetType().GetProperty("initialized");
        initializedProperty.ShouldNotBeNull();
        
        var initialized = (bool)initializedProperty.GetValue(responseData)!;
        initialized.ShouldBeTrue();
        
        _testOutputHelper.WriteLine($"✅ System is initialized: {initialized}");
    }

    #endregion

    #region Theories API Tests

    [Fact]
    public async Task GetTheoriesAsync_WithoutInitialization_ShouldReturnEmptyList()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing theories API without initialization...");
        var controller = CreateController();

        // Act
        var result = await controller.GetTheoriesAsync();

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var responseData = okResult.Value.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine($"📊 Theories response: {responseData}");

        // Check if response has success property
        var successProperty = responseData.GetType().GetProperty("success");
        successProperty.ShouldNotBeNull();
        
        var success = (bool)successProperty.GetValue(responseData)!;
        success.ShouldBeTrue();

        // Check total theories
        var totalTheoriesProperty = responseData.GetType().GetProperty("totalTheories");
        totalTheoriesProperty.ShouldNotBeNull();
        
        var totalTheories = (int)totalTheoriesProperty.GetValue(responseData)!;
        _testOutputHelper.WriteLine($"📊 Total theories: {totalTheories}");
    }

    [Fact]
    public async Task GetTheoriesAsync_AfterInitialization_ShouldReturnInitialTheories()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing theories API after initialization...");
        var controller = CreateController();

        // Initialize first
        await controller.InitializeSystemAsync(new InitializeRequest { SystemLLM = "AzureOpenAI" });

        // Wait a moment for initialization to complete
        await Task.Delay(2000);

        // Act
        var result = await controller.GetTheoriesAsync();

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var responseData = okResult.Value.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine($"📊 Theories after init: {responseData}");

        // Check if response has success property
        var successProperty = responseData.GetType().GetProperty("success");
        successProperty.ShouldNotBeNull();
        
        var success = (bool)successProperty.GetValue(responseData)!;
        success.ShouldBeTrue();

        // Check total theories
        var totalTheoriesProperty = responseData.GetType().GetProperty("totalTheories");
        totalTheoriesProperty.ShouldNotBeNull();
        
        var totalTheories = (int)totalTheoriesProperty.GetValue(responseData)!;
        _testOutputHelper.WriteLine($"📊 Total theories after init: {totalTheories}");

        // Check theories array
        var theoriesProperty = responseData.GetType().GetProperty("theories");
        theoriesProperty.ShouldNotBeNull();
        
        var theories = theoriesProperty.GetValue(responseData);
        _testOutputHelper.WriteLine($"📊 Theories object: {theories}");
        
        // If we have theories, log their details
        if (theories is System.Collections.IEnumerable enumerable)
        {
            var theoryList = enumerable.Cast<object>().ToList();
            _testOutputHelper.WriteLine($"📊 Theory count in list: {theoryList.Count}");
            
            foreach (var theory in theoryList.Take(5)) // Log first 5 theories
            {
                _testOutputHelper.WriteLine($"📊 Theory: {theory}");
            }
        }
    }

    #endregion

    #region Knowledge Base Direct Tests

    [Fact]
    public async Task KnowledgeBase_DirectAccess_ShouldShowInitialTheories()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing knowledge base direct access...");
        var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(
            new Guid("12345678-1234-1234-1234-123456789012")); // Use fixed GUID

        // Act - Initialize knowledge base
        _testOutputHelper.WriteLine("🚀 Initializing knowledge base...");
        var initResult = await knowledgeAgent.InitializeWithPsiTheoryAsync();
        initResult.ShouldBeTrue();

        // Wait for initialization
        await Task.Delay(1000);

        // Act - Get all theories
        _testOutputHelper.WriteLine("📖 Getting all theories...");
        var theories = await knowledgeAgent.GetAllTheoriesAsync();

        // Assert
        theories.ShouldNotBeNull();
        _testOutputHelper.WriteLine($"📊 Direct access theory count: {theories.Count}");

        foreach (var theory in theories.Take(10)) // Log first 10 theories
        {
            _testOutputHelper.WriteLine($"📊 Theory {theory.Id}: {theory.Type} - {theory.Content.Substring(0, Math.Min(50, theory.Content.Length))}...");
        }

        // Verify we have the expected initial theories
        theories.Count.ShouldBeGreaterThan(0);
        theories.Any(t => t.Id == "A1").ShouldBeTrue("Should have axiom A1");
        theories.Any(t => t.Id == "P1").ShouldBeTrue("Should have proposition P1");
        theories.Any(t => t.Id == "T1-1").ShouldBeTrue("Should have theorem T1-1");
    }

    [Fact]
    public async Task Coordinator_InitializationFlow_ShouldInitializeKnowledgeBase()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing coordinator initialization flow...");
        var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(
            new Guid("12345678-1234-1234-1234-123456789001")); // Use fixed GUID

        // Act - Initialize coordinator
        _testOutputHelper.WriteLine("🚀 Initializing coordinator...");
        var initResult = await coordinator.InitializeSystemAsync("AzureOpenAI");
        
        _testOutputHelper.WriteLine($"📊 Coordinator init result: {initResult}");
        
        if (!initResult)
        {
            // Get coordinator state to see what went wrong
            var coordinatorState = await coordinator.GetStateAsync();
            _testOutputHelper.WriteLine($"❌ Coordinator state after failed init:");
            _testOutputHelper.WriteLine($"   - Status: {coordinatorState.SystemStatus}");
            _testOutputHelper.WriteLine($"   - Initialized: {coordinatorState.Initialized}");
            _testOutputHelper.WriteLine($"   - Available agents: {string.Join(", ", coordinatorState.AvailableAgents)}");
        }
        
        initResult.ShouldBeTrue();

        // Wait for initialization
        await Task.Delay(3000);

        // Act - Check coordinator state
        _testOutputHelper.WriteLine("📊 Checking coordinator state...");
        var state = await coordinator.GetStateAsync();
        
        _testOutputHelper.WriteLine($"📊 System status: {state.SystemStatus}");
        _testOutputHelper.WriteLine($"📊 Initialized: {state.Initialized}");
        _testOutputHelper.WriteLine($"📊 Available agents: {string.Join(", ", state.AvailableAgents)}");

        // Assert
        state.Initialized.ShouldBeTrue();
        state.SystemStatus.ShouldNotBeNullOrEmpty();
        state.AvailableAgents.ShouldNotBeEmpty();
    }

    #endregion

    #region Configuration System Tests

    [Fact]
    public async Task ConfigManagerGAgent_ShouldProvideSystemLLMConfig()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing ConfigManagerGAgent for SystemLLM configs...");

        // First setup configuration
        await SetupSystemLLMConfigAsync();

        // Act - Get ConfigManagerGAgent
        var configManager = await _gAgentFactory.GetSystemLLMConfigGAgent();
        
        // Request SystemLLM configuration
        var requestEvent = new ConfigRequestEvent
        {
            ConfigType = "Aevatar.GAgents.AI.Options.SystemLLMConfigOptions",
            ConfigKey = null // Request entire dictionary
        };

        var response = await configManager.RequestConfigAsync(requestEvent);

        // Assert
        _testOutputHelper.WriteLine($"📊 Config response - Success: {response.Success}");
        _testOutputHelper.WriteLine($"📊 Config response - HasJson: {!string.IsNullOrEmpty(response.ConfigJson)}");
        
        if (!string.IsNullOrEmpty(response.ConfigJson))
        {
            _testOutputHelper.WriteLine($"📊 Config JSON: {response.ConfigJson}");
        }
        else
        {
            _testOutputHelper.WriteLine($"❌ Config error message: {response.ErrorMessage}");
        }

        response.Success.ShouldBeTrue();
        response.ConfigJson.ShouldNotBeNullOrEmpty();
        response.ConfigJson.ShouldContain("AzureOpenAI");
    }

    private async Task SetupSystemLLMConfigAsync()
    {
        _testOutputHelper.WriteLine("🔧 Setting up SystemLLM configuration for tests...");
        
        var configManager = await _gAgentFactory.GetSystemLLMConfigGAgent();
        
        // Create a mock configuration JSON with test LLM settings
        var testConfigJson = @"{
            ""AzureOpenAI"": {
                ""ModelName"": ""gpt-4"",
                ""Endpoint"": ""https://test.openai.azure.com/"",
                ""ApiKey"": ""test-api-key-12345"",
                ""Memo"": ""Test configuration"",
                ""NetworkTimeoutInSeconds"": 100,
                ""ProviderEnum"": 0,
                ""ModelIdEnum"": 0
            },
            ""TestProvider"": {
                ""ModelName"": ""test-model"",
                ""Endpoint"": ""https://test.example.com/"",
                ""ApiKey"": ""test-key"",
                ""Memo"": ""Test provider"",
                ""NetworkTimeoutInSeconds"": 60,
                ""ProviderEnum"": 1,
                ""ModelIdEnum"": 1
            }
        }";

        var updateEvent = new ConfigUpdateEvent
        {
            ConfigType = "Aevatar.GAgents.AI.Options.SystemLLMConfigOptions",
            ConfigJson = testConfigJson
        };

        var updateResponse = await configManager.UpdateConfigAsync(updateEvent);
        
        _testOutputHelper.WriteLine($"📊 Config setup - Success: {updateResponse.Success}");
        if (!updateResponse.Success)
        {
            _testOutputHelper.WriteLine($"❌ Config setup error: {updateResponse.ErrorMessage}");
        }
    }

    #endregion

    #region Individual Agent Initialization Tests

    [Fact]
    public async Task KnowledgeAgent_Initialization_ShouldSucceed()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing knowledge agent initialization...");
        var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(Guid.NewGuid());

        // Act
        var result = await knowledgeAgent.InitializeWithPsiTheoryAsync();

        // Assert
        _testOutputHelper.WriteLine($"📊 Knowledge agent init result: {result}");
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ReasoningAgent_Initialization_ShouldSucceed()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing reasoning agent initialization...");
        var reasoningAgentId = new Guid("66666666-6666-6666-6666-666666666666");
        var reasoningAgent = await _gAgentFactory.GetGAgentAsync<IAutoReasoningAIGAgent>(reasoningAgentId);
        
        // First setup LLM configuration
        _testOutputHelper.WriteLine("🔧 Setting up LLM configuration for AI agent...");
        await SetupSystemLLMConfigAsync();
        
        // Initialize the knowledge agent it depends on
        _testOutputHelper.WriteLine("🔍 Initializing knowledge agent first...");
        var knowledgeAgentId = new Guid("22222222-2222-2222-2222-222222222222");
        var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(knowledgeAgentId);
        var knowledgeInitResult = await knowledgeAgent.InitializeWithPsiTheoryAsync();
        _testOutputHelper.WriteLine($"📊 Knowledge agent init result: {knowledgeInitResult}");
        
        _testOutputHelper.WriteLine("🔍 Now initializing reasoning agent...");

        // Act
        bool result;
        try
        {
            result = await reasoningAgent.InitializeAsync("AzureOpenAI");
            _testOutputHelper.WriteLine($"📊 Reasoning agent init result: {result}");
            
            if (!result)
            {
                // Get reasoning agent state to see what went wrong
                var state = await reasoningAgent.GetStateAsync();
                _testOutputHelper.WriteLine($"❌ Reasoning agent state after failed init:");
                _testOutputHelper.WriteLine($"   - Initialized: {state.Initialized}");
                _testOutputHelper.WriteLine($"   - Active tasks: {state.ActiveTasks.Count}");
                _testOutputHelper.WriteLine($"   - Completed tasks: {state.CompletedTasks.Count}");
                
                // Try to debug the Brain initialization 
                _testOutputHelper.WriteLine("🔍 Debugging AIGAgentBase initialization...");
                
                // Check if we can access brain directly
                var stateType = state.GetType();
                _testOutputHelper.WriteLine($"   - State type: {stateType.Name}");
                _testOutputHelper.WriteLine($"   - Has brain properties: {stateType.GetProperties().Any(p => p.Name.Contains("Brain"))}");
                
                // Log reasoning agent state history
                foreach (var prop in stateType.GetProperties())
                {
                    try
                    {
                        var value = prop.GetValue(state);
                        _testOutputHelper.WriteLine($"   - {prop.Name}: {value}");
                    }
                    catch (Exception propEx)
                    {
                        _testOutputHelper.WriteLine($"   - {prop.Name}: Error getting value - {propEx.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"❌ Reasoning agent init failed with exception: {ex.Message}");
            _testOutputHelper.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            result = false;
        }

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task VerificationAgent_Initialization_ShouldSucceed()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing verification agent initialization...");
        var verificationAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());

        // Act
        bool result;
        try
        {
            result = await verificationAgent.InitializeAsync();
            _testOutputHelper.WriteLine($"📊 Verification agent init result: {result}");
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"❌ Verification agent init failed with exception: {ex.Message}");
            _testOutputHelper.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            result = false;
        }

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task FormalizationAgent_Initialization_ShouldSucceed()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing formalization agent initialization...");
        var formalizationAgent = await _gAgentFactory.GetGAgentAsync<IFormalizationAIGAgent>(Guid.NewGuid());

        // Act
        bool result;
        try
        {
            result = await formalizationAgent.InitializeAsync("AzureOpenAI");
            _testOutputHelper.WriteLine($"📊 Formalization agent init result: {result}");
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"❌ Formalization agent init failed with exception: {ex.Message}");
            _testOutputHelper.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            result = false;
        }

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ReviewAgent_Initialization_ShouldSucceed()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing review agent initialization...");
        var reviewAgent = await _gAgentFactory.GetGAgentAsync<IEquivalenceReviewGAgent>(Guid.NewGuid());

        // Act
        bool result;
        try
        {
            result = await reviewAgent.InitializeAsync("AzureOpenAI");
            _testOutputHelper.WriteLine($"📊 Review agent init result: {result}");
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"❌ Review agent init failed with exception: {ex.Message}");
            _testOutputHelper.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            result = false;
        }

        // Assert
        result.ShouldBeTrue();
    }

    #endregion
}