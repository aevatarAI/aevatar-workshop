using Aevatar.Core;
using Aevatar.Core.Abstractions;
using System.Collections.Concurrent;

namespace Aevatar.Workshop.GAgentPlugin;

[GenerateSerializer]
public class ResultGAgentState : StateBase
{
    [Id(0)] public string? Result { get; set; }
    [Id(1)] public string? ExecutionId { get; set; }
}

[GenerateSerializer]
public class ResultArrivedGAgentStateLogEvent : ResultStateLogEvent
{
    [Id(0)] public string Result { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ClearResultStateLogEvent : ResultStateLogEvent;

[GenerateSerializer]
public class SetExecutionIdStateLogEvent : ResultStateLogEvent
{
    [Id(0)] public string ExecutionId { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ResultStateLogEvent : StateLogEventBase<ResultStateLogEvent>;

public interface IResultGAgent : IStateGAgent<ResultGAgentState>
{
    Task ClearResultAsync();
    Task SetExecutionIdAsync(string executionId);
}

[GAgent]
public class ResultGAgent : GAgentBase<ResultGAgentState, ResultStateLogEvent>, IResultGAgent
{
    private static readonly ConcurrentDictionary<string, Action<string>> _completionCallbacks = new();

    public static void RegisterCompletionCallback(string executionId, Action<string> callback)
    {
        _completionCallbacks[executionId] = callback;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for collecting GAgent's event handler execution results.");
    }

    public async Task ClearResultAsync()
    {
        RaiseEvent(new ClearResultStateLogEvent());
        await ConfirmEvents();
    }

    public async Task SetExecutionIdAsync(string executionId)
    {
        RaiseEvent(new SetExecutionIdStateLogEvent { ExecutionId = executionId });
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
        RaiseEvent(new ResultArrivedGAgentStateLogEvent
        {
            Result = result
        });
        await ConfirmEvents();

        // 通知等待的 TaskCompletionSource
        if (!string.IsNullOrEmpty(State.ExecutionId) && 
            _completionCallbacks.TryRemove(State.ExecutionId, out var callback))
        {
            callback(result);
        }
    }

    protected override void GAgentTransitionState(ResultGAgentState state,
        StateLogEventBase<ResultStateLogEvent> @event)
    {
        switch (@event)
        {
            case ResultArrivedGAgentStateLogEvent resultEvent:
                state.Result = resultEvent.Result;
                break;
            case ClearResultStateLogEvent:
                state.Result = null;
                state.ExecutionId = null;
                break;
            case SetExecutionIdStateLogEvent setExecutionIdEvent:
                state.ExecutionId = setExecutionIdEvent.ExecutionId;
                break;
        }
    }
}