# GAgentFactory: Creating and Managing Agents in Aevatar

## Introduction

`GAgentFactory` is the central utility for creating, retrieving, and configuring GAgents in the Aevatar framework. It abstracts away the complexity of Orleans grain instantiation and ensures that your agents are properly activated and configured before use.

For context on GAgent design and registration, see the [GAgent documentation](./01-gagent.md) and [GAgent Attribute](./03-gagent-attribute.md).

---

## What Does GAgentFactory Do?

- **Creates and retrieves GAgents** by type, alias, namespace, or primary key.
- **Ensures agents are activated** before you interact with them.
- **Applies configuration** to agents at creation time, supporting dynamic and flexible agent behavior.
- **Supports both generic and strongly-typed access** to agents.
- **Supports artifact GAgents and batch operations.**

---

## How It Works

GAgentFactory wraps the Orleans `IClusterClient` and provides a set of convenient methods for agent instantiation:

### 1. By GrainId
```csharp
var guid = Guid.NewGuid().ToString("N");
var grainId = GrainId.Create("test.group", guid);
var gAgent = await gAgentFactory.GetGAgentAsync(grainId);
```
- Retrieves a GAgent by its unique GrainId.
- Ensures the agent is activated and ready.

### 2. By Alias and Namespace
```csharp
var gAgent = await gAgentFactory.GetGAgentAsync("naiveTest", "Aevatar.Core.Tests.TestGAgents");
```
- Uses the alias and namespace (as defined by `[GAgent]`) to locate the agent.

### 3. By Type
```csharp
var gAgent = await gAgentFactory.GetGAgentAsync(typeof(MyAgent));
```
- Uses the agent's type to resolve the correct grain.

### 4. By Generic Interface
```csharp
var gAgent = await gAgentFactory.GetGAgentAsync<IMyAgent>();
```
- Strongly-typed access for compile-time safety.

### 5. With Configuration
```csharp
var config = new MyAgentConfig { Greeting = "Hello" };
var gAgent = await gAgentFactory.GetGAgentAsync<IMyAgent>(Guid.NewGuid(), config);
```
- Passes a configuration object to the agent, which is applied via `ConfigAsync` after activation.
- Useful for dynamic initialization and runtime customization.

### 6. Artifact GAgents
```csharp
var artifactGAgent = await gAgentFactory.GetArtifactGAgentAsync<MyArtifact, MyArtifactGAgentState, MyArtifactStateLogEvent>();
```
- Supports advanced scenarios for artifact-based agents.

---

## Real-World Usage Patterns (from Tests)

The following examples are inspired by the [GAgentFactoryTests](https://github.com/aevatarAI/aevatar-framework/blob/dev/test/Aevatar.GAgents.Tests/GAgentFactoryTests.cs):

- **Create by GrainId:**
  - Ensures the agent's primary key matches the GrainId.
  - Verifies event subscriptions are set up.

- **Create by Generic Type:**
  - Supports multiple agent types and verifies correct state and event subscriptions.

- **Create with Configuration:**
  - Applies configuration and checks that the agent's state reflects the config (e.g., a greeting message is stored).

- **Create by Alias:**
  - Demonstrates flexibility in agent lookup using human-friendly names.

- **Create by GAgent Type:**
  - Shows that agents can be retrieved by their .NET type, supporting refactoring and code navigation.

- **Get Available Types:**
  - The factory and manager can enumerate all available GAgent types and grain types, supporting dynamic discovery and tooling.

- **Artifact GAgents:**
  - Demonstrates advanced usage for artifact-based agents, including event subscriptions and artifact retrieval.

---

## Example: Full Lifecycle

```csharp
// 1. Create or retrieve an agent by alias and namespace
var gAgent = await gAgentFactory.GetGAgentAsync("naiveTest", "Aevatar.Core.Tests.TestGAgents");

// 2. Activate and configure the agent
await gAgent.ActivateAsync();
await gAgent.ConfigAsync(new NaiveGAgentConfiguration { Greeting = "Test" });

// 3. Use the agent
var state = await gAgent.GetStateAsync();
Console.WriteLine(state.Content.First()); // Should output "Test"
```

---

## Advanced Tips & Troubleshooting

- **Always use the factory:** Direct grain instantiation may skip activation/configuration logic.
- **Check event subscriptions:** Use `GetAllSubscribedEventsAsync()` to verify your agent is listening for the right events.
- **Configuration is optional:** If you don't need to customize the agent, you can omit the config parameter.
- **Type safety:** Prefer generic methods for compile-time checks.
- **Artifact agents:** Use the artifact-specific methods for advanced scenarios.
- **Dynamic discovery:** Use the manager to list all available agent types and grain types for diagnostics or tooling.

---

## Best Practices

- Use aliases and namespaces (via `[GAgent]`) for clear, conflict-free agent identification.
- Pass configuration objects when needed to customize agent behavior at runtime.
- Prefer strongly-typed interfaces for compile-time safety and clarity.
- Use the manager and resolver for advanced discovery and diagnostics.

---

## Further Reading

- [GAgent Documentation](./01-gagent.md)
- [GAgent Attribute](./03-gagent-attribute.md)
- [Aevatar Overview](./00-overview.md)
- [GAgentFactoryTests (source)](https://github.com/aevatarAI/aevatar-framework/blob/dev/test/Aevatar.GAgents.Tests/GAgentFactoryTests.cs)

---

By using GAgentFactory, you can manage your distributed agents with confidence, ensuring they are always ready, properly configured, and easy to discover in your Aevatar-powered applications. 