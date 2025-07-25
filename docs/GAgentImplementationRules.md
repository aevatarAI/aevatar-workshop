# GAgent Implementation Rules and Best Practices

This document provides comprehensive guidance for implementing GAgents in the Aevatar framework.

## 🚫 Common Mistakes to Avoid

### ❌ DO NOT inject logger in constructor
```csharp
// WRONG
private readonly ILogger<MyGAgent> _logger;
public MyGAgent(ILogger<MyGAgent> logger)
{
    _logger = logger;
}
```

### ✅ Use the built-in Logger property from GAgentBase
```csharp
// CORRECT
Logger.LogInformation("Processing event: {EventType}", eventType);
```

### ❌ DO NOT use IGrainFactory to get GAgents
```csharp
// WRONG
var grainFactory = ServiceProvider.GetRequiredService<IGrainFactory>();
var myAgent = grainFactory.GetGrain<IMyGAgent>(grainId);
```

### ✅ Use IGAgentFactory to get GAgents
```csharp
// CORRECT
var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
var myAgent = await gAgentFactory.GetGAgentAsync<IMyGAgent>(grainId);
```

### ❌ DO NOT modify State directly
```csharp
// WRONG - Direct state modification
State.Counter++;
State.Items.Add(item);
```

### ✅ Use Event Sourcing pattern
```csharp
// CORRECT - Use RaiseEvent and ConfirmEvents
RaiseEvent(new CounterIncrementedEvent { IncrementBy = 1 });
RaiseEvent(new ItemAddedEvent { Item = item });
await ConfirmEvents();
```

## 📋 GAgent Implementation Guide

### 1. Define Interface First (REQUIRED)

Every GAgent MUST have an interface defined before implementation:

```csharp
// For regular GAgent (no AI capabilities)
public interface IMyGAgent : IStateGAgent<MyState>
{
    // Add any custom methods specific to your GAgent
    Task<MyResult> ProcessDataAsync(MyData data);
}

// For AI-enabled GAgent
public interface IMyAIGAgent : IStateGAgent<MyAIState>, IAIGAgent
{
    // IAIGAgent provides AI-related capabilities
    // Add your custom methods
    Task<string> GenerateResponseAsync(string prompt);
}
```

### 2. Basic Structure

```csharp
using Aevatar.Core;
using Aevatar.Core.Abstractions;

[GAgent("my-agent", "workshop")]       // REQUIRED attribute
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    // NO constructor with parameters!
    
    // Simplified lazy-loaded service
    private IGAgentFactory GAgentFactory => 
        ServiceProvider.GetRequiredService<IGAgentFactory>();
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Detailed description of what this GAgent does");
    }
    
    // Implement interface methods
    public async Task<MyResult> ProcessDataAsync(MyData data)
    {
        // Implementation
    }
}
```

### 3. GAgent Attribute Rules

The `[GAgent]` attribute is **REQUIRED** but its parameters are optional:

```csharp
// No parameters - uses type.Namespace + "." + type.Name
[GAgent]
public class MyGAgent  // Will be: "MyNamespace.MyGAgent"

// Only alias - uses type.Namespace + "." + alias
[GAgent("my-agent")]
public class MyGAgent  // Will be: "MyNamespace.my-agent"

// Both alias and namespace - uses namespace + "." + alias
[GAgent("my-agent", "custom-ns")]
public class MyGAgent  // Will be: "custom-ns.my-agent"
```

### 4. State Definition

```csharp
[GenerateSerializer]
public class MyState : StateBase
{
    [Id(0)] public string Property1 { get; set; } = string.Empty;
    [Id(1)] public int Counter { get; set; }
    // Always initialize collections!
    [Id(2)] public List<string> Items { get; set; } = new();
    [Id(3)] public Dictionary<string, int> Metrics { get; set; } = new();
}
```

### 5. State Log Event

```csharp
[GenerateSerializer]
public class MyStateLogEvent : StateLogEventBase<MyStateLogEvent>
{
}

// Define specific state change events
[GenerateSerializer]
public class PropertyUpdatedEvent : MyStateLogEvent
{
    [Id(0)] public string NewValue { get; set; } = string.Empty;
}

[GenerateSerializer]
public class CounterIncrementedEvent : MyStateLogEvent
{
    [Id(0)] public int IncrementBy { get; set; }
}
```

### 6. State Management (Orleans Event Sourcing)

**MUST** follow Orleans Event Sourcing pattern:

```csharp
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    [EventHandler]
    public async Task HandleUpdateAsync(UpdateEvent @event)
    {
        // STEP 1: Raise state events
        RaiseEvent(new PropertyUpdatedEvent 
        { 
            NewValue = @event.Value 
        });
        
        RaiseEvent(new CounterIncrementedEvent 
        { 
            IncrementBy = 1 
        });
        
        // STEP 2: Confirm events (persists and applies them)
        await ConfirmEvents();
        
        // STEP 3: Publish any response events
        await PublishAsync(new UpdateCompletedEvent 
        { 
            UpdatedAt = DateTime.UtcNow 
        });
    }
    
    // STEP 4: Override GAgentTransitionState for custom state transitions
    protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
    {
        switch (@event)
        {
            case PropertyUpdatedEvent e:
                state.Property1 = e.NewValue;
                break;
            case CounterIncrementedEvent e:
                state.Counter += e.IncrementBy;
                break;
        }
    }
}
```

