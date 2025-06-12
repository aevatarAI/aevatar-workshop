using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class BobGAgentState : StateBase;

[GenerateSerializer]
public class BobStateLogEvent : StateLogEventBase<BobStateLogEvent>;

[GAgent("bob", "demo")]
public class BobGAgent : GAgentBase<BobGAgentState, BobStateLogEvent>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent stands for Bob.");
    }

    [EventHandler]
    public Task HandleReplyAsync(ReplyEvent eventData)
    {
        Logger.LogInformation($"Bob received reply: {eventData.Reply}");
        return Task.CompletedTask;
    }
}