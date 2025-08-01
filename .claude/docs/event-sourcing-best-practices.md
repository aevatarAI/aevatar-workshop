# Event Sourcing Best Practices & Examples

## Overview
This document provides practical examples and best practices for Event Sourcing in GAgent development, based on the fixes applied to the Acquire GAgents.

## Core Principles

### 1. Never Modify State Directly
```csharp
// ❌ WRONG - Direct state modification
public async Task UpdatePlayerAsync(string playerId, string newName)
{
    State.PlayerName = newName;  // This violates Event Sourcing!
    State.UpdatedAt = DateTime.UtcNow;
}

// ✅ CORRECT - Event Sourcing pattern
public async Task UpdatePlayerAsync(string playerId, string newName)
{
    RaiseEvent(new PlayerUpdatedEvent 
    {
        PlayerId = playerId,
        NewName = newName,
        UpdatedAt = DateTime.UtcNow
    });
    await ConfirmEvents();
}
```

### 2. Handle State Changes in GAgentTransitionState
```csharp
protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
{
    switch (@event)
    {
        case PlayerUpdatedEvent e:
            state.PlayerName = e.NewName;
            state.UpdatedAt = e.UpdatedAt;
            break;
            
        case PlayerJoinedEvent e:
            state.PlayerId = e.PlayerId;
            state.PlayerName = e.PlayerName;
            state.IsActive = true;
            state.JoinedAt = e.JoinedAt;
            break;
            
        // Handle all other events...
    }
}
```

## Event Design Patterns

### 1. Events Should Be Complete
```csharp
// ❌ INCOMPLETE - Missing information
[GenerateSerializer]
public class PlayerUpdatedEvent : MyStateLogEvent
{
    [Id(0)] public string NewName { get; set; }  // Only stores final state
}

// ✅ COMPLETE - Contains all necessary information
[GenerateSerializer]
public class PlayerUpdatedEvent : MyStateLogEvent
{
    [Id(0)] public string PlayerId { get; set; }
    [Id(1)] public string OldName { get; set; }
    [Id(2)] public string NewName { get; set; }
    [Id(3)] public DateTime UpdatedAt { get; set; }
    [Id(4)] public string UpdatedBy { get; set; }  // Who made the change
}
```

### 2. Use Past Tense for Event Names
```csharp
// ✅ Good naming convention
public class PlayerJoinedEvent : MyStateLogEvent
public class GameStartedEvent : MyStateLogEvent
public class TilePlacedEvent : MyStateLogEvent
public class StockPurchasedEvent : MyStateLogEvent

// ❌ Avoid these names
public class PlayerJoinEvent : MyStateLogEvent
public class GameStartEvent : MyStateLogEvent
public class TilePlaceEvent : MyStateLogEvent
```

## Complex State Changes

### 1. Collection Management
```csharp
// ❌ WRONG - Direct collection modification
public async Task AddItemAsync(string item)
{
    State.Items.Add(item);  // Direct modification!
}

// ✅ CORRECT - Event-based collection management
public async Task AddItemAsync(string item)
{
    RaiseEvent(new ItemAddedEvent 
    {
        Item = item,
        AddedAt = DateTime.UtcNow,
        AddedBy = GetCurrentUserId()
    });
    await ConfirmEvents();
}

protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
{
    switch (@event)
    {
        case ItemAddedEvent e:
            state.Items.Add(e.Item);
            state.ItemCount = state.Items.Count;
            state.LastUpdated = e.AddedAt;
            break;
    }
}
```

