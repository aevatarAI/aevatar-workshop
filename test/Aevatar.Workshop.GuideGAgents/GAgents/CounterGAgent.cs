using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GuideGAgents;

public interface ICounterGAgent : IStateGAgent<CounterState>
{
    Task IncrementAsync(int amount = 1);
    Task DecrementAsync(int amount = 1);
    Task<int> GetCurrentValueAsync();
}

[GenerateSerializer]
public class CounterState : StateBase
{
    [Id(0)] public int Value { get; set; }
    [Id(1)] public DateTime LastUpdated { get; set; }
    [Id(2)] public List<string> History { get; set; } = new();
}

[GenerateSerializer]
public abstract class CounterStateLogEvent : StateLogEventBase<CounterStateLogEvent>;

[GenerateSerializer]
public class CounterIncrementedEvent : CounterStateLogEvent
{
    [Id(0)] public int Amount { get; init; }
    [Id(1)] public DateTime Timestamp { get; init; }
}

[GenerateSerializer]
public class CounterDecrementedEvent : CounterStateLogEvent
{
    [Id(0)] public int Amount { get; init; }
    [Id(1)] public DateTime Timestamp { get; init; }
}

[GAgent("counter", "workshop")]
public class CounterGAgent : GAgentBase<CounterState, CounterStateLogEvent>, ICounterGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("A simple counter that can increment and decrement values");

    public async Task IncrementAsync(int amount = 1)
    {
        // Raise an event to change state
        RaiseEvent(new CounterIncrementedEvent
        {
            Amount = amount,
            Timestamp = DateTime.UtcNow
        });

        // Confirm the event to apply state changes
        await ConfirmEvents();

        Logger.LogInformation("Counter incremented by {Amount}", amount);
    }

    public async Task DecrementAsync(int amount = 1)
    {
        RaiseEvent(new CounterDecrementedEvent
        {
            Amount = amount,
            Timestamp = DateTime.UtcNow
        });

        await ConfirmEvents();

        Logger.LogInformation("Counter decremented by {Amount}", amount);
    }

    public Task<int> GetCurrentValueAsync()
        => Task.FromResult(State.Value);

    // Handle state transitions
    protected override void GAgentTransitionState(CounterState state, StateLogEventBase<CounterStateLogEvent> @event)
    {
        switch (@event)
        {
            case CounterIncrementedEvent e:
                state.Value += e.Amount;
                state.LastUpdated = e.Timestamp;
                state.History.Add($"Incremented by {e.Amount} at {e.Timestamp}");
                break;

            case CounterDecrementedEvent e:
                state.Value -= e.Amount;
                state.LastUpdated = e.Timestamp;
                state.History.Add($"Decremented by {e.Amount} at {e.Timestamp}");
                break;
        }
    }
}