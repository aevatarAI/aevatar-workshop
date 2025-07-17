using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent;

/// <summary>
/// Simple AI agent for workflow demo
/// </summary>
[GenerateSerializer]
public class WorkflowAIGAgentState : GroupMemberState
{
}

[GenerateSerializer]
public class WorkflowAIGAgentStateLogEvent : StateLogEventBase<WorkflowAIGAgentStateLogEvent>
{
}

public interface IWorkflowAIGAgent : IAIGAgent, IStateGAgent<WorkflowAIGAgentState>
{
    Task<string> ProcessAsync(string input);
}

public class WorkflowAIConfiguration : GroupMemberConfigDto;

[GAgent("workflowai", "workflow")]
public class WorkflowAIGAgent : GroupMemberGAgentBase<WorkflowAIGAgentState, WorkflowAIGAgentStateLogEvent, EventBase, WorkflowAIConfiguration>, IWorkflowAIGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("AI agent for workflow processing");
    }

    public async Task<string> ProcessAsync(string input)
    {
        Logger.LogInformation("Processing input: {Input}", input);
        
        // Use the base class ChatWithHistory method
        var chatResult = await ChatWithHistory(input);
        
        if (chatResult != null && chatResult.Count > 0)
        {
            var response = chatResult.Last().Content;
            Logger.LogInformation("AI response: {Response}", response);
            return response;
        }
        
        return "No response generated";
    }

    // Override this to initialize with a default prompt if needed
    protected override Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(State.PromptTemplate))
        {
            State.PromptTemplate = "You are a helpful AI assistant in a workflow. Process the input and provide helpful responses.";
        }
        
        return base.OnGAgentActivateAsync(cancellationToken);
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(1);
    }

    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        return Task.FromResult(new ChatResponse
        {
            Skip = true,
            Continue = false
        });
    }
} 