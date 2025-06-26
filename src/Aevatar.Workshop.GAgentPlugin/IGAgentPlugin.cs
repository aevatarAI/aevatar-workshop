using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop;

public interface IGAgentPlugin
{
    Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event);
}