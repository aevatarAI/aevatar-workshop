# GAgent Development Guide

A comprehensive guide to building intelligent agents with the Aevatar framework.

---

## 🚀 Getting Started

### What is a GAgent?

A **GAgent** is an intelligent, autonomous entity in the Aevatar framework that can:

- 🧠 **Think and Remember**: Maintain persistent state across interactions
- 💬 **Communicate**: Send and receive events with other GAgents
- ⚡ **React**: Handle events and state changes automatically
- 🤖 **AI-Powered**: Integrate with LLMs for intelligent decision-making

### Core Concepts

- **Event-Driven**: GAgents communicate through strongly-typed events
- **Event Sourcing**: All state changes are tracked as events for complete auditability
- **Orleans-Based**: Built on Microsoft Orleans for massive scalability
- **Type-Safe**: Full C# type safety with compile-time checking

---

## 🏗️ Your First GAgent

Let's build a simple Counter GAgent step by step.

### Step 1: Define the Interface

```csharp
public interface ICounterGAgent : IStateGAgent<CounterState>
{
    Task IncrementAsync(int amount = 1);
    Task DecrementAsync(int amount = 1);
    Task<int> GetCurrentValueAsync();
}
```

### Step 2: Define the State

```csharp
[GenerateSerializer]
public class CounterState : StateBase
{
    [Id(0)] public int Value { get; set; }
    [Id(1)] public DateTime LastUpdated { get; set; }
    [Id(2)] public List<string> History { get; set; } = new();
}
```

### Step 3: Define State Log Events

```csharp
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
```

### Step 4: Implement the GAgent

```csharp
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
```

### Step 5: Use Your GAgent

```csharp
// Get the GAgent through the factory
var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();
var counter = await gAgentFactory.GetGAgentAsync<ICounterGAgent>(Guid.NewGuid());

// Use it
await counter.IncrementAsync(5);
await counter.DecrementAsync(2);
var currentValue = await counter.GetCurrentValueAsync(); // Returns 3
```

---

## 🗂️ State Management

### The Event Sourcing Pattern

Instead of directly modifying state, GAgents use **event sourcing**:

1. **Raise Event**: Create an event describing what happened
2. **Confirm Events**: Apply all pending events to state
3. **State Transition**: Handle the event in `GAgentTransitionState`

### Why Event Sourcing?

✅ **Complete Audit Trail**: Every state change is recorded  
✅ **Time Travel**: Replay events to recreate any historical state  
✅ **Consistency**: Atomic state updates across distributed systems  
✅ **Debugging**: Full visibility into what changed when and why  

### State Initialization with Configuration

For GAgents that need initialization parameters:

```csharp
// Step 1: Define Configuration
[GenerateSerializer]
public class CounterConfiguration : ConfigurationBase
{
    [Id(0)] public int InitialValue { get; set; }
    [Id(1)] public string Name { get; set; } = string.Empty;
}

// Step 2: Define Initialization Event
[GenerateSerializer]
public class CounterInitializedEvent : CounterStateLogEvent
{
    [Id(0)] public int InitialValue { get; init; }
    [Id(1)] public string Name { get; init; }
}

// Step 3: Use 4-Parameter GAgentBase
public class CounterGAgent : GAgentBase<CounterState, CounterStateLogEvent, EventBase, CounterConfiguration>, ICounterGAgent
{
    protected override async Task PerformConfigAsync(CounterConfiguration configuration)
    {
        // Initialize state through events
        RaiseEvent(new CounterInitializedEvent
        {
            InitialValue = configuration.InitialValue,
            Name = configuration.Name
        });
        
        await ConfirmEvents();
    }
    
    // Handle in state transition
    protected override void GAgentTransitionState(CounterState state, StateLogEventBase<CounterStateLogEvent> @event)
    {
        switch (@event)
        {
            case CounterInitializedEvent e:
                state.Value = e.InitialValue;
                state.Id = this.GetGrainId().Key.ToString() ?? "default";
                state.History = new List<string> { $"Initialized as '{e.Name}' with value {e.InitialValue}" };
                break;
            // ... other events
        }
    }
}

// Usage with configuration
var config = new CounterConfiguration 
{ 
    InitialValue = 100, 
    Name = "My Counter" 
};
var counter = await gAgentFactory.GetGAgentAsync<ICounterGAgent>(Guid.NewGuid(), config);
```

### Common State Management Patterns

