# GAgent + MongoDB: Event Sourcing Log and State Persistence Guide

## 1. Introduction & Goals

In modern distributed systems, **Event Sourcing** is a powerful approach for persistence and consistency. Instead of storing only the final state, it records every state-changing event, enabling traceability, recovery, and scalability. The Aevatar framework, built on the Microsoft Orleans virtual actor model, is naturally suited for event sourcing.

**Why MongoDB?**  
MongoDB offers high availability, scalability, and a flexible document model—making it ideal for storing event logs and snapshots. Aevatar abstracts its storage layer, allowing developers to switch persistence backends seamlessly. MongoDB is one of the officially recommended distributed event log storage solutions.

**Goal:** This article provides a step-by-step guide to configuring and using MongoDB as the event sourcing backend for GAgent in Aevatar, and explains the core implementation details.

---

## 2. Configuring Aevatar to Use MongoDB Persistence

### 2.1 Configure the MongoDB Connection String

In your Silo project (e.g., `samples/ArtifactGAgent/ArtifactGAgent.Silo`), add the following to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "mongodb://127.0.0.1:27017/AevatarDb"
  }
}
```

### 2.2 Enable MongoDB Event Sourcing in Silo Startup

In `Program.cs`, use the Aevatar extension method to register MongoDB persistence:

```csharp
using Aevatar.EventSourcing.MongoDB.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .UseOrleans(silo =>
    {
        silo.AddMemoryGrainStorage("Default")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .AddMemoryGrainStorage("PubSubStore")
            // Key: Register MongoDB as the LogConsistencyProvider
            .AddMongoDbStorageBasedLogConsistencyProvider(options =>
            {
                options.Database = "AevatarDb";
                // You can also configure more MongoDB options via options.ClientSettings
            })
            .UseLocalhostClustering()
            .UseAevatar()
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information).AddConsole());
    })
    .UseConsoleLifetime();
```

### 2.3 About the Extension Method

`AddMongoDbStorageBasedLogConsistencyProvider` is provided by `MongoDbStorageSiloBuilderExtensions` and supports multiple overloads for custom provider names and detailed options. Internally, it registers MongoDB storage services into the Orleans Silo lifecycle via dependency injection.

---

## 3. How Aevatar.EventSourcing.Core and Aevatar.EventSourcing.MongoDb Work Together

### 3.1 Storage Abstraction: ILogConsistentStorage

Aevatar's event sourcing mechanism is based on the `ILogConsistentStorage` interface (in `Aevatar.EventSourcing.Core`), which defines the core operations for reading, appending, and querying event log versions:

```csharp
public interface ILogConsistentStorage
{
    Task<IReadOnlyList<TLogEntry>> ReadAsync<TLogEntry>(string grainTypeName, GrainId grainId, int fromVersion, int maxCount);
    Task<int> GetLastVersionAsync(string grainTypeName, GrainId grainId);
    Task<int> AppendAsync<TLogEntry>(string grainTypeName, GrainId grainId, IList<TLogEntry> entries, int expectedVersion);
}
```

### 3.2 MongoDB Implementation: MongoDbLogConsistentStorage

`Aevatar.EventSourcing.MongoDb` provides the `MongoDbLogConsistentStorage` implementation, which interacts with MongoDB:

- **ReadAsync**: Queries event logs by GrainId and version range, deserializing them into event objects.
- **AppendAsync**: Atomically appends event logs, ensuring version consistency.
- **GetLastVersionAsync**: Retrieves the latest event version number.

Example snippet:

```csharp
public async Task<IReadOnlyList<TLogEntry>> ReadAsync<TLogEntry>(string grainTypeName, GrainId grainId, int fromVersion, int maxCount)
{
    var collection = GetDatabase().GetCollection<BsonDocument>(GetStreamName(grainId));
    var filter = Builders<BsonDocument>.Filter.And(
        Builders<BsonDocument>.Filter.Eq("GrainId", grainId.ToString()),
        Builders<BsonDocument>.Filter.Gte("Version", fromVersion)
    );
    var documents = await collection.FindAsync(filter, new FindOptions { Limit = maxCount });
    // Deserialize to event objects
}
```

### 3.3 Event Sourcing Flow: LogViewAdaptor

`LogViewAdaptor` (in `Aevatar.EventSourcing.Core`) orchestrates the event sourcing flow:

- Uses `ILogConsistentStorage` to read/write event logs.
- Maintains snapshots and versions for efficient recovery and concurrency control.
- Handles retries, exceptions, and consistency checks automatically.

---

## 4. Key Code Snippets & Best Practices

### 4.1 Complete Configuration Example

**appsettings.json**:

```json
{
  "ConnectionStrings": {
    "Default": "mongodb://127.0.0.1:27017/AevatarDb"
  }
}
```

**Program.cs**:

```csharp
silo.AddMongoDbStorageBasedLogConsistencyProvider(options =>
{
    options.Database = "AevatarDb";
    // Configure more MongoDB options as needed
});
```

### 4.2 Core Implementation for Log and State Persistence

- Log writes use optimistic concurrency control via version numbers to prevent conflicts.
- State snapshots and event logs are stored separately for efficient recovery.
- Custom serializers are supported for complex object structures.

### 4.3 Recommended Practices

- **Collection Design**: Use separate collections per business or grain type for better query performance.
- **Snapshot Frequency**: Adjust snapshot strategy based on event volume and recovery needs.
- **Monitoring**: Track MongoDB write latency and errors, and scale out as needed.

---

## 5. FAQ & Troubleshooting

**Reliability**: Aevatar + MongoDB combines Orleans' distributed consistency with MongoDB's high availability, suitable for large-scale event-driven systems.

**Scalability**: The storage abstraction layer allows switching to other backends (e.g., in-memory, SQL) for different environments.

**Common Issues:**

- **Connection Failure**: Check MongoDB connection string and network connectivity.
- **Version Conflict**: Pay attention to the `expectedVersion` parameter in `AppendAsync` to ensure event order.
- **Performance Bottlenecks**: Use sharding, indexing, and batch writes for optimization.

---

For deeper implementation details or specific scenarios, feel free to explore the source code or reach out for further discussion. 