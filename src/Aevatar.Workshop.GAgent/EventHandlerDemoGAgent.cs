using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class EventHandlerDemoGAgentState : StateBase
{
    [Id(0)] public List<string> Content { get; set; } = [];
}

public class EventHandlerDemoStateLogEvent : StateLogEventBase<EventHandlerDemoStateLogEvent>;

[GAgent("eventHandlerDemo", "demo")]
public class EventHandlerDemoGAgent : GAgentBase<EventHandlerDemoGAgentState, EventHandlerDemoStateLogEvent>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This GAgent is used for testing event handlers.");
    }

    // This method can be recognized as an event handler,
    // because the method name matches `HandleEventAsync`.
    public Task HandleEventAsync(GreetingEvent eventData)
    {
        Logger.LogInformation("New greeting event received by HandleEventAsync: {EventDataGreeting}",
            eventData.Greeting);
        State.Content.Add(eventData.Greeting);
        return Task.CompletedTask;
    }

    [EventHandler]
    public Task ExecuteAsync(GreetingEvent eventData)
    {
        Logger.LogInformation("New greeting event received by ExecuteAsync: {EventDataGreeting}", eventData.Greeting);
        return Task.CompletedTask;
    }

    [AllEventHandler]
    public Task HandleEventAsync(EventWrapperBase eventData)
    {
        if (eventData is EventWrapper<EventBase> wrapper)
        {
            Logger.LogInformation($"{wrapper.EventId}: {wrapper.Event.GetType()}");
        }

        return Task.CompletedTask;
    }
}