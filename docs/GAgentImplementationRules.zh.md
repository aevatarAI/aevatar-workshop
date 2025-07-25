# GAgent 实现规则和最佳实践

本文档为在 Aevatar 框架中实现 GAgent 提供全面指导。

## 🚫 常见错误避免

### ❌ 不要在构造函数中注入 logger
```csharp
// 错误
private readonly ILogger<MyGAgent> _logger;
public MyGAgent(ILogger<MyGAgent> logger)
{
    _logger = logger;
}
```

### ✅ 使用 GAgentBase 内置的 Logger 属性
```csharp
// 正确
Logger.LogInformation("Processing event: {EventType}", eventType);
```

### ❌ 不要使用 IGrainFactory 获取 GAgent
```csharp
// 错误
var grainFactory = ServiceProvider.GetRequiredService<IGrainFactory>();
var myAgent = grainFactory.GetGrain<IMyGAgent>(grainId);
```

### ✅ 使用 IGAgentFactory 获取 GAgent
```csharp
// 正确
var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
var myAgent = await gAgentFactory.GetGAgentAsync<IMyGAgent>(grainId);
```

### ❌ 不要直接修改 State
```csharp
// 错误 - 直接修改状态
State.Counter++;
State.Items.Add(item);
```

### ✅ 使用事件溯源模式
```csharp
// 正确 - 使用 RaiseEvent 和 ConfirmEvents
RaiseEvent(new CounterIncrementedEvent { IncrementBy = 1 });
RaiseEvent(new ItemAddedEvent { Item = item });
await ConfirmEvents();
```

## 📋 GAgent 实现指南

### 1. 首先定义接口（必需）

每个 GAgent 必须在实现之前先定义接口：

```csharp
// 普通 GAgent（无 AI 功能）
public interface IMyGAgent : IStateGAgent<MyState>
{
    // 添加你的 GAgent 特定的自定义方法
    Task<MyResult> ProcessDataAsync(MyData data);
}

// 支持 AI 的 GAgent
public interface IMyAIGAgent : IStateGAgent<MyAIState>, IAIGAgent
{
    // IAIGAgent 提供 AI 相关能力
    // 添加你的自定义方法
    Task<string> GenerateResponseAsync(string prompt);
}
```

### 2. 基本结构

```csharp
using Aevatar.Core;
using Aevatar.Core.Abstractions;

[GAgent("my-agent", "workshop")]       // 必需的属性
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    // 不要带参数的构造函数！
    
    // 简化的懒加载服务
    private IGAgentFactory GAgentFactory => 
        ServiceProvider.GetRequiredService<IGAgentFactory>();
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("这个 GAgent 功能的详细描述");
    }
    
    // 实现接口方法
    public async Task<MyResult> ProcessDataAsync(MyData data)
    {
        // 实现逻辑
    }
}
```

### 3. GAgent 属性规则

`[GAgent]` 属性是**必需的**，但参数是可选的：

```csharp
// 无参数 - 使用 type.Namespace + "." + type.Name
[GAgent]
public class MyGAgent  // 将会是："MyNamespace.MyGAgent"

// 仅别名 - 使用 type.Namespace + "." + alias
[GAgent("my-agent")]
public class MyGAgent  // 将会是："MyNamespace.my-agent"

// 别名和命名空间 - 使用 namespace + "." + alias
[GAgent("my-agent", "custom-ns")]
public class MyGAgent  // 将会是："custom-ns.my-agent"
```

### 4. 状态定义

```csharp
[GenerateSerializer]
public class MyState : StateBase
{
    [Id(0)] public string Property1 { get; set; } = string.Empty;
    [Id(1)] public int Counter { get; set; }
    // 始终初始化集合！
    [Id(2)] public List<string> Items { get; set; } = new();
    [Id(3)] public Dictionary<string, int> Metrics { get; set; } = new();
}
```

### 5. 状态日志事件

```csharp
[GenerateSerializer]
public class MyStateLogEvent : StateLogEventBase<MyStateLogEvent>
{
}

// 定义特定的状态变更事件
[GenerateSerializer]
public class PropertyUpdatedEvent : MyStateLogEvent
{
    [Id(0)] public string NewValue { get; set; } = string.Empty;
}

[GenerateSerializer]
public class CounterIncrementedEvent : MyStateLogEvent
{
    [Id(0)] public int IncrementBy { get; set; }
}
```

### 6. 状态管理（Orleans 事件溯源）

**必须**遵循 Orleans 事件溯源模式：

```csharp
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    [EventHandler]
    public async Task HandleUpdateAsync(UpdateEvent @event)
    {
        // 步骤 1：触发状态事件
        RaiseEvent(new PropertyUpdatedEvent 
        { 
            NewValue = @event.Value 
        });
        
        RaiseEvent(new CounterIncrementedEvent 
        { 
            IncrementBy = 1 
        });
        
        // 步骤 2：确认事件（持久化并应用）
        await ConfirmEvents();
        
        // 步骤 3：发布任何响应事件
        await PublishAsync(new UpdateCompletedEvent 
        { 
            UpdatedAt = DateTime.UtcNow 
        });
    }
    
    // 步骤 4：重写 GAgentTransitionState 进行自定义状态转换
    protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
    {
        switch (@event)
        {
            case PropertyUpdatedEvent e:
                state.Property1 = e.NewValue;
                break;
            case CounterIncrementedEvent e:
                state.Counter += e.IncrementBy;
                break;
        }
    }
}
```

