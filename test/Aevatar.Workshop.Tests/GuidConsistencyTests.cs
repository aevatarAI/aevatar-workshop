using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.GAgents;
using Aevatar.Workshop.TestBase;
using Shouldly;
using Xunit.Abstractions;
using static Aevatar.Workshop.Client.Controllers.TheoryReasoningDemoController;

namespace Aevatar.Workshop.Tests;

/// <summary>
/// Tests to verify GUID consistency between Controller and Coordinator
/// </summary>
[Collection(ClusterCollection.Name)]
public sealed class GuidConsistencyTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public GuidConsistencyTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task Controller_And_Coordinator_Should_Use_Same_KnowledgeAgent_Instance()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing GUID consistency between Controller and Coordinator...");
        
        // Get the same GUID used by both Controller and Coordinator
        var knowledgeAgentId = new Guid("22222222-2222-2222-2222-222222222222");
        
        // Act - Get KnowledgeAgent using the consistent GUID
        _testOutputHelper.WriteLine($"🚀 Getting KnowledgeAgent with GUID: {knowledgeAgentId}");
        var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(knowledgeAgentId);
        
        // Initialize the knowledge base
        _testOutputHelper.WriteLine("🚀 Initializing knowledge base...");
        var initResult = await knowledgeAgent.InitializeWithPsiTheoryAsync();
        initResult.ShouldBeTrue();
        
        // Verify theories were loaded
        var stats = await knowledgeAgent.GetStatisticsAsync();
        var totalTheories = stats.GetValueOrDefault("Total", 0);
        _testOutputHelper.WriteLine($"📊 Total theories after initialization: {totalTheories}");
        
        totalTheories.ShouldBeGreaterThan(0, "Should have loaded initial theories");
        
        // Now get the same agent again using the same GUID - should be the same instance
        _testOutputHelper.WriteLine("🔍 Getting KnowledgeAgent again with same GUID...");
        var sameAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(knowledgeAgentId);
        
        // Should have the same theories
        var sameStats = await sameAgent.GetStatisticsAsync();
        var sameTotalTheories = sameStats.GetValueOrDefault("Total", 0);
        _testOutputHelper.WriteLine($"📊 Total theories from same agent: {sameTotalTheories}");
        
        sameTotalTheories.ShouldBe(totalTheories, "Same GUID should return same instance with same data");
        
        // Verify specific theory exists
        var theory = await sameAgent.GetTheoryAsync("A1");
        theory.ShouldNotBeNull("Theory A1 should exist in the knowledge base");
        _testOutputHelper.WriteLine($"✅ Found theory A1: {theory.Content.Substring(0, Math.Min(100, theory.Content.Length))}...");
    }

    [Fact]
    public async Task Coordinator_Should_Initialize_And_Persist_Knowledge()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing Coordinator initialization and knowledge persistence...");
        
        var coordinatorId = new Guid("11111111-1111-1111-1111-111111111111");
        var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(coordinatorId);
        
        // Act - Initialize the system
        _testOutputHelper.WriteLine("🚀 Initializing system through Coordinator...");
        var initResult = await coordinator.InitializeSystemAsync("AzureOpenAI");
        initResult.ShouldBeTrue();
        
        // Get the knowledge agent using the same GUID as Controller/Coordinator
        var knowledgeAgentId = new Guid("22222222-2222-2222-2222-222222222222");
        var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(knowledgeAgentId);
        
        // Verify theories are accessible
        var stats = await knowledgeAgent.GetStatisticsAsync();
        var totalTheories = stats.GetValueOrDefault("Total", 0);
        _testOutputHelper.WriteLine($"📊 Total theories accessible through knowledge agent: {totalTheories}");
        
        totalTheories.ShouldBeGreaterThan(0, "Theories should be accessible after coordinator initialization");
        
        // Check system status
        var coordinatorState = await coordinator.GetStateAsync();
        _testOutputHelper.WriteLine($"📊 System status: {coordinatorState.SystemStatus}");
        coordinatorState.SystemStatus.ShouldBe("initialized");
    }
}