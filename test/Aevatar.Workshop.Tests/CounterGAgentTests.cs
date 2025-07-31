using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GuideGAgents;
using Aevatar.Workshop.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public class CounterGAgentTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public CounterGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task CounterGAgentTest()
    {
        // Arrange.
        var counter = await _gAgentFactory.GetGAgentAsync<ICounterGAgent>(Guid.NewGuid());

        // Action.
        await counter.IncrementAsync(5);
        await counter.DecrementAsync(2);
        var currentValue = await counter.GetCurrentValueAsync();

        // Assert.
        currentValue.ShouldBe(3);

        // Print histories.
        var state = await counter.GetStateAsync();
        foreach (var history in state.History)
        {
            _testOutputHelper.WriteLine(history);
        }
    }
}