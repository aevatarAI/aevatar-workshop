
# Aevatar Workshop Quickstart Guide

Welcome to the Aevatar Workshop! This guide will help you get started with the core features of the Aevatar framework, focusing on GAgent collaboration. You'll learn how to run the provided demos, understand the basics of event-driven agent communication, and create your own custom GAgent.

## What is Aevatar?
Aevatar is a framework for building distributed, event-driven systems using agents (GAgents). GAgents can be ordinary agents or AI-powered agents, and they can collaborate to accomplish complex tasks. This workshop repo is designed to help you quickly experience Aevatar's power and flexibility.

---

## Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed
- Git and a Unix-like shell (macOS/Linux recommended)

---

## 1. Clone and Build the Project

```bash
git clone git@github.com:aevatarAI/aevatar-workshop.git
cd aevatar-workshop
sh quickstart.sh
```

The `quickstart.sh` script will:
- Build all projects
- Start the Host service in the background (logs: `host.log`)
- Start the Client service in the background (logs: `client.log`)

> **Tip:** To stop the services, use the `kill` command shown at the end of the script output.

---

## 2. Running the Demos

The Client project supports three demos, each demonstrating a different aspect of GAgent collaboration. You can specify which demo to run by passing a mode parameter to `quickstart.sh`:

- **EventHandlerDemo** (mode 0, default): Basic event handler
- **MultiGAgentDemo** (mode 1): Two GAgents communicating
- **RouterDemo** (mode 2): Complex AI agent scenario

### 2.1 EventHandlerDemo (mode 0)
This demo shows the simplest event handler usage. The client sends a `GreetingEvent` to a GAgent, which logs the greeting.

**Run:**
```bash
sh quickstart.sh 0 "Hello, Aevatar!"
```
- The second argument customizes the greeting message (optional).
- Check `host.log` for output.

### 2.2 MultiGAgentDemo (mode 1)
This demo demonstrates two GAgents (Alice and Bob) communicating via events:
- The client sends a `GreetingEvent` to Alice.
- Alice handles the event and sends a `ReplyEvent` to Bob.
- Bob logs the reply.

**Run:**
```bash
sh quickstart.sh 1
```
- Check `host.log` for the collaboration log between Alice and Bob.

### 2.3 RouterDemo (mode 2)
This demo showcases a more complex scenario with AI agents:
- A router agent coordinates a researcher and a writer agent.
- The researcher gathers information, and the writer generates a report.
- The process is fully automated and demonstrates multi-agent orchestration.

**Run:**
- Configure: Open Host's configuration file (src/Aevatar.Workshop.Host/appsettings.json) and configure the SystemLLMConfigs section. Here we have used Azure OpenAI. Please configure your Endpoint and ApiKey.

```json
{
  "SystemLLMConfigs": {
    "OpenAI": {
      "ProviderEnum": "Azure",
      "ModelIdEnum": "OpenAI",
      "ModelName": "gpt-4o",
      "Endpoint": "",
      "ApiKey": ""
    }
  }
}
```

- Run the demo.
```bash
sh quickstart.sh 2
```
- Check `host.log` for the research and report output.

---

## 3. Understanding the Demos

### EventHandlerDemo
- Shows how a GAgent can handle events using event handler methods.
- Demonstrates the basic event-driven programming model in Aevatar.

### MultiGAgentDemo
- Illustrates how multiple GAgents can collaborate by sending and handling events.
- Alice and Bob are both ordinary GAgents, but the same pattern applies to AI agents.

### RouterDemo
- Demonstrates advanced orchestration with AI agents.
- Shows how to build workflows where agents have specialized roles and interact to complete a task.

---

## 4. Creating Your Own GAgent

You can easily define your own GAgent and use it in the client. Here's how:

### Step 1: Define Your GAgent
Create a new class in `src/Aevatar.Workshop.GAgent/`, e.g. `MyCustomGAgent.cs`:

```csharp
using Aevatar.Core;
using Aevatar.Core.Abstractions;

[GAgent("mycustom", "demo")]
public class MyCustomGAgent : GAgentBase<StateBase, StateLogEventBase<StateLogEventBase>>
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("This is my custom GAgent.");

    [EventHandler]
    public Task HandleMyEventAsync(MyEvent eventData)
    {
        // Your logic here
        return Task.CompletedTask;
    }
}
```

### Step 2: Define Your Event
Create a new event class, e.g. `MyEvent.cs`:

```csharp
using Aevatar.Core.Abstractions;

[GenerateSerializer]
public class MyEvent : EventBase
{
    [Id(0)] public string Message { get; set; }
}
```

### Step 3: Use Your GAgent in the Client
In your client demo (e.g. in `YourOwnDemo.cs`):

```csharp
var myAgent = await gAgentFactory.GetGAgentAsync("mycustom", "demo");
await myAgent.PublishEventAsync(new MyEvent { Message = "Hello from my custom agent!" });
```

---

## 5. Where to Look for Output
- **host.log**: Logs from the Host service (agent backend)
- **client.log**: Logs from the Client (demo execution, agent collaboration)
- **Console**: If you run the client directly, output will also appear in your terminal

---

## 6. Next Steps
- Try modifying the demos or creating your own GAgent and event types
- Explore the `src/Aevatar.Workshop.GAgent/` and `src/Aevatar.Workshop.Client/` directories for more examples
- Read the other docs in the `docs/` directory for deeper dives into GAgent architecture and event handling

---

Happy hacking with Aevatar! 🚀 