#### ✅ Do This
```csharp
// Correct: Use events to modify state
RaiseEvent(new ValueChangedEvent { NewValue = 42 });
await ConfirmEvents();

// Always initialize collections in state
[GenerateSerializer]
public class MyState : StateBase
{
    [Id(0)] public List<string> Items { get; set; } = new();
    [Id(1)] public Dictionary<string, int> Counters { get; set; } = new();
}
```

#### ❌ Don't Do This
```csharp
// Wrong: Direct state modification
State.Value = 42; // This will be lost!

// Wrong: Uninitialized collections
public List<string> Items { get; set; } // Will be null!
```

---

## 📡 Event System

Events are the primary communication mechanism between GAgents.

### Event Types

#### 1. Business Events (for communication)
```csharp
[GenerateSerializer]
public class OrderCreatedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public decimal Amount { get; init; }
    [Id(2)] public string CustomerId { get; init; } = string.Empty;
}
```

#### 2. State Log Events (for state changes)
```csharp
[GenerateSerializer]
public class OrderStateChangedEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public OrderStatus NewStatus { get; init; }
}
```

### Event Handlers

#### Attribute-Based Handlers
```csharp
[EventHandler]
public async Task HandleOrderCreatedAsync(OrderCreatedEvent @event)
{
    Logger.LogInformation("Processing order {OrderId}", @event.OrderId);
    
    // Update state
    RaiseEvent(new OrderProcessingStartedEvent { OrderId = @event.OrderId });
    await ConfirmEvents();
    
    // Notify other agents
    await PublishAsync(new OrderProcessingNotificationEvent 
    { 
        OrderId = @event.OrderId,
        Status = "Processing Started"
    });
}
```

#### Convention-Based Handlers
```csharp
// Method named "HandleEventAsync" is automatically recognized
public async Task HandleEventAsync(OrderCreatedEvent @event)
{
    // Handle the event
}
```

#### All-Events Handler
```csharp
[AllEventHandler(allowSelfHandling: false)]
public Task HandleAllEventsAsync(EventWrapperBase eventWrapper)
{
    Logger.LogDebug("Received event: {EventType}", eventWrapper.Event.GetType().Name);
    return Task.CompletedTask;
}
```

### Event Communication Setup

GAgents must be in the same communication group to exchange events:

```csharp
// Create a coordinator GAgent
var coordinator = await gAgentFactory.GetGAgentAsync<ICoordinatorGAgent>();

// Register other GAgents with the coordinator
await coordinator.RegisterAsync(orderProcessor);
await coordinator.RegisterAsync(inventoryManager);
await coordinator.RegisterAsync(notificationService);

// Now they can communicate through events
await orderProcessor.PublishAsync(new OrderCreatedEvent { /* ... */ });
```

### Event Publishing Patterns

#### Broadcast to All
```csharp
await PublishAsync(new SystemMaintenanceEvent { Message = "System going down" });
```

#### Targeted Publishing
```csharp
await PublishAsync(targetGrainId, new PersonalNotificationEvent { Message = "Hello!" });
```

#### Event Subscription
```csharp
// Subscribe to another GAgent's events
await SubscribeToAsync(otherGAgent);

// Unsubscribe when no longer needed
await UnsubscribeFromAsync(otherGAgent);
```

---

## 🤖 AI Integration

Transform your GAgents into intelligent agents with AI capabilities.

### Creating an AI-Enhanced GAgent

#### Step 1: Define AI Interface
```csharp
public interface IOrderAIGAgent : IStateGAgent<OrderAIState>, IAIGAgent
{
    Task<string> ProcessCustomerRequestAsync(string request);
    Task<bool> ValidateOrderAsync(string orderDetails);
}
```

#### Step 2: Inherit from AIGAgentBase
```csharp
[GAgent("order-ai", "workshop")]
public class OrderAIGAgent : AIGAgentBase<OrderAIState, OrderAIStateLogEvent>, IOrderAIGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("AI-powered order processing agent");

    // Use AIGAgentTransitionState instead of GAgentTransitionState
    protected override void AIGAgentTransitionState(OrderAIState state, StateLogEventBase<OrderAIStateLogEvent> @event)
    {
        switch (@event)
        {
            case OrderProcessedEvent e:
                state.ProcessedOrders.Add(e.OrderId);
                state.LastProcessedAt = e.Timestamp;
                break;
        }
    }
}
```

### AI Initialization