### 7. 事件处理器类型

#### 类型 1：带属性的常规事件处理器
```csharp
[EventHandler(priority: 100, allowSelfHandling: false)]
public async Task HandleMyEventAsync(MyEvent @event)
{
    Logger.LogInformation("Handling event: {EventId}", @event.Id);
    // 处理事件
}
```

#### 类型 2：全事件处理器
```csharp
[AllEventHandler(allowSelfHandling: false)]
public Task HandleAllEventsAsync(EventWrapperBase eventWrapper)
{
    // 如需要可使用反射提取事件信息
    var eventType = eventWrapper.GetType();
    Logger.LogDebug("Received event of type: {Type}", eventType.Name);
    return Task.CompletedTask;
}
```

#### 类型 3：默认处理器（基于约定）
```csharp
// 如果方法名恰好是 "HandleEventAsync" 且参数类型不是抽象的，则不需要属性
public async Task HandleEventAsync(MyConcreteEvent @event)
{
    Logger.LogInformation("Default handler for: {EventType}", @event.GetType().Name);
    // 这将自动被识别为事件处理器
}
```

### 8. 服务访问模式（简化版）

使用直接的懒加载属性访问服务：

```csharp
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    // 简化的服务访问 - 不需要支持字段
    private IGAgentFactory GAgentFactory => 
        ServiceProvider.GetRequiredService<IGAgentFactory>();
    
    private IGAgentService GAgentService => 
        ServiceProvider.GetRequiredService<IGAgentService>();
    
    public async Task DoSomethingAsync()
    {
        // 直接使用服务
        var otherAgent = await GAgentFactory.GetGAgentAsync<IOtherGAgent>(Guid.NewGuid());
        var info = await GAgentService.GetGAgentInfoAsync(this.GetGrainId());
    }
}
```

### 9. GAgent 间的事件发布通信

```csharp
// GAgent 必须在同一组中（父子关系）

// 设置组通信
public async Task SetupGroupCommunicationAsync()
{
    var publisher = await GAgentFactory.GetGAgentAsync<IPublishingGAgent>();
    
    // 将此代理注册为子代理
    await publisher.RegisterAsync(this);
    
    // 注册其他代理
    var processor = await GAgentFactory.GetGAgentAsync<IProcessorGAgent>(Guid.NewGuid());
    await publisher.RegisterAsync(processor);
    
    // 现在代理可以通过事件通信
    await publisher.PublishEventAsync(new ProcessDataEvent(), processor);
}

// 直接发布（仅在同组内有效）
await PublishAsync(new MyEvent());
```

## 🛠️ 完整示例

