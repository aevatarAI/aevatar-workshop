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

### 3. Demo GAgents

#### NotificationGAgent
- Handles notification events
- Maintains notification history
- Tracks notification statistics by level
- Triggers data processing for error notifications

#### ProcessingGAgent
- Processes data with priority queue support
- Simulates asynchronous task processing
- Sends completion notifications
- Demonstrates both [EventHandler] and method name conventions

#### CoordinatorDemoGAgent
- Coordinates multiple GAgents for complex tasks
- Manages task acceptance from participating agents
- Tracks coordination success rates
- Demonstrates inter-agent communication

#### EventLoggerGAgent
- Records all events in the system using [AllEventHandler]
- Provides event search and filtering
- Calculates event statistics
- Shows how to handle all events globally

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
2. **Select a GAgent**: Click on any GAgent card to view its details
3. **Trigger Events**: Use the event trigger forms to send events
4. **Run Scenario**: Click "Simulate Scenario" for an automated demonstration
5. **Monitor Activity**: Watch the event log for real-time updates

## Technical Details

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

### Event Publishing

GAgents can publish events to their subscribers:
```csharp
await PublishAsync(new NotificationEvent { 
    Title = "Task Complete",
    Message = "Processing finished successfully"
});
```

### Subscription Management

GAgents can subscribe to other GAgents:
```csharp
await agent1.SubscribeToAsync(agent2);
```

## Benefits

- **Loose Coupling**: GAgents communicate through events without direct dependencies
- **Scalability**: Event-driven architecture scales naturally with Orleans
- **Resilience**: Event sourcing provides natural recovery mechanisms
- **Flexibility**: Easy to add new event types and handlers
- **Observability**: All events can be logged and monitored

This demo provides a foundation for understanding how to build complex, event-driven systems using the Aevatar framework's GAgent architecture. 