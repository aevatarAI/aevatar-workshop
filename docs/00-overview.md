# Overview

Aevatar is a distributed, event-sourced actor framework built on Microsoft Orleans. It empowers developers to build scalable, reliable, and cloud-native applications with minimal friction.

---

## Core Strengths

### Easy To Use
Aevatar offers a plug-and-play experience. With clear APIs and minimal configuration, you can launch your first distributed agent in minutes.

### Focused on What Matters
The framework abstracts away infrastructure complexity, letting you focus on business logic and event flows, not boilerplate or plumbing code.

### Powered by Orleans / Virtual Actor Model
Aevatar leverages Microsoft Orleans' Virtual Actor Model, ensuring high scalability, reliability, and seamless state management for distributed systems.

---

## Get Started in 5 Minutes

Aevatar is designed for rapid onboarding. You can:

- **Clone the repository**
- **Install dependencies**
- **Run the sample or test project locally**

A typical developer can see a working distributed agent system in under five minutes. (See the Quick Start section for step-by-step instructions.)

---

## Customizing Your Own GAgent

Aevatar makes it easy to define your own business logic by customizing GAgent classes. For example, to create an AI-powered agent:

```csharp
[GAgent]
public class SampleAIGAgent : GAgentBase<SampleAIGAgentState, SampleAIStateLogEvent>, ISampleAIGAgent
{
    public override Task<string> GetDescriptionAsync() => Task.FromResult("An AI GAgent sample.");

    public async Task PretendingChatAsync(string message)
    {
        // Business logic here
        RaiseEvent(new TokenUsageStateLogEvent { ... });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(SampleAIGAgentState state, StateLogEventBase<SampleAIStateLogEvent> @event)
    {
        // State transition logic
    }
}
```

**Key points:**
- Inherit from `GAgentBase<TState, TEvent>`
- Use `[GAgent]` attribute for automatic registration
- Implement your business methods and state transitions
- Use event sourcing and state management out of the box

Explore `test/Aevatar.Core.Tests/TestGAgents/` for more real-world agent examples, including hierarchical agents, event handlers, and stateful business logic.

---

## Why Orleans? Cloud-Native Reliability and Scalability

Aevatar is built on Microsoft Orleans, a battle-tested framework powering large-scale cloud services (e.g., Xbox, Halo, Azure). Orleans provides:

- **Virtual Actor Model**: No manual lifecycle management; actors are automatically activated/deactivated as needed.
- **Automatic State Persistence**: State is reliably stored and recovered, even in the face of failures.
- **Scalability**: Effortlessly scale out to thousands or millions of actors across cloud nodes.
- **Fault Tolerance**: Built-in recovery and supervision for robust, always-on services.
- **Stream Processing**: Native support for pub/sub and event-driven architectures.

With Aevatar, you inherit all these benefits, letting you focus on your domain logic while running reliably at any scale, from local dev to global cloud.

---

**In summary:**  
Aevatar lets you build, run, and scale distributed, event-driven applications in minutes—without worrying about the underlying complexity.  
Start with a sample agent, customize your logic, and deploy with confidence, powered by Orleans.