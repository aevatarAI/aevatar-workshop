using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.GAgents;
using Aevatar.Workshop.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

/// <summary>
/// Tests for TheoryKnowledgeGAgent initialization and theory management
/// </summary>
[Collection(ClusterCollection.Name)]
public sealed class TheoryKnowledgeGAgentTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public TheoryKnowledgeGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task TheoryKnowledgeGAgent_InitializeWithPsiTheoryAsync_ShouldLoadInitialTheories()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing TheoryKnowledgeGAgent initialization...");
        var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(Guid.NewGuid());

        // Act
        _testOutputHelper.WriteLine("🚀 Calling InitializeWithPsiTheoryAsync...");
        var result = await knowledgeAgent.InitializeWithPsiTheoryAsync();

        // Assert
        _testOutputHelper.WriteLine($"📊 InitializeWithPsiTheoryAsync result: {result}");
        result.ShouldBeTrue();

        // Verify theories were loaded
        var stats = await knowledgeAgent.GetStatisticsAsync();
        var totalTheories = stats.GetValueOrDefault("Total", 0);
        _testOutputHelper.WriteLine($"📊 Total theories after initialization: {totalTheories}");
        
        totalTheories.ShouldBeGreaterThan(0, "Should have loaded initial theories");

        // Verify specific initial theories exist
        var expectedTheories = new[] { "A1", "C1-1", "D1-1", "P1", "T1-1", "T1-2", "L1-1" };
        
        foreach (var theoryId in expectedTheories)
        {
            var theory = await knowledgeAgent.GetTheoryAsync(theoryId);
            _testOutputHelper.WriteLine($"📋 Checking theory {theoryId}: {(theory != null ? "✅ Found" : "❌ Missing")}");
            theory.ShouldNotBeNull($"Theory {theoryId} should exist after initialization");
        }
        
        _testOutputHelper.WriteLine("✅ All initial theories verified successfully");
    }

    [Fact]
    public async Task TheoryKnowledgeGAgent_AddTheoryAsync_ShouldGenerateCorrectId()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing theory addition and ID generation...");
        var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(Guid.NewGuid());
        
        // Initialize with base theories first
        await knowledgeAgent.InitializeWithPsiTheoryAsync();

        var newTheory = new TheoryElement
        {
            Type = "P",
            Content = "Test proposition for ID generation",
            FormalExpression = "Test ∀x: P(x)",
            ReasoningMethod = "test",
            QualityScore = 0.8
        };

        // Act
        _testOutputHelper.WriteLine("🚀 Adding new theory...");
        var theoryId = await knowledgeAgent.AddTheoryAsync(newTheory);

        // Assert
        _testOutputHelper.WriteLine($"📊 Generated theory ID: {theoryId}");
        theoryId.ShouldNotBeNullOrEmpty();
        
        // Should generate P2-1 since P1 already exists
        theoryId.ShouldBe("P2-1", "Should generate sequential ID for type P");

        // Verify theory was saved
        var savedTheory = await knowledgeAgent.GetTheoryAsync(theoryId);
        _testOutputHelper.WriteLine($"📋 Saved theory verification: {(savedTheory != null ? "✅ Found" : "❌ Missing")}");
        savedTheory.ShouldNotBeNull("Theory should be retrievable after adding");
        savedTheory.Id.ShouldBe(theoryId);
        savedTheory.Content.ShouldBe(newTheory.Content);
    }

    [Fact]
    public async Task TheoryKnowledgeGAgent_ConcurrentAccess_ShouldHandleCorrectly()
    {
        // Arrange
        _testOutputHelper.WriteLine("🔍 Testing concurrent theory addition...");
        var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(Guid.NewGuid());
        
        // Initialize with base theories first
        await knowledgeAgent.InitializeWithPsiTheoryAsync();

        // Act - Add multiple theories concurrently
        var tasks = new List<Task<string>>();
        for (int i = 0; i < 5; i++)
        {
            var theory = new TheoryElement
            {
                Type = "P",
                Content = $"Concurrent test proposition {i}",
                FormalExpression = $"Test_{i} ∀x: P_{i}(x)",
                ReasoningMethod = "concurrent_test",
                QualityScore = 0.7
            };
            
            tasks.Add(knowledgeAgent.AddTheoryAsync(theory));
        }

        var theoryIds = await Task.WhenAll(tasks);

        // Assert
        _testOutputHelper.WriteLine($"📊 Generated {theoryIds.Length} theory IDs concurrently");
        foreach (var id in theoryIds)
        {
            _testOutputHelper.WriteLine($"📋 Generated ID: {id}");
            id.ShouldNotBeNullOrEmpty();
            id.ShouldStartWith("P");
        }

        // All IDs should be unique
        theoryIds.Distinct().Count().ShouldBe(theoryIds.Length, "All generated IDs should be unique");

        // Verify all theories are retrievable
        foreach (var id in theoryIds)
        {
            var theory = await knowledgeAgent.GetTheoryAsync(id);
            theory.ShouldNotBeNull($"Theory {id} should be retrievable");
        }
    }
}