### 2. Complex Object Updates
```csharp
// ❌ WRONG - Modifying complex objects directly
public async Task UpdateGameStateAsync(GameState newGameState)
{
    State.CurrentGame = newGameState;  // Replacing entire object!
}

// ✅ CORRECT - Event-based complex object updates
public async Task UpdateGameStateAsync(GameStateUpdate update)
{
    RaiseEvent(new GameStateUpdatedEvent 
    {
        GameId = update.GameId,
        Status = update.NewStatus,
        CurrentPlayerId = update.CurrentPlayerId,
        Turn = update.Turn,
        UpdatedAt = DateTime.UtcNow
    });
    await ConfirmEvents();
}

protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
{
    switch (@event)
    {
        case GameStateUpdatedEvent e:
            if (state.CurrentGame == null)
            {
                state.CurrentGame = new GameState();
            }
            state.CurrentGame.Status = e.Status;
            state.CurrentGame.CurrentPlayerId = e.CurrentPlayerId;
            state.CurrentGame.Turn = e.Turn;
            state.CurrentGame.UpdatedAt = e.UpdatedAt;
            break;
    }
}
```

## Event Handler Patterns

### 1. External Event Handlers
```csharp
// ❌ WRONG - Direct state modification in external event handlers
[EventHandler]
public async Task HandleExternalEventAsync(ExternalEvent @event)
{
    State.SomeValue = @event.NewValue;  // Violates Event Sourcing!
}

// ✅ CORRECT - Use internal events for external event handling
[EventHandler]
public async Task HandleExternalEventAsync(ExternalEvent @event)
{
    // Convert external event to internal state change event
    RaiseEvent(new InternalStateChangedEvent 
    {
        NewValue = @event.NewValue,
        SourceEventId = @event.EventId,
        ProcessedAt = DateTime.UtcNow
    });
    await ConfirmEvents();
}
```

### 2. Event Processing with Validation
```csharp
[EventHandler]
public async Task HandlePlayerActionAsync(PlayerActionEvent @event)
{
    // Validate before creating state change
    if (!IsValidAction(@event.ActionType, @event.Parameters))
    {
        Logger.LogWarning("Invalid action: {Action}", @event.ActionType);
        return;
    }

    // Create appropriate state change event
    switch (@event.ActionType)
    {
        case "PlaceTile":
            RaiseEvent(new TilePlacementInitiatedEvent
            {
                PlayerId = @event.PlayerId,
                Position = @event.Parameters["Position"],
                Timestamp = DateTime.UtcNow
            });
            break;
            
        case "BuyStock":
            RaiseEvent(new StockPurchaseInitiatedEvent
            {
                PlayerId = @event.PlayerId,
                Stock = @event.Parameters["Stock"],
                Quantity = int.Parse(@event.Parameters["Quantity"]),
                Timestamp = DateTime.UtcNow
            });
            break;
    }
    
    await ConfirmEvents();
}
```

## State Initialization Patterns

### 1. Configuration-Based Initialization
```csharp
// ✅ CORRECT - Initialize through events
protected override async Task PerformConfigAsync(MyConfiguration config)
{
    RaiseEvent(new ConfigurationAppliedEvent 
    {
        InitialSettings = config.Settings,
        MaxCapacity = config.MaxCapacity,
        InitializedAt = DateTime.UtcNow
    });
    await ConfirmEvents();
}

protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
{
    switch (@event)
    {
        case ConfigurationAppliedEvent e:
            state.Settings = e.InitialSettings ?? new Dictionary<string, object>();
            state.MaxCapacity = e.MaxCapacity;
            state.CurrentCapacity = 0;
            state.IsInitialized = true;
            state.InitializedAt = e.InitializedAt;
            break;
    }
}
```

## Common Anti-Patterns to Avoid

### 1. The "Just This Once" Anti-Pattern
```csharp
// ❌ TEMPTING BUT WRONG - "Just this once" direct modification
public async Task InitializeAsync()
{
    if (State.IsInitialized)
        return;
        
    // This seems harmless, but it breaks Event Sourcing!
    State.IsInitialized = true;
    State.InitializedAt = DateTime.UtcNow;
}

// ✅ CORRECT - Always use events, even for initialization
public async Task InitializeAsync()
{
    if (State.IsInitialized)
        return;
        
    RaiseEvent(new AgentInitializedEvent 
    {
        InitializedAt = DateTime.UtcNow,
        Version = "1.0.0"
    });
    await ConfirmEvents();
}
```

