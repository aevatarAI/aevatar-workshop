# Aevatar Framework Fundamentals - Developer Guide

Welcome to the **Aevatar** framework! This guide introduces the core concepts for building distributed multi-agent systems.

## 🎯 Overview

Aevatar is a distributed multi-agent framework built on the **Orleans Virtual Actor** model, designed for building scalable, high-concurrency intelligent agent systems. If you're familiar with C# and basic Agent concepts, you already have the foundation to get started with Aevatar.

### Core Advantages
- ✅ **High Concurrency**: Native support for massive concurrency through Orleans Virtual Actor model
- ✅ **Auto Scaling**: No manual instance management - Orleans handles load balancing automatically
- ✅ **Event-Driven**: Loosely coupled agent communication via Orleans Streaming
- ✅ **State Persistence**: Reliable state management through Event Sourcing

---

## 🤖 1. GAgent - The Heart of Intelligent Agents

### What is a GAgent?

**GAgent** is the foundational abstraction for intelligent agents in Aevatar. Each GAgent is an independent, stateful computational unit, like a "digital worker" that can:

- 🧠 **Think Independently**: Process business logic and make decisions
- 💾 **Remember Information**: Maintain its own state data
- 📡 **Communicate with Others**: Collaborate with other GAgents through events
- ⚡ **Work Concurrently**: Handle multiple tasks simultaneously


```csharp
// Define a simple GAgent
[GAgent("calculator", "math")]
public class CalculatorGAgent : GAgentBase<CalculatorState, CalculatorStateLogEvent>, ICalculatorGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("An intelligent agent capable of mathematical calculations");
    
    // GAgent business logic
    public async Task<double> CalculateAsync(string expression)
    {
        var result = EvaluateExpression(expression);
        
        // Update state through events
        RaiseEvent(new CalculationPerformedEvent 
        { 
            Expression = expression, 
            Result = result 
        });
        
        await ConfirmEvents();
        return result;
    }
}
```

### Orleans Virtual Actor Model

GAgents are built on the **Orleans Virtual Actor** model, which means:

1. **Location Transparency**: You don't need to worry about which server the GAgent runs on
2. **Auto Activation**: Orleans automatically creates and destroys instances as needed
3. **Single-threaded Guarantee**: Each GAgent instance processes one request at a time, avoiding concurrency issues
4. **Fault Tolerance**: System automatically handles node failures and load balancing

> 📚 **Learn More**: [Orleans Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/) - Official Virtual Actor model documentation

### Concurrency and Scaling

Thanks to Orleans foundation, Aevatar naturally supports:

- **Horizontal Scaling**: Add more server nodes to increase processing capacity
- **Smart Load Balancing**: Orleans automatically distributes GAgents to optimal nodes
- **Elastic Scaling**: Automatically adjust resource usage based on load

---

## 📡 2. Event Handler - Inter-Agent Communication

### Event-Driven Architecture

In Aevatar, GAgents primarily communicate through **Events** rather than direct calls. This pattern is called **Event-Driven Architecture**.

### Defining and Using Event Handlers

#### 1. Define Events

```csharp
// Define an event
[GenerateSerializer]
public record CalculationRequestEvent : EventBase
{
    [Id(0)] public string Expression { get; init; } = string.Empty;
    [Id(1)] public string RequestId { get; init; } = string.Empty;
}
```

#### 2. Implement Event Handler

```csharp
public class CalculatorGAgent : GAgentBase<CalculatorState, CalculatorStateLogEvent>
{
    // Mark event handling methods with [EventHandler] attribute
    [EventHandler]
    public async Task HandleCalculationRequestAsync(CalculationRequestEvent request)
    {
        Logger.LogInformation("Received calculation request: {Expression}", request.Expression);
        
        // Execute calculation logic
        var result = await CalculateAsync(request.Expression);
        
        // Publish result event
        await PublishAsync(new CalculationCompletedEvent 
        { 
            RequestId = request.RequestId,
            Result = result 
        });
    }
}
```

#### 3. Publish Events

```csharp
// Send events from one GAgent to others
await PublishAsync(new CalculationRequestEvent 
{ 
    Expression = "2 + 3 * 4",
    RequestId = Guid.NewGuid().ToString()
});
```

### Orleans Streaming Infrastructure

Aevatar's event system is built on **Orleans Streaming**:

