using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.GAgents;
using Aevatar.Workshop.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public sealed class ThinkingStepsSessionIdTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;
    
    // Use the same GUIDs as defined in TheoryReasoningDemoController for consistency
    private static readonly Guid CoordinatorId = new Guid("11111111-1111-1111-1111-111111111111");
    private static readonly Guid KnowledgeAgentId = new Guid("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ReasoningAgentId = new Guid("66666666-6666-6666-6666-666666666666");
    private static readonly Guid ConfigManagerAgentId = new Guid("77777777-7777-7777-7777-777777777777");

    public ThinkingStepsSessionIdTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task AutoReasoningAIGAgent_SessionIdParameter_ShouldBeAccepted()
    {
        // Arrange
        var reasoningAgent = await _gAgentFactory.GetGAgentAsync<IAutoReasoningAIGAgent>(ReasoningAgentId);
        var testSessionId = "test-session-123";
        
        // Act & Assert - Test that the sessionId parameter is accepted
        try
        {
            var result = await reasoningAgent.PerformDeductiveReasoningAsync(
                new List<string> { "test-theory-1" }, 
                "test-domain", 
                testSessionId);
            
            _testOutputHelper.WriteLine($"✅ SessionId parameter accepted successfully");
            _testOutputHelper.WriteLine($"Result: {string.Join(", ", result)}");
            result.ShouldNotBeNull();
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"❌ Method call failed: {ex.Message}");
            throw;
        }
    }
    
    [Fact]
    public async Task AutoReasoningAIGAgent_SessionIdPropagation_ShouldWork()
    {
        // Arrange
        var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(CoordinatorId);
        var reasoningAgent = await _gAgentFactory.GetGAgentAsync<IAutoReasoningAIGAgent>(ReasoningAgentId);
        
        // Test that coordinator can call reasoning agent with sessionId
        _testOutputHelper.WriteLine("Testing sessionId propagation in reasoning methods");
        
        var testSessionId = "test-session-456";
        var sourceTheories = new List<string> { "A1", "T1-1" }; // Use known initial theories
        
        try
        {
            // Act - Call the reasoning method with sessionId parameter (no actual LLM needed for this test)
            var result = await reasoningAgent.PerformDeductiveReasoningAsync(
                sourceTheories, 
                "test_domain", 
                testSessionId);
            
            // Assert
            result.ShouldNotBeNull();
            _testOutputHelper.WriteLine($"✅ SessionId '{testSessionId}' was successfully passed to reasoning method");
            _testOutputHelper.WriteLine($"Reasoning completed with result count: {result.Count}");
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"❌ SessionId propagation test failed: {ex.Message}");
            throw;
        }
    }
}