using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Aevatar.Workshop.GAgentPlugin;

[GenerateSerializer]
public class ResultGAgentStreamsState : StateBase
{
    [Id(0)] public string? Result { get; set; }
    [Id(1)] public string? ExecutionId { get; set; }
    [Id(2)] public string? StreamProvider { get; set; }
    [Id(3)] public string? StreamNamespace { get; set; }
}

[GenerateSerializer]
public class ResultArrivedStreamsStateLogEvent : ResultStreamsStateLogEvent
{
    [Id(0)] public string Result { get; set; } = string.Empty;
}

[GenerateSerializer]
public class SetExecutionContextStateLogEvent : ResultStreamsStateLogEvent
{
    [Id(0)] public string ExecutionId { get; set; } = string.Empty;
    [Id(1)] public string StreamProvider { get; set; } = string.Empty;
    [Id(2)] public string StreamNamespace { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ResultStreamsStateLogEvent : StateLogEventBase<ResultStreamsStateLogEvent>;

public interface IResultGAgentStreams : IStateGAgent<ResultGAgentStreamsState>
{
    Task SetExecutionContextAsync(string executionId, string streamProvider, string streamNamespace);
}

[GAgent]
public class ResultGAgentStreams : GAgentBase<ResultGAgentStreamsState, ResultStreamsStateLogEvent>, IResultGAgentStreams
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for collecting GAgent's event handler execution results with Streams support.");
    }

    public async Task SetExecutionContextAsync(string executionId, string streamProvider, string streamNamespace)
    {
        RaiseEvent(new SetExecutionContextStateLogEvent 
        { 
            ExecutionId = executionId,
            StreamProvider = streamProvider,
            StreamNamespace = streamNamespace
        });
        await ConfirmEvents();
    }

    [AllEventHandler]
    public async Task OnResultEvent(EventWrapperBase eventWrapper)
    {
        if (eventWrapper is not EventWrapper<EventBase> typedWrapper)
        {
            return;
        }

        var result = typedWrapper.Event.ToString()!;
        RaiseEvent(new ResultArrivedStreamsStateLogEvent
        {
            Result = result
        });
        await ConfirmEvents();

        // Notify result to Orleans Streams
        if (!string.IsNullOrEmpty(State.ExecutionId) && 
            !string.IsNullOrEmpty(State.StreamProvider) && 
            !string.IsNullOrEmpty(State.StreamNamespace))
        {
            try
            {
                var streamProvider = this.GetStreamProvider(State.StreamProvider);
                var resultStream = streamProvider.GetStream<ExecutionCompletedEvent>(State.StreamNamespace, State.ExecutionId);
                
                await resultStream.OnNextAsync(new ExecutionCompletedEvent
                {
                    ExecutionId = State.ExecutionId,
                    Result = result
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send result through stream for execution {ExecutionId}", State.ExecutionId);
            }
        }
    }

    protected override void GAgentTransitionState(ResultGAgentStreamsState state,
        StateLogEventBase<ResultStreamsStateLogEvent> @event)
    {
        switch (@event)
        {
            case ResultArrivedStreamsStateLogEvent resultEvent:
                state.Result = resultEvent.Result;
                break;
            case SetExecutionContextStateLogEvent contextEvent:
                state.ExecutionId = contextEvent.ExecutionId;
                state.StreamProvider = contextEvent.StreamProvider;
                state.StreamNamespace = contextEvent.StreamNamespace;
                break;
        }
    }
} 