- **Reliable Delivery**: Ensures events are never lost
- **Ordering Guarantee**: Events from the same GAgent are processed in order
- **Backpressure Control**: Automatically handles consumer capacity issues
- **Persistent Streams**: Supports event persistence

> 📚 **Learn More**: [Orleans Streams Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/streaming/) - Official Orleans streaming documentation

### Event Handler Benefits

1. **Loose Coupling**: GAgents don't need direct dependencies on each other
2. **Asynchronous Processing**: Events are delivered asynchronously, improving system responsiveness
3. **Extensibility**: Easy to add new event handling logic
4. **Observability**: Event streams provide clear system behavior traces

---

## 💾 3. Event Sourcing - The Art of State Management

### What is Event Sourcing?

**Event Sourcing** is a state management pattern with the core principle: **Instead of storing current state directly, store the sequence of events that led to state changes**.

### Traditional Approach vs Event Sourcing

```csharp
// ❌ Traditional approach (avoid)
public void UpdateBalance(decimal amount)
{
    State.Balance += amount;  // Direct state modification
}

// ✅ Event Sourcing approach
public async Task DepositAsync(decimal amount)
{
    // 1. Create event
    RaiseEvent(new MoneyDepositedEvent { Amount = amount });
    
    // 2. Commit event
    await ConfirmEvents();
}

// 3. Apply event in state transition method
protected override void GAgentTransitionState(AccountState state, StateLogEventBase<AccountStateLogEvent> @event)
{
    switch (@event)
    {
        case MoneyDepositedEvent depositEvent:
            state.Balance += depositEvent.Amount;
            state.LastTransactionTime = DateTime.UtcNow;
            break;
    }
}
```

### Complete Event Sourcing Flow

#### 1. Define State Class

```csharp
[GenerateSerializer]
public class AccountState : StateBase
{
    [Id(0)] public decimal Balance { get; set; }
    [Id(1)] public DateTime LastTransactionTime { get; set; }
    [Id(2)] public List<string> TransactionHistory { get; set; } = new();
}
```

#### 2. Define State Log Events

```csharp
[GenerateSerializer]
public abstract record AccountStateLogEvent : StateLogEventBase<AccountStateLogEvent>;

[GenerateSerializer]
public record MoneyDepositedEvent : AccountStateLogEvent
{
    [Id(0)] public decimal Amount { get; init; }
}

[GenerateSerializer]
public record MoneyWithdrawnEvent : AccountStateLogEvent
{
    [Id(0)] public decimal Amount { get; init; }
}
```

#### 3. Implement State Transition Logic

```csharp
protected override void GAgentTransitionState(AccountState state, StateLogEventBase<AccountStateLogEvent> @event)
{
    switch (@event)
    {
        case MoneyDepositedEvent deposit:
            state.Balance += deposit.Amount;
            state.TransactionHistory.Add($"Deposit: {deposit.Amount:C}");
            state.LastTransactionTime = DateTime.UtcNow;
            break;
            
        case MoneyWithdrawnEvent withdrawal:
            state.Balance -= withdrawal.Amount;
            state.TransactionHistory.Add($"Withdrawal: {withdrawal.Amount:C}");
            state.LastTransactionTime = DateTime.UtcNow;
            break;
    }
}
```

### Event Sourcing Benefits

1. **Complete Audit Trail**: Every state change is fully recorded
2. **Time Travel**: Replay event sequences to view state at any point in time
3. **Debug Friendly**: Complete system behavior tracking through event sequences
4. **Data Consistency**: Avoids data inconsistency from concurrent modifications

> 📚 **Learn More**: [Event Sourcing Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing) - Microsoft's official Event Sourcing pattern documentation

---

## 🔄 Complete Workflow

Let's understand how these three concepts work together through a complete example:

```csharp
// 1. Smart home light control GAgent
[GAgent("light", "smarthome")]
public class LightGAgent : GAgentBase<LightState, LightStateLogEvent>, ILightGAgent
{
    // 2. Event Handler - process turn on light events
    [EventHandler]
    public async Task HandleTurnOnLightAsync(TurnOnLightEvent lightEvent)
    {
        Logger.LogInformation("Received turn on light command: {Location}", lightEvent.Location);
        
        // 3. Event Sourcing - update state through events
        RaiseEvent(new LightTurnedOnEvent 
        { 
            Location = lightEvent.Location,
            Brightness = lightEvent.Brightness ?? 100
        });
        
        await ConfirmEvents();
        
        // 4. Publish state change events to other GAgents
        await PublishAsync(new LightStateChangedEvent 
        { 
            Location = lightEvent.Location,
            IsOn = true,
            Brightness = lightEvent.Brightness ?? 100
        });
    }
    
    // State transition logic
    protected override void GAgentTransitionState(LightState state, StateLogEventBase<LightStateLogEvent> @event)
    {
        switch (@event)
        {
            case LightTurnedOnEvent turnedOn:
                state.IsOn = true;
                state.Brightness = turnedOn.Brightness;
                state.LastModified = DateTime.UtcNow;
                break;
        }
    }
}
```