```csharp
// Initialize with AI capabilities
var initDto = new InitializeDto
{
    LLMConfig = new LLMConfigDto
    {
        SystemLLM = "gpt-4o"  // Reference system-wide config
    },
    Instructions = "You are an order processing specialist. Help customers with their orders.",
    EnableGAgentTools = true,  // Allow AI to call other GAgents
    EnableMCPTools = true,     // Enable Model Context Protocol tools
    SelectedGAgents = new List<string> { "inventory-manager", "payment-processor" }
};

var aiAgent = await gAgentFactory.GetGAgentAsync<IOrderAIGAgent>(Guid.NewGuid());
await aiAgent.InitializeAsync(initDto);
```

### Tool Registration

#### GAgent Tools
Your AI agent can automatically call other GAgents as tools:

```csharp
var initDto = new InitializeDto
{
    EnableGAgentTools = true,
    SelectedGAgents = new List<string> 
    { 
        "inventory-manager",    // AI can check inventory
        "payment-processor",    // AI can process payments
        "notification-service"  // AI can send notifications
    }
};
```

#### MCP (Model Context Protocol) Tools
Integrate external tools and services:

```csharp
var initDto = new InitializeDto
{
    EnableMCPTools = true,
    MCPServers = new List<MCPServerConfig>
    {
        new MCPServerConfig
        {
            ServerName = "filesystem",
            Command = "npx",
            Arguments = new List<string> { "@modelcontextprotocol/server-filesystem", "/workspace" }
        },
        new MCPServerConfig
        {
            ServerName = "weather",
            Command = "python",
            Arguments = new List<string> { "weather_server.py" }
        }
    }
};
```

### Using AI Chat

```csharp
public async Task<string> ProcessCustomerRequestAsync(string request)
{
    var response = await ChatAsync(new ChatRequestDto
    {
        Prompt = request,
        ChatId = Guid.NewGuid().ToString(),
        // AI will automatically use available tools if needed
    });

    // Tool calls are automatically tracked
    Logger.LogInformation("AI response: {Response}, Tool calls: {ToolCount}", 
        response.Response, response.ToolCalls?.Count ?? 0);

    return response.Response;
}
```

---

## 🔧 Advanced Topics

### Timer Registration

#### Stateless Timers
```csharp
private IGrainTimer? _heartbeatTimer;

protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
{
    // Register a heartbeat timer
    _heartbeatTimer = this.RegisterGrainTimer(
        async (token) => await SendHeartbeatAsync(),
        new GrainTimerCreationOptions
        {
            DueTime = TimeSpan.FromSeconds(30),
            Period = TimeSpan.FromSeconds(30),
            Interleave = true  // Allow other calls during timer execution
        }
    );
    
    return base.OnGAgentActivateAsync(cancellationToken);
}

private async Task SendHeartbeatAsync()
{
    await PublishAsync(new HeartbeatEvent 
    { 
        AgentId = this.GetGrainId().ToString(),
        Timestamp = DateTime.UtcNow
    });
}
```

#### Stateful Timers
```csharp
private IGrainTimer? _processTimer;

protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
{
    var timerState = new { BatchSize = 10, MaxRetries = 3 };
    
    _processTimer = this.RegisterGrainTimer(
        timerState,
        async (state, token) => await ProcessBatchAsync(state.BatchSize, state.MaxRetries),
        new GrainTimerCreationOptions
        {
            DueTime = TimeSpan.FromMinutes(1),
            Period = TimeSpan.FromMinutes(5)
        }
    );
    
    return base.OnGAgentActivateAsync(cancellationToken);
}
```

#### Timer Cleanup
```csharp
public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
{
    _heartbeatTimer?.Dispose();
    _processTimer?.Dispose();
    return base.OnDeactivateAsync(reason, cancellationToken);
}
```

### Service Access

```csharp
// Lazy property for service access
private IGAgentFactory GAgentFactory => 
    ServiceProvider.GetRequiredService<IGAgentFactory>();

private IMyCustomService CustomService =>
    ServiceProvider.GetRequiredService<IMyCustomService>();

// Usage
public async Task ProcessWithOtherAgentAsync()
{
    var otherAgent = await GAgentFactory.GetGAgentAsync<IOtherGAgent>(targetId);
    await otherAgent.DoSomethingAsync();
}
```

### Override Lifecycle Methods

