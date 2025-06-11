using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.Workshop.AIRouterWorkflowGAgent.Writer;

[GenerateSerializer]
public class WriterState : AIGAgentStateBase
{
    [Id(0)]
    public string Article { get; set; }
}