### Execution Flow:

1. **Event Trigger**: AI GAgent sends `TurnOnLightEvent`
2. **Event Handler Response**: Light GAgent's `HandleTurnOnLightAsync` is called
3. **Event Sourcing Update**: Update internal state through `LightTurnedOnEvent`
4. **State Broadcast**: Publish `LightStateChangedEvent` to notify other interested GAgents

---

## 🏗️ Best Practices

### 1. GAgent Design Principles
- **Single Responsibility**: Each GAgent focuses on one business domain
- **Stateless Methods**: Business methods should not directly modify state
- **Async First**: Use `async/await` for all I/O operations

### 2. Event Handler Best Practices
- **Idempotency**: Ensure repeated processing of the same event doesn't cause side effects
- **Fast Processing**: Event Handlers should complete quickly to avoid blocking
- **Error Handling**: Properly handle exceptions to avoid affecting other events

### 3. Event Sourcing Considerations
- **Event Immutability**: Once created, event content should not be modified
- **Backward Compatibility**: New event structures should be compatible with old versions
- **Appropriate Granularity**: Event granularity should be balanced - neither too fine nor too coarse

---

## 🎮 Hands-On Experience

Want to experience these concepts firsthand? Visit our **[Smart Home Demo](http://localhost:5000/demos/smart-home-demo.html)**:

1. 🏠 Observe how AI GAgent understands user commands
2. 📡 Watch events flow between different device GAgents
3. 💾 Experience how Event Sourcing updates device states
4. 🔄 Understand the complete event-driven architecture flow

> 💡 **Tip**: Even without configuring an LLM API Key, you can manually operate devices to observe GAgent event interactions!

---

## 📚 Glossary

| Term | English | Description |
|------|------|------|
| **GAgent** | Generic Agent | The foundational intelligent agent class in Aevatar framework, encapsulating state management and event handling |
| **Virtual Actor** | Virtual Actor | Core concept of Orleans framework providing location-transparent distributed object model |
| **Event Handler** | Event Handler | Methods that handle specific event types, marked with `[EventHandler]` attribute |
| **Event Sourcing** | Event Sourcing | Pattern for managing data by storing event sequences rather than direct state |
| **State Transition** | State Transition | Process of transforming current state to new state based on events |
| **Orleans Streaming** | Orleans Streaming | Orleans framework's streaming infrastructure for reliable asynchronous messaging |
| **Grain** | Grain | Concrete implementation of Virtual Actor in Orleans, foundation for GAgents |
| **Event Log** | Event Log | Log storing all state change events for state reconstruction and auditing |
| **State Log Event** | State Log Event | Internal events dedicated to state management, different from external GAgent communication events |
| **Event Sourcing Pattern** | Event Sourcing Pattern | Architectural pattern storing data as event sequences rather than final state |
| **Immutable Event** | Immutable Event | Events that cannot be modified once created, ensuring historical record integrity |
| **Event Replay** | Event Replay | Re-executing event sequences to rebuild state at specific points in time |

---

## 🔗 Further Reading

### Orleans Related
- [Orleans Official Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/) - Complete Orleans Virtual Actor framework documentation
- [Orleans Streams](https://docs.microsoft.com/en-us/dotnet/orleans/streaming/) - Orleans streaming documentation

### Event Sourcing
- [Event Sourcing Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing) - Microsoft architecture guidance
- [Event Sourcing by Martin Fowler](https://martinfowler.com/eaaDev/EventSourcing.html) - Martin Fowler's classic article

### Distributed Systems
- [Azure Architecture Guide](https://learn.microsoft.com/en-us/azure/architecture/guide/) - Azure application architecture fundamentals
- [Actor Model](https://en.wikipedia.org/wiki/Actor_model) - Theoretical foundation of Actor model

---

**Ready to start your Aevatar journey?** 

Begin with the Smart Home Demo to experience how these concepts work together! 🚀 