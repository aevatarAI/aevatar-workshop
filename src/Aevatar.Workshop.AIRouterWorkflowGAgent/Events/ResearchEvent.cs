using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.AIRouterWorkflowGAgent.Events;

[Description("Research something.")]
[GenerateSerializer]
public class ResearchEvent : EventBase
{
    [Description("The content to be research.")]
    [Id(0)]
    public string Content { get; set; }
}