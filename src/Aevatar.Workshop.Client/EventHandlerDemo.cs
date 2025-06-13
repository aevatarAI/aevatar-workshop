using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent;

namespace Aevatar.Workshop.Client;

public static class EventHandlerDemo
{
    public static async Task RunAsync(IGAgentFactory gAgentFactory, string greeting = "Hello, Aevatar!")
    {
        // GAgent can be created via interface.
        var publishingGAgent = await gAgentFactory.GetGAgentAsync<IPublishingGAgent>();

        // GAgent can also be created via alias & namespace.
        var eventHandlerGAgent = await gAgentFactory.GetGAgentAsync("eventHandlerDemo", "demo");

        // Now we publish an event to the eventHandlerGAgent.
        await publishingGAgent.PublishEventAsync(new GreetingEvent { Greeting = greeting }, eventHandlerGAgent);
    }
}