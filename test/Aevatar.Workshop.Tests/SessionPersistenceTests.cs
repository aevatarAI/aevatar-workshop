using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.GAgents;
using Aevatar.Workshop.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public sealed class SessionPersistenceTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;
    
    // Use the same GUIDs as defined in TheoryReasoningDemoController for consistency
    private static readonly Guid CoordinatorId = new Guid("11111111-1111-1111-1111-111111111111");
    private static readonly Guid KnowledgeAgentId = new Guid("22222222-2222-2222-2222-222222222222");

    public SessionPersistenceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task CoordinatorGAgent_CompletedSessions_ShouldBePersisted()
    {
        // Arrange
        var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(CoordinatorId);

        // Initialize the system first
        await coordinator.InitializeSystemAsync("AzureOpenAI");
        
        var config = new ReasoningSessionConfig
        {
            EnabledReasoningMethods = new List<string> { "deductive" },
            MaxIterations = 2,
            QualityThreshold = 0.5,
            EnableAutoReview = false,
            EnableAutoRevision = false,
            Parameters = new Dictionary<string, string> { ["SystemLLM"] = "AzureOpenAI" }
        };

        // Act - Start a reasoning session
        var sessionId = await coordinator.StartReasoningSessionAsync(config);
        sessionId.ShouldNotBeNull();
        sessionId.ShouldNotBeEmpty();

        // Allow some time for session processing
        await Task.Delay(2000);

        // Stop the session to move it to completed
        var stopResult = await coordinator.StopReasoningSessionAsync(sessionId);
        stopResult.ShouldBeTrue();

        // Allow some time for completion processing
        await Task.Delay(1000);

        // Assert - Verify session appears in completed sessions
        var allSessions = await coordinator.GetAllSessionsAsync();
        var completedSessions = await coordinator.GetCompletedSessionsAsync();
        var activeSessions = await coordinator.GetActiveSessionsAsync();

        _testOutputHelper.WriteLine($"Total sessions: {allSessions.Count}");
        _testOutputHelper.WriteLine($"Active sessions: {activeSessions.Count}");
        _testOutputHelper.WriteLine($"Completed sessions: {completedSessions.Count}");

        // Verify the session exists in completed sessions
        completedSessions.ShouldContain(s => s.SessionId == sessionId);
        
        // Verify all sessions contains both active and completed
        allSessions.Count.ShouldBe(activeSessions.Count + completedSessions.Count);
        
        // Verify the completed session has correct status
        var completedSession = completedSessions.First(s => s.SessionId == sessionId);
        completedSession.Status.ShouldBe("completed");
        completedSession.CompletedAt.ShouldNotBeNull();
        
        _testOutputHelper.WriteLine($"Session {sessionId} completed at: {completedSession.CompletedAt}");
    }

    [Fact]
    public async Task SessionsAPI_ShouldReturnAllSessions()
    {
        // Arrange
        var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(CoordinatorId);

        // Initialize if not already done
        await coordinator.InitializeSystemAsync("AzureOpenAI");
        
        // Get initial state
        var initialAllSessions = await coordinator.GetAllSessionsAsync();
        var initialCount = initialAllSessions.Count;

        var config = new ReasoningSessionConfig
        {
            EnabledReasoningMethods = new List<string> { "deductive" },
            MaxIterations = 1,
            QualityThreshold = 0.5,
            EnableAutoReview = false,
            EnableAutoRevision = false,
            Parameters = new Dictionary<string, string> { ["SystemLLM"] = "AzureOpenAI" }
        };

        // Act - Create multiple sessions
        var sessionId1 = await coordinator.StartReasoningSessionAsync(config);
        var sessionId2 = await coordinator.StartReasoningSessionAsync(config);

        // Stop one session to complete it
        await Task.Delay(1000);
        await coordinator.StopReasoningSessionAsync(sessionId1);
        
        // Leave sessionId2 active
        await Task.Delay(500);

        // Assert - Verify all sessions are returned
        var finalAllSessions = await coordinator.GetAllSessionsAsync();
        var finalActiveSessions = await coordinator.GetActiveSessionsAsync();
        var finalCompletedSessions = await coordinator.GetCompletedSessionsAsync();

        _testOutputHelper.WriteLine($"Final all sessions: {finalAllSessions.Count}");
        _testOutputHelper.WriteLine($"Final active sessions: {finalActiveSessions.Count}");
        _testOutputHelper.WriteLine($"Final completed sessions: {finalCompletedSessions.Count}");

        // Should have 2 more sessions than initially
        finalAllSessions.Count.ShouldBe(initialCount + 2);
        
        // Should have 1 active (sessionId2) and at least 1 completed (sessionId1)
        finalActiveSessions.ShouldContain(s => s.SessionId == sessionId2);
        finalCompletedSessions.ShouldContain(s => s.SessionId == sessionId1);
        
        // All sessions should include both active and completed
        finalAllSessions.ShouldContain(s => s.SessionId == sessionId1);
        finalAllSessions.ShouldContain(s => s.SessionId == sessionId2);
        
        // Verify the sessions are sorted by start time (most recent first)
        for (int i = 0; i < finalAllSessions.Count - 1; i++)
        {
            finalAllSessions[i].StartedAt.ShouldBeGreaterThanOrEqualTo(finalAllSessions[i + 1].StartedAt);
        }
    }

    [Fact]
    public async Task TheoryDetailModal_ShouldWorkWithExistingTheories()
    {
        // Arrange
        var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(KnowledgeAgentId);
        
        // Initialize knowledge base
        await knowledgeAgent.InitializeWithPsiTheoryAsync();
        
        // Act - Get all theories
        var allTheories = await knowledgeAgent.GetAllTheoriesAsync();
        
        // Assert - Should have initial theories
        allTheories.ShouldNotBeNull();
        allTheories.Count.ShouldBeGreaterThan(0);
        
        _testOutputHelper.WriteLine($"Found {allTheories.Count} theories in knowledge base:");
        
        foreach (var theory in allTheories.Take(3)) // Show first 3
        {
            _testOutputHelper.WriteLine($"- {theory.FullId}: {theory.Type} - {theory.Content?.Substring(0, Math.Min(50, theory.Content?.Length ?? 0))}...");
            
            // Verify theory has required properties for detail modal
            theory.Id.ShouldNotBeNullOrEmpty();
            theory.FullId.ShouldNotBeNullOrEmpty();
            theory.Type.ShouldNotBeNullOrEmpty();
            theory.Content.ShouldNotBeNullOrEmpty();
            theory.CreatedAt.ShouldNotBe(default(DateTime));
        }
    }
}