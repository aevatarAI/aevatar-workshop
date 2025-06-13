# Event Handlers in Aevatar: `[EventHandler]` & `[AllEventHandler]`

## Introduction

Event handlers are a cornerstone of the Aevatar framework's event-driven model. They allow GAgents to react to domain events, trigger workflows, and coordinate distributed logic. In Aevatar, **event handler events** are distinct from **event sourcing events**: the former are messages that flow between agents and should inherit from `EventBase`, while the latter are used for state mutation and may inherit from `StateLogEventBase<T>`.

For a broader context, see the [Overview](./00-overview.md) and [GAgent documentation](./01-gagent.md).

---

## Why Event Handlers Matter

- **Business Logic Encapsulation:** Event handlers let you define how your agent responds to domain events, keeping business logic modular and testable.
- **Distributed Communication:** Handlers process events sent from other agents, enabling decoupled, scalable workflows.
- **State Change Triggers:** Handlers can raise new events (including event sourcing events) to mutate state in a durable, auditable way.

---

## How Event Handlers Are Discovered

Aevatar uses reflection (see `GAgentBase.Observers.cs`) to automatically discover methods decorated with `[EventHandler]` or `[AllEventHandler]` attributes. When an event is received, the framework routes it to the appropriate handler method based on the event type.

- **[EventHandler]:** Marks a method as the handler for a specific event type (inheriting from `EventBase`).
- **[AllEventHandler]:** Marks a method that can handle all events (useful for logging, auditing, or generic processing).

**Discovery Logic:**
- The framework scans all methods in your GAgent for these attributes.
- When an event arrives, it matches the event type (must inherit from `EventBase`) to the handler's parameter type.
- If multiple handlers match, all are invoked.
- If no specific handler is found, an `[AllEventHandler]` method (if present) is called.

---

## Defining an Event Handler

### 1. Define Your Event Type (for Event Handlers)

Event handler events should inherit from `EventBase`. This ensures they are compatible with the event routing and pub/sub system.

```csharp
public class MyDomainEvent : EventBase {
    public string Data { get; set; }
}
```

### 2. Implement the Handler Method

- Decorate the method with `[EventHandler]`.
- The method must accept a single parameter of your event type (inheriting from `EventBase`).
- The method can be `async` or synchronous.

```csharp
[EventHandler]
public async Task HandleMyDomainEventAsync(MyDomainEvent evt)
{
    // Business logic here
    // Optionally raise new events or update state
}
```

### 3. Using AllEventHandler

If you want to handle all events generically (e.g., for logging):

```csharp
[AllEventHandler]
public Task HandleAllEventsAsync(EventBase evt)
{
    // This will be called for any event if no specific handler is found
    return Task.CompletedTask;
}
```

---

## Persisting State Changes with RaiseEvent (Event Sourcing)

To change the GAgent's state in a durable, event-sourced way, use `RaiseEvent` inside your handler. **Note:** The event you pass to `RaiseEvent` should typically inherit from `StateLogEventBase<T>`, not `EventBase`.

```csharp
[EventHandler]
public async Task HandleMyDomainEventAsync(MyDomainEvent evt)
{
    // Apply business logic
    var stateEvent = new MyStateLogEvent { ... };
    RaiseEvent(stateEvent); // This will persist the event and trigger a state transition
    await ConfirmEvents(); // Ensures the event is committed
}
```

- **RaiseEvent:** Persists the event (of type `StateLogEventBase<T>`) and schedules a state transition.
- **ConfirmEvents:** Commits the event, making the state change durable.

The state transition logic should be implemented in your override of `GAgentTransitionState`:

```csharp
protected override void GAgentTransitionState(MyAgentState state, StateLogEventBase<MyStateLogEvent> evt)
{
    if (evt is MyStateLogEvent myEvent)
    {
        state.SomeProperty = myEvent.Data;
    }
}
```

---

## Best Practices

- Always use strongly-typed event classes for clarity and safety.
- Event handler events should inherit from `EventBase`.
- State mutation events (for event sourcing) should inherit from `StateLogEventBase<T>`.
- Keep handler methods focused—one event, one responsibility.
- Use `[AllEventHandler]` sparingly for cross-cutting concerns.
- Always call `RaiseEvent` and `ConfirmEvents` to ensure state changes are persisted.
- Document your event types and handlers for maintainability.

---

## Further Reading

- [GAgent Documentation](./01-gagent.md)
- [Aevatar Overview](./00-overview.md)
- [Event System Deep Dive](https://deepwiki.com/aevatarAI/aevatar-framework/2.2-event-system)

---

By following these patterns, you can build robust, maintainable, and scalable event-driven agents with Aevatar. 