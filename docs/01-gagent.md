# Understanding GAgent: The Core of Aevatar

## Introduction

GAgent is the foundational abstraction in the Aevatar Framework, enabling developers to build distributed, event-driven, and stateful applications with ease. Built atop Microsoft Orleans' Virtual Actor Model, GAgent extends the actor paradigm with event sourcing, hierarchical relationships, and robust state management. This document provides a deep dive into the GAgent system, especially the `GAgentBase` class, to help you understand how it empowers your business logic.

---

## 1. GAgentBase: The Heart of the Framework

### What is GAgentBase?

`GAgentBase` is an abstract class that extends Orleans' `JournaledGrain`, providing a rich set of features for building distributed agents (actors) that:

- Maintain event-sourced state
- Communicate via events
- Organize in parent-child hierarchies
- Support dynamic configuration and lifecycle management

GAgentBase is implemented as a set of partial classes, each responsible for a specific aspect of agent behavior (e.g., event publishing, subscription, state transitions, observer management).

**References:**  
- [Core Architecture](https://deepwiki.com/aevatarAI/aevatar-framework/2-core-architecture)  
- [GAgent System](https://deepwiki.com/aevatarAI/aevatar-framework/2.1-gagent-system)

---

## 2. Core Capabilities of GAgentBase

### 2.1 Event Sourcing and State Management

- **Event Sourcing:**  
  Every state change in a GAgent is triggered by an event. Events are persisted, allowing the agent's state to be rebuilt by replaying its event log. This ensures strong consistency and auditability.

- **StateBase:**  
  Each GAgent has a strongly-typed state object (inheriting from `StateBase`), which tracks both business data and hierarchical relationships (parent/children).

- **TransitionState:**  
  The `TransitionState` method applies events to the state, following the event sourcing pattern. Developers override this to define how events mutate state.

- **Automatic Persistence:**  
  State and events are automatically persisted using Orleans' storage providers, ensuring durability and recoverability.

**References:**  
- [State Management](https://deepwiki.com/aevatarAI/aevatar-framework/2.3-state-management)

---

### 2.2 Event-Driven Communication

- **Event Publishing:**  
  GAgents can publish events to other agents or broadcast to their children. The event system is built on Orleans Streams, providing reliable, scalable event delivery.

- **Event Handling:**  
  Methods decorated with `[EventHandler]` are automatically discovered and invoked when relevant events are received. This enables clean, modular business logic.

- **Event Propagation:**  
  Events can flow both upward (child to parent) and downward (parent to children) in the agent hierarchy, supporting complex workflows and coordination patterns.

- **Observer Pattern:**  
  GAgentBase manages event observers, routing events to the correct handler methods using reflection and attribute-based discovery.

**References:**  
- [Event System](https://deepwiki.com/aevatarAI/aevatar-framework/2.2-event-system)

---

### 2.3 Hierarchical Relationships

- **Parent-Child Structure:**  
  Each GAgent can have one parent and multiple children, forming a tree-like structure. This enables delegation, aggregation, and event bubbling.

- **Registration and Subscription:**  
  - `RegisterAsync`: Add a child to the current agent.
  - `SubscribeToAsync`: Set a parent for the current agent.
  - `UnregisterAsync` / `UnsubscribeFromAsync`: Remove relationships.

- **State-Driven Relationships:**  
  Parent and child relationships are tracked in the agent's state and updated via events, ensuring consistency and traceability.

**References:**  
- [GAgent System](https://deepwiki.com/aevatarAI/aevatar-framework/2.1-gagent-system)

---

### 2.4 Lifecycle and Configuration

- **Activation:**  
  GAgents are activated by the Orleans runtime. The `OnActivateAsync` method initializes event streams, observers, and applies configuration.

- **Configuration:**  
  Agents can be configured at runtime using strongly-typed configuration objects (`ConfigAsync`). This allows for dynamic behavior without code changes.

- **Deactivation:**  
  The `OnDeactivateAsync` method handles cleanup, ensuring resources are released and state is safely persisted.

- **Factory Support:**  
  The `GAgentFactory` provides convenient APIs for creating, configuring, and retrieving GAgent instances by type, ID, or alias.

**References:**  
- [Core Architecture](https://deepwiki.com/aevatarAI/aevatar-framework/2-core-architecture)

---

## 3. Anatomy of GAgentBase: Partial Class Responsibilities

GAgentBase is split into several partial classes, each encapsulating a specific concern:

- **GAgentBase.cs**: Core logic, state management, and lifecycle hooks.
- **GAgentBase.Publish.cs**: Event publishing and downward event propagation.
- **GAgentBase.Subscribe.cs**: Event subscription and upward event propagation.
- **GAgentBase.Observers.cs**: Observer management and event handler discovery.

This modular design allows for clean separation of concerns and easier extension.

---

## 4. Developer Experience: Building with GAgent

> **Note:** Logging is built-in—see the [Logger Injection](#logger-injection-built-in-logging-for-your-agents) section below for details.

### 4.1 Creating a Custom GAgent

To implement your own agent:

1. **Define State and Events:**
   ```csharp
   public class MyAgentState : StateBase { ... }
   public class MyEvent : StateLogEventBase<MyEvent> { ... }
   ```

2. **Implement the Agent:**
   ```csharp
   [GAgent]
   public class MyAgent : GAgentBase<MyAgentState, MyEvent>
   {
       public override Task<string> GetDescriptionAsync() => Task.FromResult("My custom agent.");

       [EventHandler]
       public Task HandleMyEventAsync(MyEvent evt) { ... }

       protected override void GAgentTransitionState(MyAgentState state, StateLogEventBase<MyEvent> evt) { ... }
   }
   ```

3. **Register and Use:**
   - Use `GAgentFactory` or Orleans' grain factory to create and interact with your agent.
   - Register parent/child relationships and publish/subscribe to events as needed.

### 4.2 Hierarchical Event Flows

- **Upward Propagation:**  
  Use `SendEventUpwardsAsync` to bubble events to parent agents.

- **Downward Propagation:**  
  Use `SendEventDownwardsAsync` to broadcast events to children.

### 4.3 State Management

- **RaiseEvent:**  
  Use `RaiseEvent<T>` to trigger state changes via events.

- **TransitionState:**  
  Override `TransitionState` to define how events mutate state.

- **HandleStateChangedAsync:**  
  Override to react to state changes (e.g., trigger side effects or notifications).

---

## Logger Injection: Built-in Logging for Your Agents

Aevatar automatically injects a logger into every GAgent via the `Logger` property inherited from `GAgentBase`. This is handled by the framework's internal `AevatarGrainActivator`, so you do **not** need to manually inject or configure a logger in your custom agent classes.

**Usage Example:**
```csharp
public class MyAgent : GAgentBase<MyAgentState, MyEvent>
{
    public async Task DoSomethingAsync()
    {
        Logger.LogInformation("This is a log message from MyAgent.");
    }
}
```

You can use `Logger` for all your logging needs (info, warning, error, etc.) without any extra setup.

---

## 5. Advanced Features

- **Batch Operations:**  
  `IExtGAgent` interface provides batch registration and event publishing for performance.

- **Long-Running Tasks:**  
  GAgents can manage and track long-running operations, integrating with external systems or workflows.

- **Plugin System:**  
  Extend GAgent capabilities via plugins for custom behaviors, integrations, or domain-specific logic.

- **Permission Management:**  
  Built-in support for access control and permission checks at the agent level.

---

## 6. Orleans Integration: Why It Matters

- **Virtual Actor Model:**  
  No manual lifecycle management—actors are activated and deactivated automatically.

- **Scalability:**  
  Effortlessly scale to millions of agents across distributed cloud infrastructure.

- **Fault Tolerance:**  
  Automatic state persistence and recovery ensure resilience.

- **Stream Processing:**  
  Native support for pub/sub and event-driven workflows.

- **Storage Providers:**  
  Pluggable storage for events and state, supporting cloud and on-premises deployments.

**References:**  
- [Orleans Integration](https://deepwiki.com/aevatarAI/aevatar-framework/2-core-architecture)

---

## 7. Summary Table: GAgentBase Capabilities

| Feature                  | Description                                                                 |
|--------------------------|-----------------------------------------------------------------------------|
| Event Sourcing           | Reliable, auditable state changes via persisted events                      |
| State Management         | Strongly-typed, event-driven state with automatic persistence               |
| Event Publishing         | Upward and downward event propagation, pub/sub via Orleans Streams          |
| Hierarchical Structure   | Parent-child relationships for delegation and aggregation                   |
| Observer Management      | Attribute-based event handler discovery and routing                         |
| Configuration            | Strongly-typed, runtime configuration for dynamic behavior                  |
| Lifecycle Management     | Automatic activation, deactivation, and cleanup                            |
| Batch Operations         | Efficient bulk registration and event publishing                            |
| Plugin/Extension System  | Customizable agent behaviors and integrations                               |
| Permission Management    | Built-in access control for secure operations                               |

---

## 8. Further Reading

- [Core Architecture](https://deepwiki.com/aevatarAI/aevatar-framework/2-core-architecture)
- [GAgent System](https://deepwiki.com/aevatarAI/aevatar-framework/2.1-gagent-system)
- [Event System](https://deepwiki.com/aevatarAI/aevatar-framework/2.2-event-system)
- [State Management](https://deepwiki.com/aevatarAI/aevatar-framework/2.3-state-management)

---

## Conclusion

GAgentBase is the engine that powers the Aevatar Framework's distributed, event-driven capabilities. By abstracting away the complexities of distributed systems, event sourcing, and actor lifecycles, it lets you focus on your business logic—while delivering reliability, scalability, and flexibility out of the box.

Whether you're building a simple workflow or a complex, cloud-scale application, GAgent and its ecosystem provide the tools and patterns you need to succeed.

---

**For hands-on guides, API references, and advanced topics, see the rest of the documentation site.** 