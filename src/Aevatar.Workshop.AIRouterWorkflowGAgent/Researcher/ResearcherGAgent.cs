using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.Router.GEvents;
using Aevatar.Workshop.AIRouterWorkflowGAgent.Events;
using Aevatar.Workshop.GAgent;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.AIRouterWorkflowGAgent.Researcher;

public interface IResearcherGAgent : IAIGAgent, IGAgent
{
    Task<string> GetResultAsync();
}

[Description("Research agent")]
public class ResearcherGAgent : AIGAgentBase<ResearcherState, ResearcherStateLogEvent>, IResearcherGAgent
{
    public override async Task<string> GetDescriptionAsync()
    {
        return "Research GAgent";
    }

    [EventHandler]
    public async Task HandleEventAsync(ResearchEvent @event)
    {
        Logger.LogInformation("Handle research event.");

        var prompt = ResearchPromptTemplate.Prompt.Replace("{CONTENT}", @event.Content);
        await PublishAsync(new RecordEvent
        {
            Message = $"[Prompt for Researcher]: \n{prompt}",
        });
        var chatResult = await ChatWithHistory(prompt);
        var researchResult = chatResult?[0].Content;

        RaiseEvent(new SetResearchResultStateLogEvent
        {
            ResearchResult = researchResult
        });
        await ConfirmEvents();

        await PublishAsync(new RouteNextGEvent
        {
            ProcessResult = researchResult
        });
        await PublishAsync(new RecordEvent
        {
            Message = researchResult
        });
    }

    public async Task<string> GetResultAsync()
    {
        return State.ResearchResult;
    }

    protected override void AIGAgentTransitionState(ResearcherState state,
        StateLogEventBase<ResearcherStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetResearchResultStateLogEvent setResearchResultStateLogEvent:
                State.ResearchResult = setResearchResultStateLogEvent.ResearchResult;
                break;
        }
    }
}