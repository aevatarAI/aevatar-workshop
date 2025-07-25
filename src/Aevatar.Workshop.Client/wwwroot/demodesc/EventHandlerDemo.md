# GAgent Event Handler Demo

## Overview

The Event Handler Demo showcases the event-driven architecture of GAgents in the Aevatar framework. This demo illustrates how GAgents can handle events, communicate with each other, and maintain state through event sourcing.

## Key Concepts

### 1. Event Handlers
GAgents can define event handlers in two ways:
- **[EventHandler] Attribute**: Explicitly marks a method as an event handler
- **Method Name Convention**: Methods named `HandleEventAsync` are automatically recognized

### 2. Event Types
The demo includes several event types:
- **NotificationEvent**: For sending notifications with different severity levels
- **DataProcessingEvent**: For triggering data processing tasks with priority support
- **CoordinationRequestEvent**: For coordinating multiple GAgents to work together
- **EventLoggedEvent**: For logging event processing activities

### 3. PublishingGAgent - The Event Hub
The demo uses `PublishingGAgent` as the central parent agent:
```csharp
public interface IPublishingGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}
```
- Acts as the parent for all demo GAgents
- Forwards events to all registered children
- Provides a centralized point for event distribution

### 4. Demo GAgents

#### NotificationGAgent
- Demo GAgent for handling and logging notification events
- Maintains notification history
- Tracks notification statistics by level
- Triggers data processing for error notifications

#### ProcessingGAgent
- Demo GAgent for handling data processing tasks with priority queue support
- Simulates asynchronous task processing
- Sends completion notifications
- Demonstrates both [EventHandler] and method name conventions

#### CoordinatorDemoGAgent
- Demo GAgent for coordinating multiple GAgents to work together
- Manages task acceptance from participating agents
- Tracks coordination success rates
- Demonstrates inter-agent communication

#### EventLoggerGAgent
- Demo GAgent for logging and analyzing all events in the system
- Records all events in the system using [AllEventHandler]
- Can be registered as a child to any agent to log its events
- When registered to PublishingGAgent, it sees all events flowing through the system
- Provides event search and filtering
- Calculates event statistics

## Features

### Interactive UI
- **GAgent Selection**: Click on any GAgent to see its event handlers and statistics
- **Event Triggers**: Manually trigger different types of events
- **Real-time Updates**: Event log updates automatically
- **Scenario Simulation**: Run a pre-configured scenario to see agents in action

### Event Flow Visualization
1. Trigger an event from the UI
2. Event is published to subscribed GAgents
3. Each GAgent processes the event according to its handlers
4. Processing results trigger new events
5. EventLogger records all activities

### State Management
- Each GAgent maintains its own state
- State is updated through event sourcing
- Statistics are calculated from accumulated state

## Usage

1. **Initialize Demo**: Click "Initialize Demo" to create and set up all demo GAgents
   - Creates a `PublishingGAgent` as the parent
   - Registers all demo GAgents (`NotificationGAgent`, `ProcessingGAgent`, `CoordinatorDemoGAgent`) as children
   - `EventLoggerGAgent` subscribes to other agents to log their events
2. **Select a GAgent**: Click on any GAgent card to view its details
3. **Trigger Events**: Use the event trigger forms to send events
   - Events are published through the parent `PublishingGAgent`
   - The parent forwards events to all registered children
   - Children can also publish events upward to the parent
4. **Run Scenario**: Click "Simulate Scenario" for an automated demonstration
5. **Monitor Activity**: Watch the event log for real-time updates

## Technical Details

### GAgent Implementation Structure

According to the best practices, all GAgent-related code is kept in a single file:

```csharp
// File: src/GAgents/NotificationGAgent.cs

// Interface definition (required)
public interface INotificationGAgent : IStateGAgent<NotificationGAgentState>
{
    Task<NotificationStatistics> GetStatisticsAsync();
}

// State definition
[GenerateSerializer]
public class NotificationGAgentState : StateBase
{
    [Id(0)] public List<NotificationRecord> NotificationHistory { get; set; } = new();
    [Id(1)] public int TotalNotifications { get; set; }
}

// GAgent implementation
[GAgent("notification-demo", "workshop")]
public class NotificationGAgent : GAgentBase<NotificationGAgentState, NotificationStateLogEvent>, INotificationGAgent
{
    // State log events as nested classes
    [GenerateSerializer]
    public class NotificationStateLogEvent : StateLogEventBase<NotificationStateLogEvent> { }
    
    // Event handlers...
}
```

### Event Handler Patterns

```csharp
// Explicit attribute
[EventHandler]
public async Task HandleNotificationAsync(NotificationEvent @event) { }

// Method name convention
public Task HandleEventAsync(EventBase @event) { }

// Handle all events
[AllEventHandler]
public Task LogAllEventsAsync(EventWrapperBase eventWrapper) { }
```

### File Organization

Events and GAgents are organized in separate directories:

```
src/
├── Events/                    # Shared events (EventBase derived)
│   ├── NotificationEvent.cs
│   ├── DataProcessingEvent.cs
│   ├── CoordinationRequestEvent.cs
│   └── EventLoggedEvent.cs
└── GAgents/                   # GAgent implementations (all-in-one files)
    ├── NotificationGAgent.cs
    ├── ProcessingGAgent.cs
    ├── CoordinatorDemoGAgent.cs
    └── EventLoggerGAgent.cs
```

