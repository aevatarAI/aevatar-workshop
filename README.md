# Aevatar Workshop Quickstart Guide

Welcome to the Aevatar Workshop! This guide will help you get started with the core features of the Aevatar framework, focusing on GAgent collaboration. You'll learn how to run the provided demos, understand the basics of event-driven agent communication, and create your own custom GAgent.

## What is Aevatar?
Aevatar is a framework for building distributed, event-driven systems using agents (GAgents). GAgents can be ordinary agents or AI-powered agents, and they can collaborate to accomplish complex tasks. This workshop repo is designed to help you quickly experience Aevatar's power and flexibility.

---

## Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed
- Git and a Unix-like shell (macOS/Linux recommended)

---

## 1. Clone, Build, and Launch the Web Interface

```bash
git clone git@github.com:aevatarAI/aevatar-workshop.git
cd aevatar-workshop
sh quickstart.sh
```

The `quickstart.sh` script will:
- Build all projects
- Start the Host service in the background (logs: `host.log`)
- Start the Client service in the background (logs: `client.log`)
- **Automatically open the client web interface in your browser** (http://localhost:5000)

> **Tip:** To stop the services, use the `kill` command shown at the end of the script output, or run `./shutdown.sh`.

---

## 2. Using the Web Interface

After running `quickstart.sh`, visit [http://localhost:5000](http://localhost:5000) (should open automatically).

> **All demo instructions and usage tips are now shown directly in the web interface. You do not need to refer to this README for running or understanding the demos.**
> 
> **Tip:** For MultiGAgentDemo, you can now choose the secret number (1-100) directly in the web interface before running the demo.

The web interface allows you to:
- Select and run any of the provided demos (EventHandlerDemo, MultiGAgentDemo, RouterDemo, YourOwnDemo)
- Input parameters (e.g. greeting for EventHandlerDemo)
- View real-time logs for both Host and Client (with manual refresh)
- For MultiGAgentDemo, view the live chat messages between agents in a dedicated output area

**No need to use command-line arguments for demo selection—everything is available via the website!**

---

## 3. Running the Demos (via Web UI)

> **See the web interface for detailed instructions and scenario descriptions for each demo.**

- **EventHandlerDemo:** Event-driven GAgent collaboration. Optionally enter a greeting, run the demo, and check Host Log for details.
- **MultiGAgentDemo:** Multi-agent number guessing game. Alice picks a secret number (default 42, you can choose 1-100 in the web UI), Bob guesses. Chat and guesses are shown in the chat panel. No need to edit code to change the number.
- **RouterDemo:** AI routing and multi-agent orchestration. Requires API key configuration. Host Log shows orchestration, Client Log shows the final report.
- **YourOwnDemo:** Run your own custom demo logic. Extend in `YourOwnDemo.cs`.

---

## 4. MultiGAgentDemo Configuration

If you want to use advanced features in MultiGAgentDemo (such as AI agents or persistent state), you may need to configure additional settings or provide API keys.

Open Host's configuration file (`src/Aevatar.Workshop.Host/appsettings.json`) and configure the SystemLLMConfigs section. Here we have used Azure OpenAI. Please configure your Endpoint and ApiKey.

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

---

## 5. RouterDemo Configuration

Before running RouterDemo, you must configure your API key and endpoint:

Open Host's configuration file (`src/Aevatar.Workshop.Host/appsettings.json`) and configure the SystemLLMConfigs section. Here we have used Azure OpenAI. Please configure your Endpoint and ApiKey.

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

---

## 6. Creating Your Own GAgent

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
var publisher = await gAgentFactory.GetGAgentAsync<IPublishingGAgent>();
await publisher.PublishEventAsync(new MyEvent { Message = "Hello from my custom agent!" }, myAgent);
```

---

## 7. Where to Look for Output
- **host.log**: Logs from the Host service (agent backend)
- **client.log**: Logs from the Client (demo execution, agent collaboration)
- **Website**: All demo results, logs, and agent chat messages are visible in the web interface

---

## 8. Next Steps
- Try modifying the demos or creating your own GAgent and event types
- Explore the `src/Aevatar.Workshop.GAgent/` and `src/Aevatar.Workshop.Client/` directories for more examples
- Read the other docs in the `docs/` directory for deeper dives into GAgent architecture and event handling

---

Happy hacking with Aevatar! 🚀 