```csharp
protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
{
    Logger.LogInformation("GAgent {GrainId} is activating", this.GetGrainId());
    // Initialize resources, timers, etc.
    return base.OnGAgentActivateAsync(cancellationToken);
}

public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
{
    Logger.LogInformation("GAgent {GrainId} is deactivating due to {Reason}", 
        this.GetGrainId(), reason);
    // Cleanup resources
    return base.OnDeactivateAsync(reason, cancellationToken);
}

protected override Task HandleStateChangedAsync()
{
    Logger.LogDebug("State changed in {GrainId}", this.GetGrainId());
    // React to state changes
    return Task.CompletedTask;
}

protected override Task OnRegisterAgentAsync(GrainId agentGuid)
{
    Logger.LogInformation("Agent {AgentId} registered with {GrainId}", 
        agentGuid, this.GetGrainId());
    return Task.CompletedTask;
}
```

---

## ✅ Best Practices

### Project Structure

Organize your GAgent code for maintainability:

```
src/
├── Events/                    # Shared events (EventBase derived)
│   ├── OrderEvents.cs
│   ├── NotificationEvents.cs
│   └── SystemEvents.cs
└── GAgents/                   # GAgent implementations
    ├── OrderGAgent.cs         # All-in-one: interface, state, events, implementation
    ├── InventoryGAgent.cs
    ├── PaymentGAgent.cs
    └── NotificationGAgent.cs
```

### All-in-One File Pattern

Keep related code together for better readability:

```csharp
// OrderGAgent.cs - Everything in one file

// Interface
public interface IOrderGAgent : IStateGAgent<OrderState>
{
    Task<string> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderStatus> GetOrderStatusAsync(string orderId);
}

// State
[GenerateSerializer]
public class OrderState : StateBase
{
    [Id(0)] public Dictionary<string, Order> Orders { get; set; } = new();
    [Id(1)] public DateTime LastOrderTime { get; set; }
}

// State Log Events
[GenerateSerializer]
public abstract class OrderStateLogEvent : StateLogEventBase<OrderStateLogEvent>;

[GenerateSerializer]
public class OrderCreatedLogEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public Order Order { get; init; } = new();
}

// Implementation
[GAgent("order", "ecommerce")]
public class OrderGAgent : GAgentBase<OrderState, OrderStateLogEvent>, IOrderGAgent
{
    // Implementation here...
}
```

### Common Pitfalls and Solutions

#### ❌ Problem: Direct State Modification
```csharp
// Wrong
public Task AddItemAsync(string item)
{
    State.Items.Add(item); // This change will be lost!
    return Task.CompletedTask;
}
```

#### ✅ Solution: Event-Driven State Changes
```csharp
// Correct
public async Task AddItemAsync(string item)
{
    RaiseEvent(new ItemAddedEvent { Item = item });
    await ConfirmEvents();
}
```

#### ❌ Problem: Using IGrainFactory
```csharp
// Wrong
var grainFactory = ServiceProvider.GetRequiredService<IGrainFactory>();
var grain = grainFactory.GetGrain<IMyGAgent>(id);
```

#### ✅ Solution: Use IGAgentFactory
```csharp
// Correct
var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
var agent = await gAgentFactory.GetGAgentAsync<IMyGAgent>(id);
```

#### ❌ Problem: Constructor Dependencies
```csharp
// Wrong
public MyGAgent(ILogger<MyGAgent> logger, IMyService service)
{
    // This won't work in Orleans!
}
```

#### ✅ Solution: Service Locator Pattern
```csharp
// Correct
private IMyService MyService => 
    ServiceProvider.GetRequiredService<IMyService>();
```

### Performance Tips

1. **Batch State Changes**: Group multiple events together before `ConfirmEvents()`
2. **Reasonable Timer Intervals**: Don't overwhelm the system with frequent timers
3. **Proper Resource Cleanup**: Always dispose timers and resources in `OnDeactivateAsync`
4. **Event Size**: Keep events small and focused
5. **Async/Await**: Always use proper async patterns

### Testing Your GAgents

```csharp
[Test]
public async Task CounterGAgent_Should_Increment_Correctly()
{
    // Arrange
    var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();
    var counter = await gAgentFactory.GetGAgentAsync<ICounterGAgent>(Guid.NewGuid());

    // Act
    await counter.IncrementAsync(5);
    var result = await counter.GetCurrentValueAsync();

    // Assert
    Assert.AreEqual(5, result);
}
```

---

