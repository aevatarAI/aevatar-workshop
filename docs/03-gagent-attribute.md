# The `[GAgent]` Attribute: Defining and Discovering Your Agents

## Introduction

The `[GAgent]` attribute is a key part of the Aevatar framework, enabling developers to register their custom agents (GAgents) with unique namespaces and aliases. This makes it easy to organize, discover, and retrieve agents using the `GAgentFactory`.

---

## What Does `[GAgent]` Do?

- **Registers your class as a GAgent** so it can participate in the Aevatar ecosystem.
- **Defines a unique namespace and alias** for your agent, which are used for identification and lookup.
- **Enables dynamic retrieval** of your agent via the `GAgentFactory` by type, alias, or namespace.

The attribute is implemented as `GAgentAttribute` in the framework and is applied to your agent class.

---

## Usage

### Basic Usage

```csharp
[GAgent]
public class MyAgent : GAgentBase<MyState, MyEvent> { ... }
```

This registers `MyAgent` with its default namespace (the class's namespace) and alias (the class name).

### Custom Alias

```csharp
[GAgent("custom-alias")]
public class MyAgent : GAgentBase<MyState, MyEvent> { ... }
```

This registers `MyAgent` with the alias `custom-alias` in its default namespace.

### Custom Alias and Namespace

```csharp
[GAgent("custom-alias", "custom.namespace")]
public class MyAgent : GAgentBase<MyState, MyEvent> { ... }
```

This registers `MyAgent` with the alias `custom-alias` in the namespace `custom.namespace`.

---

## How It Works

The attribute implements `IGrainTypeProviderAttribute` and provides a `GetGrainType` method. This method determines the unique grain type string based on the alias and namespace you provide, or falls back to the class's namespace and name.

This unique identifier is used by the `GAgentFactory` to locate and instantiate your agent:

```csharp
var agent = await gAgentFactory.GetGAgentAsync("custom-alias", "custom.namespace");
```

---

## Best Practices

- Use clear, descriptive aliases and namespaces to avoid conflicts and improve discoverability.
- Stick to a consistent naming convention across your project.
- Document your agent's purpose and registration details for maintainability.

---

## Further Reading

- [GAgent Documentation](./01-gagent.md)
- [Aevatar Overview](./00-overview.md)
- [GAgentFactory Usage](https://deepwiki.com/aevatarAI/aevatar-framework/2.1-gagent-system)

---

By leveraging the `[GAgent]` attribute, you can easily organize and retrieve your distributed agents, making your Aevatar-based applications more modular and maintainable. 