### 7. Event Handler Types

#### Type 1: Regular Event Handler with Attribute
```csharp
[EventHandler(priority: 100, allowSelfHandling: false)]
public async Task HandleMyEventAsync(MyEvent @event)
{
    Logger.LogInformation("Handling event: {EventId}", @event.Id);
    // Process event
}
```

#### Type 2: All Event Handler
```csharp
[AllEventHandler(allowSelfHandling: false)]
public Task HandleAllEventsAsync(EventWrapperBase eventWrapper)
{
    // Extract event information using reflection if needed
    var eventType = eventWrapper.GetType();
    Logger.LogDebug("Received event of type: {Type}", eventType.Name);
    return Task.CompletedTask;
}
```

#### Type 3: Default Handler (Convention-based)
```csharp
// No attribute needed if method name is exactly "HandleEventAsync"
// and parameter type is not abstract
public async Task HandleEventAsync(MyConcreteEvent @event)
{
    Logger.LogInformation("Default handler for: {EventType}", @event.GetType().Name);
    // This will be recognized as an event handler automatically
}
```

### 8. Service Access Pattern (Simplified)

Use direct lazy properties for services:

```csharp
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    // Simplified service access - no backing field needed
    private IGAgentFactory GAgentFactory => 
        ServiceProvider.GetRequiredService<IGAgentFactory>();
    
    private IGAgentService GAgentService => 
        ServiceProvider.GetRequiredService<IGAgentService>();
    
    public async Task DoSomethingAsync()
    {
        // Use the service directly
        var otherAgent = await GAgentFactory.GetGAgentAsync<IOtherGAgent>(Guid.NewGuid());
        var info = await GAgentService.GetGAgentInfoAsync(this.GetGrainId());
    }
}
```

### 9. Event Publishing for Inter-GAgent Communication

```csharp
// GAgents must be in the same group (Parent-Children relationship)

// Setup group communication
public async Task SetupGroupCommunicationAsync()
{
    var publisher = await GAgentFactory.GetGAgentAsync<IPublishingGAgent>();
    
    // Register this agent as a child
    await publisher.RegisterAsync(this);
    
    // Register other agents
    var processor = await GAgentFactory.GetGAgentAsync<IProcessorGAgent>(Guid.NewGuid());
    await publisher.RegisterAsync(processor);
    
    // Now agents can communicate via events
    await publisher.PublishEventAsync(new ProcessDataEvent(), processor);
}

// Direct publish (only works within same group)
await PublishAsync(new MyEvent());
```

## 🛠️ Complete Example

