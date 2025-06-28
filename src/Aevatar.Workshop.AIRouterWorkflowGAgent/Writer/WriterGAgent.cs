using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.Router.GEvents;
using Aevatar.Workshop.Events;
using Aevatar.Workshop.GAgent;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.AIRouterWorkflowGAgent.Writer;

public interface IWriterGAgent : IAIGAgent, IGAgent
{
    Task<string> GetArticleAsync();
}

[Description("Writer agent")]
public class WriterGAgent : AIGAgentBase<WriterState, WriterStateLogEvent>, IWriterGAgent
{
    public override async Task<string> GetDescriptionAsync()
    {
        return "Writer GAgent";
    }

    public async Task<string> GetArticleAsync()
    {
        return State.Article;
    }

    [EventHandler]
    public async Task HandleEventAsync(WriteEvent @event)
    {
        Logger.LogInformation("Handle write event.");

        var prompt = WriterPromptTemplate.Prompt.Replace("{CONTENT}", @event.Content);
        await PublishAsync(new RecordEvent
        {
            Message = $"[Prompt for Writer]: \n{prompt}",
        });
        var chatResult = await ChatWithHistory(prompt);
        var article = chatResult?[0].Content;

        RaiseEvent(new SetArticleStateLogEvent
        {
            Article = article
        });
        await ConfirmEvents();

        await PublishAsync(new RouteNextGEvent
        {
            ProcessResult = "Writing is done."
        });
        await PublishAsync(new RecordEvent
        {
            Message = article
        });
    }

    protected override void AIGAgentTransitionState(WriterState state,
        StateLogEventBase<WriterStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetArticleStateLogEvent setArticleStateLogEvent:
                State.Article = setArticleStateLogEvent.Article;
                break;
        }
    }
}