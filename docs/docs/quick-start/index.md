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

After running `quickstart.sh`, visit [http://localhost:5000](http://localhost:5000) (should open automatically). The web interface allows you to:
- Select and run any of the provided demos (EventHandlerDemo, MultiGAgentDemo, RouterDemo, YourOwnDemo)
- Input parameters (e.g. greeting for EventHandlerDemo)
- View real-time logs for both Host and Client (with manual refresh)
- For MultiGAgentDemo, view the live chat messages between agents in a dedicated output area

**No need to use command-line arguments for demo selection—everything is available via the website!**

---

## 3. Running the Demos (via Web UI)

### EventHandlerDemo
- Select "EventHandlerDemo" in the web UI.
- Optionally enter a custom greeting.
- Click "Run Demo".
- Check the Host Log for event handling details.

### MultiGAgentDemo
- Select "MultiGAgentDemo" in the web UI.
- **Scenario:** This demo is a multi-agent number guessing game. Alice secretly picks a number (default: 42), and Bob tries to guess it by interacting with Alice through events. The conversation and guesses are recorded and displayed in the MultiGAgentDemo Chat Messages panel.
- The chat panel will appear and auto-refresh every 5 seconds, showing the conversation between Alice and Bob.
- **Customizing the number:** By default, Alice's number is set to 42. Developers can change this number by editing the following line in `src/Aevatar.Workshop.Client/MultiGAgentDemo.cs`:
  ```csharp
  await alice.PrepareAsync(42); // Change 42 to any number between 1 and 100
  ```
  Please ensure the number is between 1 and 100.
- **Note:** This demo may require additional configuration for agent state or dependencies. See code comments for details.

### RouterDemo
- Select "RouterDemo" in the web UI.
- A yellow tip will remind you to configure your API key as described in the Quickstart documentation.
- Check the Host Log for orchestration details and the Client Log for the final report.

### YourOwnDemo
- Select "YourOwnDemo" in the web UI to run your custom demo logic.

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