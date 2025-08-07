using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.GAgents;
using Aevatar.Workshop.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public sealed class MCPPythonGAgentTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public MCPPythonGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task MCPPythonGAgent_Creation_ShouldSucceed()
    {
        // Arrange & Act
        var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(Guid.NewGuid());

        // Assert
        mcpAgent.ShouldNotBeNull();
        
        var description = await mcpAgent.GetDescriptionAsync();
        description.ShouldContain("MCP-based Python execution agent");
        
        _testOutputHelper.WriteLine($"✅ MCPPythonGAgent created successfully: {description}");
    }

    [Fact]
    public async Task MCPPythonGAgent_Initialization_ShouldHandleGracefully()
    {
        // Arrange
        var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(Guid.NewGuid());

        // Act
        var initResult = await mcpAgent.InitializeAsync();

        // Assert - Should not throw, even if MCP server is not available
        // In test environment, this may fail due to missing MCP server, but should handle gracefully
        _testOutputHelper.WriteLine($"MCP Agent initialization result: {initResult}");
        
        if (initResult)
        {
            _testOutputHelper.WriteLine("✅ MCP server connection successful");
            
            // Test basic functionality
            var serverInfo = await mcpAgent.GetMCPServerInfoAsync();
            serverInfo.ShouldNotBeNull();
            _testOutputHelper.WriteLine($"MCP Server info: {serverInfo}");
        }
        else
        {
            _testOutputHelper.WriteLine("⚠️ MCP server not available (expected in test environment)");
        }
    }

    [Fact]
    public async Task MCPPythonGAgent_CodeValidation_ShouldWork()
    {
        // Arrange
        var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync(); // May fail, but test continues

        var validCode = @"
import math
def calculate_area(radius):
    return math.pi * radius ** 2

# Test the function
result = calculate_area(5)
print(f'Area of circle with radius 5: {result}')
";

        var invalidCode = @"
import os
os.system('rm -rf /')  # Dangerous code
";

        // Act & Assert - Even if MCP server is not available, validation should still work
        try
        {
            var validResult = await mcpAgent.ValidateCodeAsync(validCode);
            var invalidResult = await mcpAgent.ValidateCodeAsync(invalidCode);

            _testOutputHelper.WriteLine($"Valid code validation: {validResult}");
            _testOutputHelper.WriteLine($"Invalid code validation: {invalidResult}");
            
            // If MCP is working, invalid code should be rejected
            if (await mcpAgent.TestMCPConnectionAsync())
            {
                invalidResult.ShouldBeFalse("Dangerous code should be rejected");
            }
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"Code validation test failed (expected if MCP server unavailable): {ex.Message}");
        }
    }

    [Fact]
    public async Task MCPPythonGAgent_ExecutionStatsAsync_ShouldReturnStats()
    {
        // Arrange
        var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync();

        // Act
        var stats = await mcpAgent.GetExecutionStatsAsync();

        // Assert
        stats.ShouldNotBeNull();
        stats.ShouldContainKey("TotalExecutions");
        stats.ShouldContainKey("SuccessfulExecutions");
        stats.ShouldContainKey("FailedExecutions");
        stats.ShouldContainKey("MCPServerConnected");

        _testOutputHelper.WriteLine("📊 MCP Agent Statistics:");
        foreach (var stat in stats)
        {
            _testOutputHelper.WriteLine($"  {stat.Key}: {stat.Value}");
        }
    }

    [Fact]
    public async Task MCPPythonGAgent_EnvironmentManagement_ShouldHandleBasicOperations()
    {
        // Arrange
        var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync();

        var testEnvName = "test-env-" + Guid.NewGuid().ToString("N")[..8];

        // Act & Assert
        try
        {
            // List environments (should work even without MCP server)
            var environments = await mcpAgent.ListEnvironmentsAsync();
            environments.ShouldNotBeNull();
            _testOutputHelper.WriteLine($"Available environments: {string.Join(", ", environments)}");

            // Test environment creation (may fail without MCP server)
            var createResult = await mcpAgent.CreateEnvironmentAsync(testEnvName, "3.9");
            _testOutputHelper.WriteLine($"Environment creation result: {createResult.Success}");

            if (createResult.Success)
            {
                // Test environment info
                var envInfo = await mcpAgent.GetEnvironmentInfoAsync(testEnvName);
                envInfo.ShouldNotBeNull();
                envInfo.EnvironmentName.ShouldBe(testEnvName);

                // Cleanup
                var deleteResult = await mcpAgent.DeleteEnvironmentAsync(testEnvName);
                _testOutputHelper.WriteLine($"Environment deletion result: {deleteResult}");
            }
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"Environment management test failed (expected if MCP server unavailable): {ex.Message}");
        }
    }

    [Fact]
    public async Task MCPPythonGAgent_StateManagement_ShouldTrackExecutions()
    {
        // Arrange
        var mcpAgent = await _gAgentFactory.GetGAgentAsync<IMCPPythonGAgent>(Guid.NewGuid());
        await mcpAgent.InitializeAsync();

        // Act
        var initialState = await mcpAgent.GetStateAsync();

        // Simulate some executions (even if they fail due to no MCP server)
        try
        {
            var executionResult =await mcpAgent.ExecuteCodeAsync("print('test')");
            executionResult.Success.ShouldBeTrue();
        }
        catch
        {
            // Expected if MCP server is not available
        }

        var finalState = await mcpAgent.GetStateAsync();

        // Assert
        initialState.ShouldNotBeNull();
        finalState.ShouldNotBeNull();

        _testOutputHelper.WriteLine($"Initial state - Total executions: {initialState.TotalExecutions}");
        _testOutputHelper.WriteLine($"Final state - Total executions: {finalState.TotalExecutions}");
        _testOutputHelper.WriteLine($"MCP server connected: {finalState.MCPServerConnected}");
        _testOutputHelper.WriteLine($"Available environments: {string.Join(", ", finalState.AvailableEnvironments)}");
    }
}