### Event Publishing

Events can be published in several ways:

```csharp
// 1. From within a GAgent (publishes to parent and self)
await PublishAsync(new NotificationEvent { 
    Title = "Task Complete",
    Message = "Processing finished successfully"
});

// 2. Through the PublishingGAgent parent (broadcasts to all children)
var publishingAgent = await gAgentFactory.GetGAgentAsync<IPublishingGAgent>(publishingAgentId);
await publishingAgent.PublishEventAsync(new DataProcessingEvent {
    DataType = "UserData",
    Priority = ProcessingPriority.High
});

// 3. Direct event publishing with specific targets
await PublishEventAsync(myEvent, targetGAgent);
```

### State Management with Event Sourcing

GAgents must use event sourcing for state management:
```csharp
// Step 1: Define state change events
[GenerateSerializer]
public class NotificationAddedEvent : NotificationStateLogEvent
{
    [Id(0)] public NotificationRecord Record { get; set; } = null!;
}

// Step 2: Raise events instead of direct state modification
public async Task HandleNotificationAsync(NotificationEvent @event)
{
    var record = new NotificationRecord { /* ... */ };
    
    // Don't do this: State.NotificationHistory.Add(record);
    // Do this instead:
    RaiseEvent(new NotificationAddedEvent { Record = record });
    await ConfirmEvents();
}

// Step 3: Apply state changes in GAgentTransitionState
protected override void GAgentTransitionState(NotificationGAgentState state, StateLogEventBase<NotificationStateLogEvent> @event)
{
    switch (@event)
    {
        case NotificationAddedEvent e:
            state.NotificationHistory.Add(e.Record);
            state.TotalNotifications++;
            break;
    }
}
```

### Parent-Child Relationships and Event Flow

GAgents use a hierarchical parent-child structure for event communication:

#### Establishing Relationships
```csharp
// Register child agents with a parent
await parentAgent.RegisterAsync(childAgent1);
await parentAgent.RegisterAsync(childAgent2);

// Or register multiple at once
await parentAgent.RegisterManyAsync(new List<IGAgent> { child1, child2, child3 });
```

When you call `RegisterAsync`:
1. The child is added to the parent's `State.Children` list
2. The child's `State.Parent` is set to the parent's GrainId
3. The child automatically subscribes to the parent's event stream

#### Event Flow Directions

**1. Upward Event Publishing (Child → Parent)**
```csharp
// In a child GAgent, publish event to parent
await PublishAsync(new NotificationEvent { 
    Title = "Task Complete",
    Message = "Processing finished"
});
// This event goes up to the parent
```

**2. Downward Event Forwarding (Parent → Children)**
```csharp
// Parent GAgents with [AllEventHandler] automatically forward events to children
[AllEventHandler(allowSelfHandling: true)]
protected virtual async Task ForwardEventAsync(EventWrapperBase eventWrapper)
{
    // Events are automatically forwarded to all children
    await SendEventDownwardsAsync(eventWrapper);
}
```

**3. Targeted Event Publishing**
```csharp
// Parent can publish events that will be received by all children
await parentAgent.PublishEventAsync(new CoordinationRequestEvent {
    TaskName = "Process Data",
    RequiredAgents = new List<string> { "agent1", "agent2" }
});
```

#### Event Flow Example
```
                  ParentGAgent
                  /     |     \
               /        |        \
         Child1     Child2      Child3
           ↑           ↑           ↑
           └───────────┴───────────┘
          Events flow up from children
                     and
          Events flow down from parent
```

#### Unregistering and Cleanup
```csharp
// Remove a child from parent
await parentAgent.UnregisterAsync(childAgent);

// This will:
// 1. Remove child from parent's State.Children
// 2. Clear child's State.Parent
// 3. Unsubscribe child from parent's event stream
```

## Implementation Best Practices

### Key Rules for GAgent Development

1. **Interface First**: Always define an interface before implementing a GAgent
2. **No Constructor Parameters**: GAgents must have parameterless constructors
3. **Built-in Logger**: Use the `Logger` property, never inject ILogger
4. **GAgent Factory**: Use `IGAgentFactory`, not `IGrainFactory`
5. **Event Sourcing**: Always use `RaiseEvent()` + `ConfirmEvents()` for state changes

### Service Access Pattern

```csharp
// Correct way to access services
private IGAgentFactory GAgentFactory => 
    ServiceProvider.GetRequiredService<IGAgentFactory>();

// Use it directly
var agent = await GAgentFactory.GetGAgentAsync<IOtherGAgent>(id);
```

## Benefits

- **Loose Coupling**: GAgents communicate through events without direct dependencies
- **Scalability**: Event-driven architecture scales naturally with Orleans
- **Resilience**: Event sourcing provides natural recovery mechanisms
- **Flexibility**: Easy to add new event types and handlers
- **Observability**: All events can be logged and monitored

This demo provides a foundation for understanding how to build complex, event-driven systems using the Aevatar framework's GAgent architecture. 