### File: src/GAgents/DataProcessorGAgent.cs
```csharp
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// Interface definition
public interface IDataProcessorGAgent : IStateGAgent<DataProcessorState>
{
    Task<DataProcessorStatistics> GetStatisticsAsync();
}

// State definition
[GenerateSerializer]
public class DataProcessorState : StateBase
{
    [Id(0)] public List<string> ProcessedDataIds { get; set; } = new();
    [Id(1)] public Dictionary<string, string> Results { get; set; } = new();
    [Id(2)] public int TotalProcessed { get; set; }
    [Id(3)] public DateTime? LastProcessedAt { get; set; }
}

// Statistics DTO
[GenerateSerializer]
public class DataProcessorStatistics
{
    [Id(0)] public int TotalProcessed { get; set; }
    [Id(1)] public DateTime? LastProcessedAt { get; set; }
    [Id(2)] public List<string> ProcessedDataIds { get; set; } = new();
}

// Required GAgent attribute
[GAgent("data-processor", "workshop")]
public class DataProcessorGAgent : GAgentBase<DataProcessorState, DataProcessorStateLogEvent>, IDataProcessorGAgent
{
    // State log event base (internal to this GAgent)
    [GenerateSerializer]
    public class DataProcessorStateLogEvent : StateLogEventBase<DataProcessorStateLogEvent>
    {
    }

    // Specific state change events (internal to this GAgent)
    [GenerateSerializer]
    public class DataProcessedLogEvent : DataProcessorStateLogEvent
    {
        [Id(0)] public string DataId { get; set; } = string.Empty;
        [Id(1)] public string Result { get; set; } = string.Empty;
        [Id(2)] public DateTime ProcessedAt { get; set; }
    }

    // Simplified service access
    private IGAgentFactory GAgentFactory => ServiceProvider.GetRequiredService<IGAgentFactory>();
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Processes incoming data and tracks statistics. Supports batch processing and real-time updates.");
    }
    
    [EventHandler]
    public async Task HandleProcessDataEventAsync(ProcessDataEvent @event)
    {
        Logger.LogInformation("Processing data: {DataId} of type {DataType}", 
            @event.DataId, @event.DataType);
        
        try
        {
            // Process the data
            var result = await ProcessDataAsync(@event.Data);
            
            // Update state using event sourcing
            RaiseEvent(new DataProcessedLogEvent
            {
                DataId = @event.DataId,
                Result = result,
                ProcessedAt = DateTime.UtcNow
            });
            
            // Confirm state changes
            await ConfirmEvents();
            
            // Publish completion event
            await PublishAsync(new DataProcessedEvent
            {
                DataId = @event.DataId,
                Result = result,
                ProcessedAt = DateTime.UtcNow,
                ProcessorId = this.GetGrainId()
            });
            
            Logger.LogInformation("Successfully processed data: {DataId}", @event.DataId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process data: {DataId}", @event.DataId);
            throw;
        }
    }
    
    [AllEventHandler(allowSelfHandling: false)]
    public Task LogAllEventsAsync(EventWrapperBase eventWrapper)
    {
        var eventType = eventWrapper.GetType();
        Logger.LogDebug("Event received: {EventType}", eventType.Name);
        return Task.CompletedTask;
    }
    
    // Override for custom state transitions
    protected override void GAgentTransitionState(DataProcessorState state, StateLogEventBase<DataProcessorStateLogEvent> @event)
    {
        switch (@event)
        {
            case DataProcessedLogEvent e:
                state.ProcessedDataIds.Add(e.DataId);
                state.Results[e.DataId] = e.Result;
                state.TotalProcessed++;
                state.LastProcessedAt = e.ProcessedAt;
                break;
                
            // Handle other state change events here
        }
    }
    
    private async Task<string> ProcessDataAsync(string data)
    {
        // Simulate async processing
        await Task.Delay(100);
        return $"Processed: {data}";
    }
    
    public Task<DataProcessorStatistics> GetStatisticsAsync()
    {
        return Task.FromResult(new DataProcessorStatistics
        {
            TotalProcessed = State.TotalProcessed,
            LastProcessedAt = State.LastProcessedAt,
            ProcessedDataIds = State.ProcessedDataIds.ToList()
        });
    }
}
```

### File: src/Events/ProcessDataEvent.cs
```csharp
[GenerateSerializer]
public class ProcessDataEvent : EventBase
{
    [Id(0)] public string DataId { get; set; } = string.Empty;
    [Id(1)] public string Data { get; set; } = string.Empty;
    [Id(2)] public string DataType { get; set; } = string.Empty;
}
```

### File: src/Events/DataProcessedEvent.cs
```csharp
[GenerateSerializer]
public class DataProcessedEvent : EventBase
{
    [Id(0)] public string DataId { get; set; } = string.Empty;
    [Id(1)] public string Result { get; set; } = string.Empty;
    [Id(2)] public DateTime ProcessedAt { get; set; }
    [Id(3)] public GrainId ProcessorId { get; set; }
}
```

## 🔍 Key Points to Remember

1. **Interface First**: Always define interface before implementation (IStateGAgent<TState> or IStateGAgent<TState> + IAIGAgent)
2. **Logger**: Always use the built-in `Logger` property, never inject
3. **GAgent Factory**: Use `IGAgentFactory`, not `IGrainFactory`
4. **[GAgent] Attribute**: Required, but parameters are optional
5. **GetDescriptionAsync()**: Required method for detailed description
6. **State Management**: Always use RaiseEvent() → ConfirmEvents() → GAgentTransitionState
7. **Collections**: Always initialize collections in state classes
8. **Event Handlers**: Three types - [EventHandler], [AllEventHandler], or "HandleEventAsync" convention
9. **Services**: Use simplified lazy properties (no backing field needed)
10. **Event Communication**: GAgents must be in the same group (parent-children)
11. **Serialization**: Always add `[GenerateSerializer]` and `[Id(n)]` attributes

## 📂 File Organization

```
src/
├── Events/                    # Shared events (EventBase derived)
│   ├── NotificationEvent.cs
│   ├── DataProcessingEvent.cs
│   ├── CoordinationEvent.cs
│   └── CommonEvents.cs
└── GAgents/                   # GAgent implementations (all-in-one files)
    ├── DataProcessorGAgent.cs # Contains: interface, state, state log events, implementation
    ├── NotificationGAgent.cs
    ├── ProcessingGAgent.cs
    ├── CoordinatorGAgent.cs
    └── EventLoggerGAgent.cs
```

### Best Practice: Single File per GAgent
Keep all GAgent-related code in a single file for better readability and maintainability:

1. **Interface definition** (e.g., `IDataProcessorGAgent`)
2. **State class** (e.g., `DataProcessorState`)
3. **State log events** (as nested classes within the GAgent)
4. **GAgent implementation** (e.g., `DataProcessorGAgent`)

This approach makes it easier to understand the complete context of a GAgent without jumping between multiple files. 