## 📖 Complete Examples

### Example 1: E-commerce Order Processing System

This example shows a complete multi-GAgent system for processing orders:

```csharp
// === Order Events ===
[GenerateSerializer]
public class OrderSubmittedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public List<OrderItem> Items { get; init; } = new();
    [Id(2)] public string CustomerId { get; init; } = string.Empty;
}

[GenerateSerializer]
public class OrderValidatedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public bool IsValid { get; init; }
    [Id(2)] public string ValidationMessage { get; init; } = string.Empty;
}

[GenerateSerializer]
public class PaymentProcessedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public bool IsSuccessful { get; init; }
    [Id(2)] public decimal Amount { get; init; }
}

// === Order GAgent ===
public interface IOrderGAgent : IStateGAgent<OrderState>
{
    Task<string> SubmitOrderAsync(SubmitOrderRequest request);
    Task<OrderStatus> GetOrderStatusAsync(string orderId);
}

[GenerateSerializer]
public class OrderState : StateBase
{
    [Id(0)] public Dictionary<string, Order> Orders { get; set; } = new();
    [Id(1)] public Dictionary<string, OrderStatus> OrderStatuses { get; set; } = new();
}

[GenerateSerializer]
public abstract class OrderStateLogEvent : StateLogEventBase<OrderStateLogEvent>;

[GenerateSerializer]
public class OrderCreatedLogEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public Order Order { get; init; } = new();
}

[GenerateSerializer]
public class OrderStatusChangedLogEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public OrderStatus NewStatus { get; init; }
}

[GAgent("order", "ecommerce")]
public class OrderGAgent : GAgentBase<OrderState, OrderStateLogEvent>, IOrderGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Manages customer orders and order lifecycle");

    public async Task<string> SubmitOrderAsync(SubmitOrderRequest request)
    {
        var orderId = Guid.NewGuid().ToString();
        var order = new Order
        {
            Id = orderId,
            CustomerId = request.CustomerId,
            Items = request.Items,
            TotalAmount = request.Items.Sum(i => i.Price * i.Quantity),
            CreatedAt = DateTime.UtcNow
        };

        // Update state
        RaiseEvent(new OrderCreatedLogEvent { OrderId = orderId, Order = order });
        RaiseEvent(new OrderStatusChangedLogEvent { OrderId = orderId, NewStatus = OrderStatus.Submitted });
        await ConfirmEvents();

        // Notify other agents
        await PublishAsync(new OrderSubmittedEvent
        {
            OrderId = orderId,
            Items = request.Items,
            CustomerId = request.CustomerId
        });

        Logger.LogInformation("Order {OrderId} submitted for customer {CustomerId}", 
            orderId, request.CustomerId);

        return orderId;
    }

    public Task<OrderStatus> GetOrderStatusAsync(string orderId)
    {
        return Task.FromResult(
            State.OrderStatuses.TryGetValue(orderId, out var status) 
                ? status 
                : OrderStatus.NotFound
        );
    }

    [EventHandler]
    public async Task HandleOrderValidatedAsync(OrderValidatedEvent @event)
    {
        var newStatus = @event.IsValid ? OrderStatus.Validated : OrderStatus.ValidationFailed;
        
        RaiseEvent(new OrderStatusChangedLogEvent 
        { 
            OrderId = @event.OrderId, 
            NewStatus = newStatus 
        });
        await ConfirmEvents();

        Logger.LogInformation("Order {OrderId} validation result: {IsValid}", 
            @event.OrderId, @event.IsValid);
    }

    [EventHandler]
    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent @event)
    {
        var newStatus = @event.IsSuccessful ? OrderStatus.PaymentCompleted : OrderStatus.PaymentFailed;
        
        RaiseEvent(new OrderStatusChangedLogEvent 
        { 
            OrderId = @event.OrderId, 
            NewStatus = newStatus 
        });
        await ConfirmEvents();

        Logger.LogInformation("Order {OrderId} payment result: {IsSuccessful}", 
            @event.OrderId, @event.IsSuccessful);
    }

    protected override void GAgentTransitionState(OrderState state, StateLogEventBase<OrderStateLogEvent> @event)
    {
        switch (@event)
        {
            case OrderCreatedLogEvent e:
                state.Orders[e.OrderId] = e.Order;
                break;
                
            case OrderStatusChangedLogEvent e:
                state.OrderStatuses[e.OrderId] = e.NewStatus;
                break;
        }
    }
}

// === Inventory GAgent ===
[GAgent("inventory", "ecommerce")]
public class InventoryGAgent : GAgentBase<InventoryState, InventoryStateLogEvent>, IInventoryGAgent
{
    [EventHandler]
    public async Task HandleOrderSubmittedAsync(OrderSubmittedEvent @event)
    {
        // Validate inventory availability
        bool isValid = await ValidateInventoryAsync(@event.Items);
        
        if (isValid)
        {
            // Reserve inventory
            await ReserveInventoryAsync(@event.OrderId, @event.Items);
        }

        // Notify order system
        await PublishAsync(new OrderValidatedEvent
        {
            OrderId = @event.OrderId,
            IsValid = isValid,
            ValidationMessage = isValid ? "Inventory available" : "Insufficient inventory"
        });
    }

    // ... implementation details
}

// === Payment GAgent ===
[GAgent("payment", "ecommerce")]
public class PaymentGAgent : GAgentBase<PaymentState, PaymentStateLogEvent>, IPaymentGAgent
{
    [EventHandler]
    public async Task HandleOrderValidatedAsync(OrderValidatedEvent @event)
    {
        if (!@event.IsValid) return;

        // Process payment
        bool paymentSuccessful = await ProcessPaymentAsync(@event.OrderId);

        // Get order amount
        var amount = await GetOrderAmountAsync(@event.OrderId);

        // Notify order system
        await PublishAsync(new PaymentProcessedEvent
        {
            OrderId = @event.OrderId,
            IsSuccessful = paymentSuccessful,
            Amount = amount
        });
    }

    // ... implementation details
}

// === Notification GAgent ===
[GAgent("notification", "ecommerce")]
public class NotificationGAgent : GAgentBase<NotificationState, NotificationStateLogEvent>, INotificationGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Sends notifications to customers via multiple channels");

    [EventHandler]
    public async Task HandleOrderNotificationAsync(OrderNotificationEvent @event)
    {
        Logger.LogInformation("Sending {Type} notification to customer {CustomerId} for order {OrderId}", 
            @event.Type, @event.CustomerId, @event.OrderId);

        // Check customer notification preferences
        var preferences = await GetCustomerPreferencesAsync(@event.CustomerId);
        
        if (!ShouldSendNotification(@event.Type, preferences))
        {
            Logger.LogInformation("Notification skipped due to customer preferences");
            return;
        }

        // Determine notification channel
        var channel = DetermineChannel(@event.Type, preferences);
        
        // Create notification record
        var notificationId = Guid.NewGuid().ToString();
        var notification = new NotificationRecord
        {
            Id = notificationId,
            CustomerId = @event.CustomerId,
            Type = @event.Type,
            Channel = channel,
            Subject = @event.Subject,
            Message = @event.Message,
            SentAt = DateTime.UtcNow,
            Status = NotificationStatus.Sent
        };

        // Send notification
        var success = await SendNotificationAsync(notification);
        if (!success)
        {
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = "Failed to send notification";
        }

        // Update state
        RaiseEvent(new NotificationSentLogEvent { Record = notification });
        await ConfirmEvents();

        Logger.LogInformation("Notification {NotificationId} processed with status {Status}", 
            notificationId, notification.Status);
    }

    private async Task<bool> SendNotificationAsync(NotificationRecord notification)
    {
        // Simulate sending notification via different channels
        await Task.Delay(100); // Simulate network call
        
        return notification.Channel switch
        {
            NotificationChannel.Email => await SendEmailAsync(notification),
            NotificationChannel.SMS => await SendSmsAsync(notification),
            NotificationChannel.Push => await SendPushNotificationAsync(notification),
            _ => false
        };
    }

    private Task<bool> SendEmailAsync(NotificationRecord notification)
    {
        // Email sending logic
        Logger.LogInformation("Sending email to customer {CustomerId}: {Subject}", 
            notification.CustomerId, notification.Subject);
        return Task.FromResult(new Random().NextDouble() > 0.05); // 95% success rate
    }

    private Task<bool> SendSmsAsync(NotificationRecord notification)
    {
        // SMS sending logic  
        Logger.LogInformation("Sending SMS to customer {CustomerId}: {Subject}", 
            notification.CustomerId, notification.Subject);
        return Task.FromResult(new Random().NextDouble() > 0.03); // 97% success rate
    }

    private Task<bool> SendPushNotificationAsync(NotificationRecord notification)
    {
        // Push notification logic
        Logger.LogInformation("Sending push notification to customer {CustomerId}: {Subject}", 
            notification.CustomerId, notification.Subject);
        return Task.FromResult(new Random().NextDouble() > 0.02); // 98% success rate
    }

    protected override void GAgentTransitionState(NotificationState state, StateLogEventBase<NotificationStateLogEvent> @event)
    {
        switch (@event)
        {
            case NotificationSentLogEvent e:
                // Add to customer notification history
                if (!state.CustomerNotifications.ContainsKey(e.Record.CustomerId))
                    state.CustomerNotifications[e.Record.CustomerId] = new List<NotificationRecord>();
                
                state.CustomerNotifications[e.Record.CustomerId].Add(e.Record);
                
                // Update statistics
                state.TotalNotificationsSent++;
                state.LastNotificationTime = e.Record.SentAt;
                break;
        }
    }

    // ... implementation details
}

// === Usage ===
public class ECommerceService
{
    private readonly IGAgentFactory _gAgentFactory;

    public async Task SetupSystemAsync()
    {
        // Create coordinator
        var coordinator = await _gAgentFactory.GetGAgentAsync<ICoordinatorGAgent>();

        // Create and register all agents
        var orderAgent = await _gAgentFactory.GetGAgentAsync<IOrderGAgent>();
        var inventoryAgent = await _gAgentFactory.GetGAgentAsync<IInventoryGAgent>();
        var paymentAgent = await _gAgentFactory.GetGAgentAsync<IPaymentGAgent>();
        var notificationAgent = await _gAgentFactory.GetGAgentAsync<INotificationGAgent>();

        // Register for event communication
        await coordinator.RegisterAsync(orderAgent);
        await coordinator.RegisterAsync(inventoryAgent);
        await coordinator.RegisterAsync(paymentAgent);
        await coordinator.RegisterAsync(notificationAgent);

        Logger.LogInformation("E-commerce system setup complete");
    }

    public async Task<string> ProcessOrderAsync(SubmitOrderRequest request)
    {
        var orderAgent = await _gAgentFactory.GetGAgentAsync<IOrderGAgent>();
        return await orderAgent.SubmitOrderAsync(request);
    }
}
```

