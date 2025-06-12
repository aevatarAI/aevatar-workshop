# Aevatar SignalR: 5-Minute Quickstart & 15-Minute Architecture Deep Dive

## 🚀 5-Minute Quickstart: SignalR & GAgent Event Interaction

### 1. Client Usage Pattern

Just three steps to enable real-time event communication between your frontend and backend GAgent:

**(1) Establish SignalR Connection**
```csharp
const string hubUrl = "http://localhost:5001/aevatarHub";
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("ReceiveResponse", (message) =>
{
    Console.WriteLine($"[Event] {message}");
});

await connection.StartAsync();
```

**(2) Publish Event to GAgent**
```csharp
var grainType = "SignalRSample.GAgents.signalR";
var grainKey = "test".ToGuid().ToString("N");
var eventTypeName = typeof(NaiveTestEvent).FullName!;
var eventJson = JsonConvert.SerializeObject(new { Greeting = "Test message" });

await connection.InvokeAsync("PublishEventAsync",
    GrainId.Create(grainType, grainKey),
    eventTypeName,
    eventJson);
```

**(3) Receive GAgent Response**
- The GAgent processes the event and pushes the result back to the client via the `ReceiveResponse` method.

> See the complete runnable example in: `samples/SignalRSample.Client/Program.cs`  
> For more details, refer to the official [README.md](https://github.com/aevatarAI/aevatar-signalR/blob/dev/README.md)

---

## 🧩 Architecture Deep Dive

### 1. Architecture Overview

- **Aevatar SignalR** bridges the Orleans distributed GAgent system and SignalR clients, supporting elastic scaling, event sourcing, and AI integration.
- Main components:
    - `AevatarSignalRHub`: SignalR entry point, distributed connection management.
    - `SignalRGAgent`: Bridge for event forwarding and response.
    - `GAgent`: Core for business logic and event handling.

### 2. Event Flow Mechanism

1. **Client** connects to `/aevatarHub` via SignalR and calls `PublishEventAsync` to send an event.
2. **SignalRGAgent** receives the event and forwards it to the target GAgent.
3. **GAgent** processes the event via `[EventHandler]` methods and responds with `PublishAsync`.
4. **SignalRGAgent** pushes the response back to the client via SignalR (`ReceiveResponse`).
5. The entire process is distributed, asynchronous, and ensures event consistency.

> See event definition and handler examples in:
> - `samples/SignalRSample.GAgents/SignalRTestGAgent.cs`
> - `samples/SignalRSample.GAgents/NaiveTestEvent.cs`

### 3. Server Registration & Extensibility

- The server registers the Hub and GAgent as follows:
```csharp
builder.Host.UseOrleans(silo =>
{
    silo.UseAevatar()
        .UseSignalR()
        .RegisterHub<AevatarSignalRHub>();
});
app.MapHub<AevatarSignalRHub>("/aevatarHub");
```
- GAgents are obtained and registered via `IGAgentFactory`, supporting multi-tenancy, plugins, and AI capabilities.

### 4. Architectural Highlights

- **Elastic Scalability**: Built on Orleans virtual actor model, naturally supports horizontal scaling.
- **Event Sourcing**: All events are persisted in MongoDB, enabling replay and audit.
- **Multi-Tenancy & Plugins**: Each tenant can dynamically load isolated plugins.
- **AI Integration**: Built-in Semantic Kernel, supports multiple AI service connectors.
- **Developer Experience**: Just define events and GAgents—distributed messaging and orchestration are fully managed by the framework.

> For in-depth principles and architecture, see:  
> [Aevatar Framework DeepWiki](https://deepwiki.com/aevatarAI/aevatar-framework)  
> [Aevatar SignalR DeepWiki](https://deepwiki.com/aevatarAI/aevatar-signalR)

---

## 🏗️ In-Depth: Core Components and Technical Details

### AevatarSignalRHub: The Distributed SignalR Entry Point

`AevatarSignalRHub` is the main SignalR hub for the system. All client connections, messages, and disconnections are routed through this entry point. Its core responsibilities and implementation details include:

- **Distributed Connection Management**: Integrates with `OrleansHubLifetimeManager`, which stores connection state in Orleans grains. This enables seamless horizontal scaling—connections are not tied to a single server instance.
- **Event Reception and Dispatch**: Exposes hub methods (such as `PublishEventAsync`) that receive events from clients and forward them to the appropriate `SignalRGAgent` for processing.
- **Response Routing**: Receives responses from GAgents (via SignalRGAgent) and pushes them back to the correct client using the `ReceiveResponse` method.
- **Decoupling and Scalability**: By decoupling SignalR connection logic from business logic and distributing state, the hub supports multi-instance deployments and high availability.

> For more, see [DeepWiki: SignalR Hub Implementation](https://deepwiki.com/aevatarAI/aevatar-signalR)

### SignalRGAgent: The Bridge Between SignalR and GAgent

`SignalRGAgent` is an Orleans grain that acts as the bridge between SignalR clients and the distributed GAgent system. Its technical responsibilities include:

- **Event Forwarding**: Receives events from `AevatarSignalRHub` and determines the target GAgent instance to handle the event.
- **Asynchronous Processing**: Uses internal async queues and retry logic to ensure reliable message delivery and processing, even in the face of transient failures.
- **Response Publishing**: After the GAgent processes an event, `SignalRGAgent` pushes the response back to the client through the SignalR channel.
- **Extensibility**: Supports multi-tenancy, plugin loading, and AI integration, making it a flexible point for advanced features.

> For more, see [README](https://github.com/aevatarAI/aevatar-signalR/blob/dev/README.md) and [DeepWiki: GAgent Integration](https://deepwiki.com/aevatarAI/aevatar-signalR)

### Supporting Grains: State and Routing Infrastructure

The system leverages several specialized Orleans grains to manage distributed state and routing:

#### ClientGrain
- **Purpose**: Each SignalR connection is mapped to a `ClientGrain`, which manages the state of that individual connection.
- **Responsibilities**: Tracks connection status, handles message buffering, and supports reconnection scenarios.
- **Benefit**: Ensures that client state is durable and available across server restarts or scaling events.

#### ConnectionGroupGrain
- **Purpose**: Implements SignalR's group messaging semantics in a distributed fashion.
- **Responsibilities**: Manages sets of connection IDs for each group, supports broadcasting to groups, and allows for excluding specific connections from group messages.
- **Benefit**: Enables efficient, scalable group messaging and user-based routing across the cluster.

#### ServerDirectoryGrain
- **Purpose**: Acts as a singleton directory for all server instances in the cluster.
- **Responsibilities**: Tracks active servers, coordinates load balancing, and assists in routing messages to the correct server instance.
- **Benefit**: Supports high availability and efficient routing in multi-instance deployments.

> For more details, see [DeepWiki: Orleans Distributed State Management](https://deepwiki.com/aevatarAI/aevatar-signalR)