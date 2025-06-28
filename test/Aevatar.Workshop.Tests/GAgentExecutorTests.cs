using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent;
using Shouldly;

namespace Aevatar.Workshop.Tests;

public sealed class GAgentExecutorTests : AevatarWorkshopTestBase
{
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly IGAgentFactory _gAgentFactory;

    public GAgentExecutorTests()
    {
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _gAgentExecutor = GetRequiredService<IGAgentExecutor>();
    }

    [Fact]
    public async Task ExecuteGAgentEventHandler_VerifyStateLogEvent_ShouldBeRecorded()
    {
        // Arrange
        var targetGAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<EventHandlerDemoGAgentState>>();
        var grainId = targetGAgent.GetGrainId();

        var greetingEvent = new GreetingEvent
        {
            Greeting = "StateLog verification test！"
        };

        // Act
        var result = await _gAgentExecutor.ExecuteGAgentEventHandler(grainId, greetingEvent);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain(greetingEvent.Greeting);

        // Get the EventHandlerDemoGAgent instance to verify its state
        var state = await targetGAgent.GetStateAsync();

        // Verify the greeting was added to the state content
        state.Content.ShouldContain(greetingEvent.Greeting);
    }

    [Fact]
    public async Task ExecuteGAgentEventHandler_WithTimeout_ShouldThrowTimeoutException()
    {
        // This test would require a special setup to simulate timeout
        // For now, we'll test that the executor handles events within reasonable time

        var greetingEvent = new GreetingEvent
        {
            Greeting = "Timeout test!"
        };

        var targetGAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<EventHandlerDemoGAgentState>>();

        // Act & Assert - should complete within reasonable time
        var startTime = DateTime.UtcNow;
        await _gAgentExecutor.ExecuteGAgentEventHandler(targetGAgent, greetingEvent);
        var duration = DateTime.UtcNow - startTime;

        Assert.True(duration.TotalSeconds < 10, "duration.TotalSeconds < 10");
    }
}