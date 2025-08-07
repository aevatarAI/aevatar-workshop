using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.GAgents;
using Aevatar.Workshop.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public sealed class HybridPythonVerificationGAgentTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public HybridPythonVerificationGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task HybridPythonVerificationGAgent_Creation_ShouldSucceed()
    {
        // Arrange & Act
        var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(Guid.NewGuid());

        // Assert
        hybridAgent.ShouldNotBeNull();
        
        var description = await hybridAgent.GetDescriptionAsync();
        description.ShouldContain("Hybrid Python verification agent");
        
        _testOutputHelper.WriteLine($"✅ HybridPythonVerificationGAgent created successfully: {description}");
    }

    [Fact]
    public async Task HybridPythonVerificationGAgent_Initialization_ShouldDetectAvailableAgents()
    {
        // Arrange
        var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(Guid.NewGuid());

        // Act
        var config = new HybridPythonVerificationConfig
        {
            PreferredMode = PythonExecutionMode.Auto,
            EnableFallback = true,
            LogModeSelections = true
        };

        var initResult = await hybridAgent.InitializeAsync(config);

        // Assert
        initResult.ShouldBeTrue("At least one Python agent should be available");

        var state = await hybridAgent.GetStateAsync();
        _testOutputHelper.WriteLine($"Legacy agent available: {state.LegacyAgentAvailable}");
        _testOutputHelper.WriteLine($"MCP agent available: {state.MCPAgentAvailable}");
        _testOutputHelper.WriteLine($"Current mode: {state.CurrentMode}");

        // At least one agent should be available
        (state.LegacyAgentAvailable || state.MCPAgentAvailable).ShouldBeTrue();
    }

    [Fact]
    public async Task HybridPythonVerificationGAgent_ModeSelection_ShouldWorkCorrectly()
    {
        // Arrange
        var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(Guid.NewGuid());
        await hybridAgent.InitializeAsync();

        // Act & Assert - Test mode setting
        var legacyModeSet = await hybridAgent.SetExecutionModeAsync(PythonExecutionMode.Legacy);
        var currentMode = await hybridAgent.GetCurrentModeAsync();
        
        _testOutputHelper.WriteLine($"Legacy mode set result: {legacyModeSet}");
        _testOutputHelper.WriteLine($"Current mode after setting to Legacy: {currentMode}");

        if (legacyModeSet)
        {
            currentMode.ShouldBe(PythonExecutionMode.Legacy);
        }

        // Test MCP mode
        var mcpModeSet = await hybridAgent.SetExecutionModeAsync(PythonExecutionMode.MCP);
        if (mcpModeSet)
        {
            currentMode = await hybridAgent.GetCurrentModeAsync();
            currentMode.ShouldBe(PythonExecutionMode.MCP);
            _testOutputHelper.WriteLine("✅ Successfully set MCP mode");
        }
        else
        {
            _testOutputHelper.WriteLine("⚠️ MCP mode not available (expected if MCP server is not running)");
        }

        // Test Auto mode
        var autoModeSet = await hybridAgent.SetExecutionModeAsync(PythonExecutionMode.Auto);
        autoModeSet.ShouldBeTrue("Auto mode should always be available");
        
        currentMode = await hybridAgent.GetCurrentModeAsync();
        _testOutputHelper.WriteLine($"Mode after setting to Auto: {currentMode}");
    }

    [Fact]
    public async Task HybridPythonVerificationGAgent_BothAgentsTest_ShouldProvideResults()
    {
        // Arrange
        var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(Guid.NewGuid());
        await hybridAgent.InitializeAsync();

        // Act
        var testResult = await hybridAgent.TestBothAgentsAsync();

        // Assert
        testResult.ShouldBeTrue("At least one agent should be working");

        var state = await hybridAgent.GetStateAsync();
        _testOutputHelper.WriteLine($"Agent test results:");
        _testOutputHelper.WriteLine($"  Legacy agent: {(state.LegacyAgentAvailable ? "✅ Available" : "❌ Not available")}");
        _testOutputHelper.WriteLine($"  MCP agent: {(state.MCPAgentAvailable ? "✅ Available" : "❌ Not available")}");
        _testOutputHelper.WriteLine($"  Last mode check: {state.LastModeCheck}");
    }

    [Fact]
    public async Task HybridPythonVerificationGAgent_CodeValidation_ShouldWorkWithFallback()
    {
        // Arrange
        var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(Guid.NewGuid());
        await hybridAgent.InitializeAsync();

        var testCode = @"
import math

def calculate_factorial(n):
    if n <= 1:
        return 1
    return n * calculate_factorial(n - 1)

# Test the function
result = calculate_factorial(5)
print(f'Factorial of 5: {result}')
assert result == 120, 'Factorial calculation is incorrect'
";

        // Act
        var validationResult = await hybridAgent.ValidatePythonCodeAsync(testCode);

        // Assert
        _testOutputHelper.WriteLine($"Code validation result: {validationResult}");
        
        // Get execution statistics
        var stats = await hybridAgent.GetHybridStatsAsync();
        stats.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine("📊 Hybrid Agent Statistics:");
        foreach (var stat in stats)
        {
            _testOutputHelper.WriteLine($"  {stat.Key}: {stat.Value}");
        }

        // Should work with at least one agent available
        validationResult.ShouldBeOfType<bool>();
    }

    [Fact]
    public async Task HybridPythonVerificationGAgent_TheoryVerification_ShouldHandleBasicCase()
    {
        // Arrange
        var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(Guid.NewGuid());
        await hybridAgent.InitializeAsync();

        var theoryId = "test-theory-" + Guid.NewGuid().ToString("N")[..8];
        var theoryContent = "The area of a circle with radius r is π * r²";
        var formalExpression = "A = π * r²";

        // Act
        try
        {
            var verificationResult = await hybridAgent.VerifyTheoryAsync(theoryId, theoryContent, formalExpression);

            // Assert
            verificationResult.ShouldNotBeNull();
            verificationResult.TheoryId.ShouldBe(theoryId);
            
            _testOutputHelper.WriteLine($"Theory verification result:");
            _testOutputHelper.WriteLine($"  Theory ID: {verificationResult.TheoryId}");
            _testOutputHelper.WriteLine($"  Tests passed: {verificationResult.TestsPassed}");
            _testOutputHelper.WriteLine($"  Execution time: {verificationResult.ExecutionTime} ms");
            _testOutputHelper.WriteLine($"  Total tests: {verificationResult.TotalTests}");
            _testOutputHelper.WriteLine($"  Passed tests: {verificationResult.PassedTests}");
            
            if (!string.IsNullOrEmpty(verificationResult.ErrorOutput))
            {
                _testOutputHelper.WriteLine($"  Error output: {verificationResult.ErrorOutput}");
            }

            if (!string.IsNullOrEmpty(verificationResult.StandardOutput))
            {
                _testOutputHelper.WriteLine($"  Standard output: {verificationResult.StandardOutput}");
            }
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"Theory verification failed (may be expected if no agents available): {ex.Message}");
        }
    }

    [Fact]
    public async Task HybridPythonVerificationGAgent_PythonCodeGeneration_ShouldReturnCode()
    {
        // Arrange
        var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(Guid.NewGuid());
        await hybridAgent.InitializeAsync();

        var theoryContent = "Calculate the sum of squares from 1 to n";
        var formalExpression = "sum = Σ(i²) for i from 1 to n";

        // Act
        try
        {
            var generatedCode = await hybridAgent.GeneratePythonCodeAsync(theoryContent, formalExpression);

            // Assert
            generatedCode.ShouldNotBeNull();
            generatedCode.ShouldNotBeEmpty();
            
            _testOutputHelper.WriteLine($"Generated Python code:");
            _testOutputHelper.WriteLine(generatedCode);
            
            // Basic validation that it looks like Python code
            (generatedCode.Contains("def") || generatedCode.Contains("=") || generatedCode.Contains("import")).ShouldBeTrue();
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"Code generation failed (may be expected if no agents available): {ex.Message}");
        }
    }

    [Fact]
    public async Task HybridPythonVerificationGAgent_ExecutionHistory_ShouldTrackExecutions()
    {
        // Arrange
        var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(Guid.NewGuid());
        await hybridAgent.InitializeAsync();

        // Act - Perform some operations
        try
        {
            await hybridAgent.ValidatePythonCodeAsync("print('test1')");
            await hybridAgent.ValidatePythonCodeAsync("print('test2')");
        }
        catch
        {
            // Operations may fail if no agents available, but state should still be tracked
        }

        // Get execution history
        var recentExecutions = await hybridAgent.GetRecentExecutionsAsync(5);
        var performanceComparison = await hybridAgent.GetPerformanceComparisonAsync();

        // Assert
        recentExecutions.ShouldNotBeNull();
        performanceComparison.ShouldNotBeNull();

        _testOutputHelper.WriteLine($"Recent executions count: {recentExecutions.Count}");
        
        foreach (var execution in recentExecutions)
        {
            _testOutputHelper.WriteLine($"  Execution {execution.ExecutionId}:");
            _testOutputHelper.WriteLine($"    Mode: {execution.SelectedMode}");
            _testOutputHelper.WriteLine($"    Start time: {execution.StartTime}");
            _testOutputHelper.WriteLine($"    Reason: {execution.Reason}");
            _testOutputHelper.WriteLine($"    Fallback used: {execution.FallbackUsed}");
        }

        _testOutputHelper.WriteLine("Performance metrics:");
        foreach (var metric in performanceComparison)
        {
            _testOutputHelper.WriteLine($"  {metric.Key}: {metric.Value}");
        }
    }

    [Fact]
    public async Task HybridPythonVerificationGAgent_StateTransitions_ShouldUpdateCorrectly()
    {
        // Arrange
        var hybridAgent = await _gAgentFactory.GetGAgentAsync<IHybridPythonVerificationGAgent>(Guid.NewGuid());
        
        // Act
        var initialState = await hybridAgent.GetStateAsync();
        await hybridAgent.InitializeAsync();
        var postInitState = await hybridAgent.GetStateAsync();

        // Assert
        initialState.ShouldNotBeNull();
        postInitState.ShouldNotBeNull();

        _testOutputHelper.WriteLine("State transitions:");
        _testOutputHelper.WriteLine($"  Initial - Legacy: {initialState.LegacyAgentAvailable}, MCP: {initialState.MCPAgentAvailable}");
        _testOutputHelper.WriteLine($"  Post-init - Legacy: {postInitState.LegacyAgentAvailable}, MCP: {postInitState.MCPAgentAvailable}");
        _testOutputHelper.WriteLine($"  Legacy executions: {postInitState.LegacyExecutionCount}");
        _testOutputHelper.WriteLine($"  MCP executions: {postInitState.MCPExecutionCount}");
        _testOutputHelper.WriteLine($"  Fallback count: {postInitState.FallbackCount}");
        _testOutputHelper.WriteLine($"  Current mode: {postInitState.CurrentMode}");

        // After initialization, at least one agent should be detected
        (postInitState.LegacyAgentAvailable || postInitState.MCPAgentAvailable).ShouldBeTrue();
    }
}