### 2. The "Performance Optimization" Anti-Pattern
```csharp
// ❌ WRONG - "Optimizing" by bypassing events
public async Task BatchUpdateAsync(List<Item> items)
{
    // This is faster but breaks Event Sourcing!
    foreach (var item in items)
    {
        State.Items.Add(item);
    }
    State.LastUpdated = DateTime.UtcNow;
}

// ✅ CORRECT - Batch operations still use events
public async Task BatchUpdateAsync(List<Item> items)
{
    foreach (var item in items)
    {
        RaiseEvent(new ItemAddedEvent 
        {
            Item = item,
            BatchId = Guid.NewGuid(),
            AddedAt = DateTime.UtcNow
        });
    }
    await ConfirmEvents();
}
```

## Debugging Event Sourcing Issues

### 1. Logging Event Patterns
```csharp
protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
{
    Logger.LogDebug("Processing event {EventType} for grain {GrainId}", 
        @event.GetType().Name, this.GetGrainId());
    
    try
    {
        switch (@event)
        {
            case MyEvent e:
                Logger.LogDebug("Applying MyEvent: {Property}", e.Property);
                state.SomeProperty = e.Property;
                break;
        }
        
        Logger.LogDebug("Event processing completed successfully");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to process event {EventType}", @event.GetType().Name);
        throw;
    }
}
```

### 2. State Validation
```csharp
protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
{
    // Validate event before applying
    if (!ValidateEvent(@event))
    {
        Logger.LogWarning("Invalid event received: {EventType}", @event.GetType().Name);
        return;
    }
    
    // Apply state changes
    ApplyEvent(state, @event);
    
    // Validate state after applying
    if (!ValidateState(state))
    {
        Logger.LogError("State validation failed after applying {EventType}", @event.GetType().Name);
        throw new InvalidOperationException("Invalid state after event application");
    }
}
```

## Testing Event Sourcing

### 1. Event Sequence Testing
```csharp
[Fact]
public async Task GAgent_EventSequence_ShouldProduceCorrectState()
{
    // Arrange
    var agent = await _gAgentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    
    // Act - Sequence of operations
    await agent.Operation1Async();
    await agent.Operation2Async();
    await agent.Operation3Async();
    
    // Assert - Final state should reflect all events
    var state = await agent.GetStateAsync();
    state.Value.ShouldBe(expectedFinalValue);
    state.History.Count.ShouldBe(3);
    state.History.ShouldContain("Operation1");
    state.History.ShouldContain("Operation2");
    state.History.ShouldContain("Operation3");
}
```

### 2. Event Validation Testing
```csharp
[Fact]
public async Task GAgent_InvalidEvents_ShouldNotCorruptState()
{
    // Arrange
    var agent = await _gAgentFactory.GetGAgentAsync<IMyGAgent>(Guid.NewGuid());
    
    // Act & Assert - Invalid operations should not change state
    var originalState = await agent.GetStateAsync();
    
    await Assert.ThrowsAsync<ValidationException>(async () => 
    {
        await agent.InvalidOperationAsync();
    });
    
    var newState = await agent.GetStateAsync();
    newState.Value.ShouldBe(originalState.Value);
    newState.Version.ShouldBe(originalState.Version);
}
```

## Summary

### Key Takeaways:
1. **Always use events** - Never modify state directly
2. **Design complete events** - Include all necessary information
3. **Handle transitions properly** - All state changes in `GAgentTransitionState`
4. **Test event sequences** - Verify state changes through events
5. **Log event processing** - Help debugging with detailed logs
6. **Validate events and state** - Prevent corrupted state

### Remember:
- Event Sourcing is not optional in Aevatar - it's core to the architecture
- The Acquire GAgent fixes show the correct patterns to follow
- Every state change must be traceable through events
- Events are the source of truth, not the current state

By following these patterns, you ensure your GAgents are reliable, testable, and maintainable.