# GAgent Development Rules

## Part 1: GAgent Implementation Rules

When implementing a new GAgent class, follow these rules:

### 1.1 Define Interface First
```csharp
// For regular GAgent (no AI)
public interface IMyGAgent : IStateGAgent<MyState>
{
    // Add custom methods if needed
}

// For AI-enabled GAgent
public interface IMyAIGAgent : IStateGAgent<MyAIState>, IAIGAgent
{
    // Add custom methods if needed
}
```

### 1.2 Basic Structure
```csharp
using Aevatar.Core;
using Aevatar.Core.Abstractions;

[GAgent("my-agent", "workshop")]  // Required attribute
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    // NO constructor with parameters!
    
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Detailed description of this GAgent");
}
```

### 1.3 NEVER Do These
```csharp
// ❌ NEVER inject logger
public MyGAgent(ILogger<MyGAgent> logger) { }

// ❌ NEVER modify state directly  
State.Counter++;

// ❌ NEVER use IGrainFactory
var grainFactory = ServiceProvider.GetRequiredService<IGrainFactory>();
```

### 1.4 Service Access Pattern
```csharp
// Simple lazy-loaded service property
private IGAgentFactory GAgentFactory => 
    ServiceProvider.GetRequiredService<IGAgentFactory>();

// Use it directly
var agent = await GAgentFactory.GetGAgentAsync<IOtherGAgent>(id);
```

### 1.5 State Management (Orleans Event Sourcing)
```csharp
// Step 1: Define state
[GenerateSerializer]
public class MyState : StateBase
{
    [Id(0)] public int Counter { get; set; }
    [Id(1)] public List<string> Items { get; set; } = new(); // Always initialize!
}

// Step 2: Define state change events
[GenerateSerializer]
public class MyStateLogEvent : StateLogEventBase<MyStateLogEvent> { }

[GenerateSerializer]
public class CounterIncrementedEvent : MyStateLogEvent
{
    [Id(0)] public int Amount { get; set; }
}

// Step 3: Raise events to change state
public async Task IncrementAsync(int amount)
{
    RaiseEvent(new CounterIncrementedEvent { Amount = amount });
    await ConfirmEvents();
}

// Step 4: Apply state changes in GAgentTransitionState
protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
{
    switch (@event)
    {
        case CounterIncrementedEvent e:
            state.Counter += e.Amount;
            break;
    }
}
```

### 1.6 Event Handler Types
```csharp
// Type 1: Attribute-based handler
[EventHandler]
public async Task HandleMyEventAsync(MyEvent @event)
{
    Logger.LogInformation("Handling: {EventId}", @event.Id);
}

// Type 2: All events handler
[AllEventHandler(allowSelfHandling: false)]
public Task HandleAllEventsAsync(EventWrapperBase eventWrapper)
{
    Logger.LogDebug("Received event");
    return Task.CompletedTask;
}

// Type 3: Convention-based (method name must be "HandleEventAsync")
public async Task HandleEventAsync(MyConcreteEvent @event)
{
    // Automatically recognized as handler
}
```

### 1.7 GAgent Attribute Rules
- Required: `[GAgent]` or `[GAgent("alias")]` or `[GAgent("alias", "namespace")]`
- Without parameters: uses `type.Namespace + "." + type.Name`
- With alias only: uses `type.Namespace + "." + alias`
- With both: uses `namespace + "." + alias`

## Part 2: GAgent Instantiation Rules

When getting/using GAgent instances:

### 2.1 Always Use IGAgentFactory
```csharp
// ✅ CORRECT - Use IGAgentFactory
var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
var agent = await gAgentFactory.GetGAgentAsync<IMyGAgent>(grainId);

// ❌ WRONG - Never use IGrainFactory
var grainFactory = ServiceProvider.GetRequiredService<IGrainFactory>();
```

### 2.2 Event Communication Setup
GAgents must be in same group for event communication:
```csharp
// Setup parent-child relationship
var publisher = await GAgentFactory.GetGAgentAsync<IPublishingGAgent>();
await publisher.RegisterAsync(myAgent);
await publisher.RegisterAsync(otherAgent);

// Now they can communicate via events
await publisher.PublishEventAsync(new MyEvent(), targetAgent);
```
## Quick Checklist

### Implementation Checklist
- [ ] Define interface first (IStateGAgent<TState> or IStateGAgent<TState> + IAIGAgent)
- [ ] No constructor parameters
- [ ] Has [GAgent] attribute
- [ ] Implements GetDescriptionAsync()
- [ ] Uses Logger property (not injected)
- [ ] State changes via RaiseEvent + ConfirmEvents
- [ ] State transitions in GAgentTransitionState
- [ ] Collections initialized in state
- [ ] All classes have [GenerateSerializer]
- [ ] All properties have [Id(n)]

### Instantiation Checklist
- [ ] Using IGAgentFactory (not IGrainFactory)
- [ ] GAgents registered in same group for events
- [ ] Using GetGAgentAsync methods correctly

## File Organization

```
src/
├── Events/                    # Shared events (EventBase derived)
│   ├── NotificationEvent.cs
│   ├── DataProcessingEvent.cs
│   └── CoordinationEvent.cs
└── GAgents/                   # GAgent implementations (all-in-one files)
    ├── MyGAgent.cs            # Contains: interface, state, state log events, implementation
    ├── NotificationGAgent.cs
    ├── ProcessingGAgent.cs
    └── CoordinatorGAgent.cs
```

**Best Practice**: Keep all GAgent-related code in a single file for better readability:
- Interface definition (IMyGAgent)
- State class (MyGAgentState)
- State log events (as nested classes)
- GAgent implementation (MyGAgent) 