### 文件：src/GAgents/DataProcessorGAgent.cs
```csharp
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// 接口定义
public interface IDataProcessorGAgent : IStateGAgent<DataProcessorState>
{
    Task<DataProcessorStatistics> GetStatisticsAsync();
}

// 状态定义
[GenerateSerializer]
public class DataProcessorState : StateBase
{
    [Id(0)] public List<string> ProcessedDataIds { get; set; } = new();
    [Id(1)] public Dictionary<string, string> Results { get; set; } = new();
    [Id(2)] public int TotalProcessed { get; set; }
    [Id(3)] public DateTime? LastProcessedAt { get; set; }
}

// 统计信息 DTO
[GenerateSerializer]
public class DataProcessorStatistics
{
    [Id(0)] public int TotalProcessed { get; set; }
    [Id(1)] public DateTime? LastProcessedAt { get; set; }
    [Id(2)] public List<string> ProcessedDataIds { get; set; } = new();
}

// 必需的 GAgent 属性
[GAgent("data-processor", "workshop")]
public class DataProcessorGAgent : GAgentBase<DataProcessorState, DataProcessorStateLogEvent>, IDataProcessorGAgent
{
    // 状态日志事件基类（此 GAgent 内部）
    [GenerateSerializer]
    public class DataProcessorStateLogEvent : StateLogEventBase<DataProcessorStateLogEvent>
    {
    }

    // 特定的状态变更事件（此 GAgent 内部）
    [GenerateSerializer]
    public class DataProcessedLogEvent : DataProcessorStateLogEvent
    {
        [Id(0)] public string DataId { get; set; } = string.Empty;
        [Id(1)] public string Result { get; set; } = string.Empty;
        [Id(2)] public DateTime ProcessedAt { get; set; }
    }

    // 简化的服务访问
    private IGAgentFactory GAgentFactory => ServiceProvider.GetRequiredService<IGAgentFactory>();
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("处理传入数据并跟踪统计信息。支持批处理和实时更新。");
    }
    
    [EventHandler]
    public async Task HandleProcessDataEventAsync(ProcessDataEvent @event)
    {
        Logger.LogInformation("Processing data: {DataId} of type {DataType}", 
            @event.DataId, @event.DataType);
        
        try
        {
            // 处理数据
            var result = await ProcessDataAsync(@event.Data);
            
            // 使用事件溯源更新状态
            RaiseEvent(new DataProcessedLogEvent
            {
                DataId = @event.DataId,
                Result = result,
                ProcessedAt = DateTime.UtcNow
            });
            
            // 确认状态变更
            await ConfirmEvents();
            
            // 发布完成事件
            await PublishAsync(new DataProcessedEvent
            {
                DataId = @event.DataId,
                Result = result,
                ProcessedAt = DateTime.UtcNow,
                ProcessorId = this.GetGrainId()
            });
            
            Logger.LogInformation("Successfully processed data: {DataId}", @event.DataId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process data: {DataId}", @event.DataId);
            throw;
        }
    }
    
    [AllEventHandler(allowSelfHandling: false)]
    public Task LogAllEventsAsync(EventWrapperBase eventWrapper)
    {
        var eventType = eventWrapper.GetType();
        Logger.LogDebug("Event received: {EventType}", eventType.Name);
        return Task.CompletedTask;
    }
    
    // 重写以进行自定义状态转换
    protected override void GAgentTransitionState(DataProcessorState state, StateLogEventBase<DataProcessorStateLogEvent> @event)
    {
        switch (@event)
        {
            case DataProcessedLogEvent e:
                state.ProcessedDataIds.Add(e.DataId);
                state.Results[e.DataId] = e.Result;
                state.TotalProcessed++;
                state.LastProcessedAt = e.ProcessedAt;
                break;
                
            // 在此处理其他状态变更事件
        }
    }
    
    private async Task<string> ProcessDataAsync(string data)
    {
        // 模拟异步处理
        await Task.Delay(100);
        return $"Processed: {data}";
    }
    
    public Task<DataProcessorStatistics> GetStatisticsAsync()
    {
        return Task.FromResult(new DataProcessorStatistics
        {
            TotalProcessed = State.TotalProcessed,
            LastProcessedAt = State.LastProcessedAt,
            ProcessedDataIds = State.ProcessedDataIds.ToList()
        });
    }
}
```

### 文件：src/Events/ProcessDataEvent.cs
```csharp
[GenerateSerializer]
public class ProcessDataEvent : EventBase
{
    [Id(0)] public string DataId { get; set; } = string.Empty;
    [Id(1)] public string Data { get; set; } = string.Empty;
    [Id(2)] public string DataType { get; set; } = string.Empty;
}
```

### 文件：src/Events/DataProcessedEvent.cs
```csharp
[GenerateSerializer]
public class DataProcessedEvent : EventBase
{
    [Id(0)] public string DataId { get; set; } = string.Empty;
    [Id(1)] public string Result { get; set; } = string.Empty;
    [Id(2)] public DateTime ProcessedAt { get; set; }
    [Id(3)] public GrainId ProcessorId { get; set; }
}
```

## 🔍 要点总结

1. **接口优先**：实现前始终先定义接口（IStateGAgent<TState> 或 IStateGAgent<TState> + IAIGAgent）
2. **Logger**：始终使用内置的 `Logger` 属性，不要注入
3. **GAgent Factory**：使用 `IGAgentFactory`，而不是 `IGrainFactory`
4. **[GAgent] 属性**：必需，但参数是可选的
5. **GetDescriptionAsync()**：必需的详细描述方法
6. **状态管理**：始终使用 RaiseEvent() → ConfirmEvents() → GAgentTransitionState
7. **集合**：始终在状态类中初始化集合
8. **事件处理器**：三种类型 - [EventHandler]、[AllEventHandler] 或 "HandleEventAsync" 约定
9. **服务**：使用简化的懒加载属性（不需要支持字段）
10. **事件通信**：GAgent 必须在同一组中（父子关系）
11. **序列化**：始终添加 `[GenerateSerializer]` 和 `[Id(n)]` 属性

## 📂 文件组织

```
src/
├── Events/                    # 共享事件（EventBase 派生）
│   ├── NotificationEvent.cs
│   ├── DataProcessingEvent.cs
│   ├── CoordinationEvent.cs
│   └── CommonEvents.cs
└── GAgents/                   # GAgent 实现（单文件包含所有相关代码）
    ├── DataProcessorGAgent.cs # 包含：接口、状态、状态日志事件、实现
    ├── NotificationGAgent.cs
    ├── ProcessingGAgent.cs
    ├── CoordinatorGAgent.cs
    └── EventLoggerGAgent.cs
```

### 最佳实践：每个 GAgent 一个文件
将所有 GAgent 相关代码保存在单个文件中，以提高可读性和可维护性：

1. **接口定义**（例如：`IDataProcessorGAgent`）
2. **状态类**（例如：`DataProcessorState`）
3. **状态日志事件**（作为 GAgent 的嵌套类）
4. **GAgent 实现**（例如：`DataProcessorGAgent`）

这种方法使得理解 GAgent 的完整上下文更容易，无需在多个文件间跳转。 