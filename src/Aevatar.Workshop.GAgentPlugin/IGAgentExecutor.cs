using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop;

public interface IGAgentExecutor
{
    Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, EventBase @event);
    Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event);
    Task<string> ExecuteGAgentEventHandler(GrainType grainType, EventBase @event);
}