using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent;
using System;
using System.Threading.Tasks;

namespace Aevatar.Workshop.Client;

public static class MultiGAgentDemo
{
    public static async Task RunAsync(IGAgentFactory gAgentFactory)
    {
        // Create Alice and Bob
        var alice = await gAgentFactory.GetGAgentAsync("alice", "demo");
        var bob = await gAgentFactory.GetGAgentAsync("bob", "demo");
        var publisher = await gAgentFactory.GetGAgentAsync<IPublishingGAgent>();

        // Send greeting to Alice
        await publisher.PublishEventAsync(new GreetingEvent { Greeting = "Hello Alice, this is Bob!" }, alice, bob);

        Console.WriteLine("GreetingEvent sent to Alice. Watch logs for Alice and Bob's collaboration.");
    }
}