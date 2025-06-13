using Aevatar.TestKit;
using Aevatar.Workshop.GAgent;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Aevatar.Workshop.Tests;

public class SampleTests : TestKitBase<AevatarSampleTestKitSilo>
{
    [Fact(DisplayName = "Show how to register services to DI.")]
    public void TestKitDITest()
    {
        var sampleService = Silo.ServiceProvider.GetRequiredService<ISampleService>();
        sampleService.Test().ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "Show how to get a GAgent.")]
    public async Task SampleGAgentTest()
    {
        var sampleGAgent = await Silo.CreateGrainAsync<EventHandlerDemoGAgent>(Guid.NewGuid());
        var description = await sampleGAgent.GetDescriptionAsync();
        description.ShouldBe("This GAgent is used for testing event handlers.");
    }
}