This complete example demonstrates:
- Multi-GAgent coordination
- Event-driven workflow
- State management across agents
- Error handling and validation
- Real-world business logic

---

## 🎯 Quick Implementation Checklist

### Basic GAgent
- [ ] Define interface inheriting from `IStateGAgent<TState>`
- [ ] Create state class with `[GenerateSerializer]` and `[Id(n)]` attributes
- [ ] Define state log events inheriting from `StateLogEventBase<T>`
- [ ] Implement GAgent with `[GAgent]` attribute
- [ ] Use `RaiseEvent` + `ConfirmEvents` for state changes
- [ ] Handle state transitions in `GAgentTransitionState`
- [ ] Initialize collections in state class
- [ ] Use `Logger` property (not injection)

### AI-Enhanced GAgent  
- [ ] Interface inherits from both `IStateGAgent<TState>` and `IAIGAgent`
- [ ] Inherit from `AIGAgentBase<TState, TStateLogEvent>`
- [ ] Override `AIGAgentTransitionState` (not `GAgentTransitionState`)
- [ ] Call `InitializeAsync` with `InitializeDto`
- [ ] Configure LLM settings and tools
- [ ] Use `ChatAsync` for AI interactions

### Event System
- [ ] Define events inheriting from `EventBase`
- [ ] Use `[EventHandler]` attribute or `HandleEventAsync` naming
- [ ] Publish events with `PublishAsync`
- [ ] Register GAgents with coordinator for communication
- [ ] Handle events asynchronously

### Advanced Features
- [ ] Use `IGAgentFactory` (never `IGrainFactory`)
- [ ] Register timers in `OnGAgentActivateAsync`
- [ ] Dispose timers in `OnDeactivateAsync`
- [ ] Access services through `ServiceProvider`
- [ ] Use configuration classes for initialization

---

## 🔗 Next Steps

1. **Try the Examples**: Start with the Counter GAgent and build from there
2. **Explore Smart Home Demo**: See a complete multi-GAgent system in action
3. **Read the API Documentation**: Dive deeper into specific classes and methods
4. **Join the Community**: Connect with other developers building with Aevatar

Happy coding with GAgents! 🚀 