using Aevatar.GAgents.AIGAgent.State;

namespace Aevatar.Workshop.AIRouterWorkflowGAgent.Researcher;

[GenerateSerializer]
public class ResearcherState : AIGAgentStateBase
{
    [Id(0)] public string ResearchResult { get; set; }
}