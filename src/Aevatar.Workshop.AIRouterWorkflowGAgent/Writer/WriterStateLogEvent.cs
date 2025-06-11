using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.AIRouterWorkflowGAgent.Writer;

[GenerateSerializer]
public class WriterStateLogEvent: StateLogEventBase<WriterStateLogEvent>
{
    
}

[GenerateSerializer]
public class SetArticleStateLogEvent: WriterStateLogEvent
{
    [Id(0)]
    public string Article { get; set; }
}