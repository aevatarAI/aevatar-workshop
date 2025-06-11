using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.AIRouterWorkflowGAgent.Researcher;

[GenerateSerializer]
public class ResearcherStateLogEvent: StateLogEventBase<ResearcherStateLogEvent>
{
    
}

[GenerateSerializer]
public class SetResearchResultStateLogEvent: ResearcherStateLogEvent
{
    [Id(0)]
    public string ResearchResult { get; set; }
}