using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class AliceGAgentState : StateBase
{

}

[GenerateSerializer]
public class AliceStateLogEvent : StateLogEventBase<AliceStateLogEvent>
{

}

[GAgent("alice", "demo")]
public class AliceGAgent : GAgentBase<AliceGAgentState, AliceStateLogEvent>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent stands for Alice.");
    }

    [EventHandler]
    public async Task HandleGreetingAsync(GreetingEvent eventData)
    {
        Logger.LogInformation($"Alice received greeting: {eventData.Greeting}");
        // Alice replies to Bob
        await PublishAsync(new ReplyEvent
        {
            Reply = $"Hi Bob, Alice received: {eventData.Greeting}"
        });
    }
}