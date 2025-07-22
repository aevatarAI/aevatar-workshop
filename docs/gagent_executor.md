# GAgentExecutor Documentation

## Overview / 概述

GAgentExecutor is a core component in the Aevatar Workshop framework that facilitates the execution of GAgent event handlers. It provides a streamlined way to invoke event processing on GAgents while handling the complexities of distributed execution, result collection, and timeout management.

GAgentExecutor 是 Aevatar Workshop 框架中的核心组件，用于执行 GAgent 事件处理器。它提供了一种简化的方式来调用 GAgent 上的事件处理，同时处理分布式执行、结果收集和超时管理的复杂性。

## Key Features / 核心功能

- **Event Handler Execution**: Execute event handlers on specific GAgent instances
- **Result Collection**: Automatically collect and return execution results through streams
- **Timeout Management**: Built-in 5-minute timeout to prevent hanging operations
- **Flexible Targeting**: Support execution by GrainId or GrainType
- **Stream-based Communication**: Leverages Orleans streams for reliable result delivery

- **事件处理器执行**：在特定的 GAgent 实例上执行事件处理器
- **结果收集**：通过流自动收集并返回执行结果
- **超时管理**：内置 5 分钟超时机制，防止操作挂起
- **灵活的目标定位**：支持通过 GrainId 或 GrainType 执行
- **基于流的通信**：利用 Orleans 流实现可靠的结果传递

## Architecture / 架构

```mermaid
graph TD
    A[Client] -->|Execute Event| B[GAgentExecutor]
    B -->|Get GAgent| C[IGAgentFactory]
    B -->|Create Stream| D[Orleans Stream]
    B -->|Publish Event| E[PublishingGAgent]
    E -->|Forward Event| F[Target GAgent]
    E -->|Forward to| G[ResultGAgent]
    G -->|Collect Result| H[Result Stream]
    H -->|Return Result| B
    B -->|Return| A
```

## How It Works / 工作原理

1. **Initialization**: GAgentExecutor creates a unique execution ID and sets up a result stream
2. **GAgent Resolution**: Uses IGAgentFactory to get the target GAgent, ResultGAgent, and PublishingGAgent
3. **Stream Setup**: Creates a subscription to monitor for execution completion
4. **Event Publishing**: PublishingGAgent forwards the event to both the target GAgent and ResultGAgent
5. **Result Collection**: ResultGAgent processes the execution result and publishes it to the stream
6. **Result Type Filtering**: If expectedResultType is specified, ResultGAgent only collects events of that specific type
7. **Timeout Handling**: If no result is received within 5 minutes, a TimeoutException is thrown

1. **初始化**：GAgentExecutor 创建唯一的执行 ID 并设置结果流
2. **GAgent 解析**：使用 IGAgentFactory 获取目标 GAgent、ResultGAgent 和 PublishingGAgent
3. **流设置**：创建订阅以监控执行完成
4. **事件发布**：PublishingGAgent 将事件转发给目标 GAgent 和 ResultGAgent
5. **结果收集**：ResultGAgent 处理执行结果并将其发布到流
6. **结果类型过滤**：如果指定了 expectedResultType，ResultGAgent 仅收集该特定类型的事件
7. **超时处理**：如果在 5 分钟内未收到结果，将抛出 TimeoutException

## API Reference / API 参考

### IGAgentExecutor Interface

```csharp
public interface IGAgentExecutor
{
    // Standard methods with EventBase parameter
    Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, EventBase @event, Type? expectedResultType = null);
    Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event, Type? expectedResultType = null);
    Task<string> ExecuteGAgentEventHandler(GrainType grainType, EventBase @event, Type? expectedResultType = null);
    
    // Enhanced methods with event type name and JSON parameters
    Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, string eventTypeName, string eventJson, Type? expectedResultType = null);
    Task<string> ExecuteGAgentEventHandler(GrainId grainId, string eventTypeName, string eventJson, Type? expectedResultType = null);
    Task<string> ExecuteGAgentEventHandler(GrainType grainType, string eventTypeName, string eventJson, Type? expectedResultType = null);
}
```

### Methods / 方法

#### ExecuteGAgentEventHandler(IGAgent, EventBase, Type?)
Executes an event handler on a specific GAgent instance.

在特定的 GAgent 实例上执行事件处理器。

**Parameters / 参数:**
- `gAgent`: The target GAgent instance / 目标 GAgent 实例
- `@event`: The event to be processed / 要处理的事件
- `expectedResultType`: Optional expected result event type to wait for / 可选，等待的预期结果事件类型

**Returns / 返回:** 
- `Task<string>`: The execution result as a string / 以字符串形式返回的执行结果

#### ExecuteGAgentEventHandler(GrainId, EventBase, Type?)
Executes an event handler on a specific GAgent instance identified by GrainId.

在由 GrainId 标识的特定 GAgent 实例上执行事件处理器。

**Parameters / 参数:**
- `grainId`: The unique identifier of the target GAgent / 目标 GAgent 的唯一标识符
- `@event`: The event to be processed / 要处理的事件
- `expectedResultType`: Optional expected result event type to wait for / 可选，等待的预期结果事件类型

**Returns / 返回:** 
- `Task<string>`: The execution result as a string / 以字符串形式返回的执行结果

#### ExecuteGAgentEventHandler(GrainType, EventBase, Type?)
Executes an event handler on a new GAgent instance of the specified type.

在指定类型的新 GAgent 实例上执行事件处理器。

**Parameters / 参数:**
- `grainType`: The type of GAgent to create and execute on / 要创建和执行的 GAgent 类型
- `@event`: The event to be processed / 要处理的事件
- `expectedResultType`: Optional expected result event type to wait for / 可选，等待的预期结果事件类型

**Returns / 返回:**
- `Task<string>`: The execution result as a string / 以字符串形式返回的执行结果

#### ExecuteGAgentEventHandler(IGAgent, string, string, Type?)
Executes an event handler on a specific GAgent instance using event type name and JSON.

使用事件类型名称和JSON在特定的 GAgent 实例上执行事件处理器。

**Parameters / 参数:**
- `gAgent`: The target GAgent instance / 目标 GAgent 实例
- `eventTypeName`: The name of the event type to execute / 要执行的事件类型名称
- `eventJson`: JSON string containing the event data / 包含事件数据的JSON字符串
- `expectedResultType`: Optional expected result event type to wait for / 可选，等待的预期结果事件类型

**Returns / 返回:** 
- `Task<string>`: The execution result as a string / 以字符串形式返回的执行结果

#### ExecuteGAgentEventHandler(GrainId, string, string, Type?)
Executes an event handler on a specific GAgent instance identified by GrainId using event type name and JSON.

使用事件类型名称和JSON在由 GrainId 标识的特定 GAgent 实例上执行事件处理器。

**Parameters / 参数:**
- `grainId`: The unique identifier of the target GAgent / 目标 GAgent 的唯一标识符
- `eventTypeName`: The name of the event type to execute / 要执行的事件类型名称
- `eventJson`: JSON string containing the event data / 包含事件数据的JSON字符串
- `expectedResultType`: Optional expected result event type to wait for / 可选，等待的预期结果事件类型

**Returns / 返回:** 
- `Task<string>`: The execution result as a string / 以字符串形式返回的执行结果

#### ExecuteGAgentEventHandler(GrainType, string, string, Type?)
Executes an event handler on a new GAgent instance of the specified type using event type name and JSON.

使用事件类型名称和JSON在指定类型的新 GAgent 实例上执行事件处理器。

**Parameters / 参数:**
- `grainType`: The type of GAgent to create and execute on / 要创建和执行的 GAgent 类型
- `eventTypeName`: The name of the event type to execute / 要执行的事件类型名称
- `eventJson`: JSON string containing the event data / 包含事件数据的JSON字符串
- `expectedResultType`: Optional expected result event type to wait for / 可选，等待的预期结果事件类型

**Returns / 返回:**
- `Task<string>`: The execution result as a string / 以字符串形式返回的执行结果

## Usage Examples / 使用示例

### Basic Usage / 基础用法

```csharp
// Inject dependencies / 注入依赖
public class MyService
{
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly IGAgentFactory _gAgentFactory;

    public MyService(IGAgentExecutor gAgentExecutor, IGAgentFactory gAgentFactory)
    {
        _gAgentExecutor = gAgentExecutor;
        _gAgentFactory = gAgentFactory;
    }

    // Execute by GrainType / 通过 GrainType 执行
    public async Task ExecuteByTypeExample()
    {
        var grainType = GrainType.Create("EventHandlerDemoGAgent");
        var greetingEvent = new GreetingEvent 
        { 
            Greeting = "Hello, Aevatar!" 
        };
        
        var result = await _gAgentExecutor.ExecuteGAgentEventHandler(grainType, greetingEvent);
        Console.WriteLine($"Execution result: {result}");
    }

    // Execute by GrainId / 通过 GrainId 执行
    public async Task ExecuteByIdExample()
    {
        var grainId = GrainId.Create(
            GrainType.Create("EventHandlerDemoGAgent"), 
            Guid.NewGuid().ToString()
        );
        
        var greetingEvent = new GreetingEvent 
        { 
            Greeting = "Hello from specific instance!" 
        };
        
        var result = await _gAgentExecutor.ExecuteGAgentEventHandler(grainId, greetingEvent);
        Console.WriteLine($"Execution result: {result}");
    }

    // Execute with expected result type / 执行并指定预期结果类型
    public async Task ExecuteWithExpectedResultTypeExample()
    {
        var grainType = GrainType.Create("EventHandlerDemoGAgent");
        var requestEvent = new RequestEvent 
        { 
            RequestId = Guid.NewGuid().ToString(),
            Data = "Process this data"
        };
        
        // Wait specifically for ResponseEvent type
        var result = await _gAgentExecutor.ExecuteGAgentEventHandler(
            grainType, 
            requestEvent, 
            typeof(ResponseEvent)
        );
        Console.WriteLine($"Response received: {result}");
    }

    // Execute with IGAgent instance / 使用 IGAgent 实例执行
    public async Task ExecuteWithIGAgentExample()
    {
        var gAgent = await _gAgentFactory.GetGAgentAsync<IEventHandlerDemoGAgent>();
        var greetingEvent = new GreetingEvent 
        { 
            Greeting = "Hello from IGAgent instance!" 
        };
        
        var result = await _gAgentExecutor.ExecuteGAgentEventHandler(gAgent, greetingEvent);
        Console.WriteLine($"Execution result: {result}");
    }

    // Execute with event type name and JSON / 使用事件类型名称和JSON执行
    public async Task ExecuteWithEventTypeNameAndJsonExample()
    {
        var grainType = GrainType.Create("EventHandlerDemoGAgent");
        var eventTypeName = "GreetingEvent";
        var eventJson = """
        {
            "Greeting": "Hello from JSON!",
            "Timestamp": "2024-01-01T00:00:00Z"
        }
        """;
        
        var result = await _gAgentExecutor.ExecuteGAgentEventHandler(
            grainType, 
            eventTypeName, 
            eventJson
        );
        Console.WriteLine($"Execution result: {result}");
    }

    // Execute with event type name, JSON and expected result type / 使用事件类型名称、JSON和预期结果类型执行
    public async Task ExecuteWithEventTypeNameJsonAndExpectedResultExample()
    {
        var grainId = GrainId.Create(
            GrainType.Create("EventHandlerDemoGAgent"), 
            Guid.NewGuid().ToString()
        );
        var eventTypeName = "RequestEvent";
        var eventJson = """
        {
            "RequestId": "12345",
            "Data": "Process this data",
            "Priority": "High"
        }
        """;
        
        // Wait specifically for ResponseEvent type
        var result = await _gAgentExecutor.ExecuteGAgentEventHandler(
            grainId, 
            eventTypeName, 
            eventJson, 
            typeof(ResponseEvent)
        );
        Console.WriteLine($"Response received: {result}");
    }
}
```

### Unit Testing Example / 单元测试示例

```csharp
[Fact]
public async Task ExecuteGAgentEventHandler_ShouldProcessEvent()
{
    // Arrange
    var targetGAgent = await _gAgentFactory.GetGAgentAsync<IStateGAgent<EventHandlerDemoGAgentState>>();
    var grainId = targetGAgent.GetGrainId();
    
    var greetingEvent = new GreetingEvent
    {
        Greeting = "Test greeting!"
    };

    // Act
    var result = await _gAgentExecutor.ExecuteGAgentEventHandler(grainId, greetingEvent);

    // Assert
    result.ShouldNotBeNull();
    result.ShouldContain(greetingEvent.Greeting);
    
    // Verify state was updated
    var state = await targetGAgent.GetStateAsync();
    state.Content.ShouldContain(greetingEvent.Greeting);
}

[Fact]
public async Task ExecuteGAgentEventHandler_WithExpectedResultType_ShouldWaitForSpecificEvent()
{
    // Arrange
    var targetGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
    var requestEvent = new MockExecutorTestEvent
    {
        Message = "Test with expected result type"
    };

    // Act - Wait specifically for MockExecutorTestResponseEvent
    var result = await _gAgentExecutor.ExecuteGAgentEventHandler(
        targetGAgent, 
        requestEvent, 
        typeof(MockExecutorTestResponseEvent)
    );

    // Assert
    result.ShouldNotBeNull();
    result.ShouldContain("Processed: Test with expected result type");
}

[Fact]
public async Task ExecuteGAgentEventHandler_WithIGAgent_ShouldExecuteSuccessfully()
{
    // Arrange
    var targetGAgent = await _gAgentFactory.GetGAgentAsync<IMockExecutorGAgent>();
    var testEvent = new MockExecutorTestEvent
    {
        Message = "Test with IGAgent instance"
    };

    // Act
    var result = await _gAgentExecutor.ExecuteGAgentEventHandler(targetGAgent, testEvent);

    // Assert
    result.ShouldNotBeNull();
    result.ShouldContain("Processed: Test with IGAgent instance");
}
```

## Events and Result Handling / 事件和结果处理

### Expected Result Type Parameter / 预期结果类型参数

The `expectedResultType` parameter allows you to specify which event type to wait for as the execution result. This is particularly useful when:

`expectedResultType` 参数允许您指定等待哪种事件类型作为执行结果。这在以下情况下特别有用：

- **Selective Result Collection**: Only collect results from specific event types
- **Event Filtering**: Filter out unwanted events and focus on the expected response
- **Async Operations**: Wait for completion events from long-running operations
- **Multi-step Processes**: Wait for specific step completion events

- **选择性结果收集**：仅收集特定事件类型的结果
- **事件过滤**：过滤掉不需要的事件，专注于预期的响应
- **异步操作**：等待长时间运行操作的完成事件
- **多步骤流程**：等待特定步骤完成事件

**Usage Examples / 使用示例:**

```csharp
// Wait for a specific response event type
var result = await _gAgentExecutor.ExecuteGAgentEventHandler(
    grainType, 
    requestEvent, 
    typeof(ResponseEvent)
);

// Execute without waiting for specific result type (collects any event)
var result = await _gAgentExecutor.ExecuteGAgentEventHandler(
    grainType, 
    requestEvent
);
```

### ExecutionCompletedEvent

The GAgentExecutor uses `ExecutionCompletedEvent` internally to communicate execution results:

GAgentExecutor 内部使用 `ExecutionCompletedEvent` 来传递执行结果：

```csharp
[GenerateSerializer]
public class ExecutionCompletedEvent
{
    [Id(0)] public string ExecutionId { get; set; } = string.Empty;
    [Id(1)] public string Result { get; set; } = string.Empty;
}
```

## Advanced Features / 高级功能

### Expected Result Type Filtering / 预期结果类型过滤

The `expectedResultType` parameter enhances the GAgentExecutor by providing selective result collection. When specified, the ResultGAgent will only collect and return events of the specified type, ignoring all other events published during execution.

`expectedResultType` 参数通过提供选择性结果收集来增强 GAgentExecutor。当指定时，ResultGAgent 将仅收集并返回指定类型的事件，忽略执行期间发布的所有其他事件。

**Benefits / 优势:**
- **Precise Control**: Wait for specific completion events
- **Noise Reduction**: Filter out intermediate or debug events
- **Better Performance**: Avoid collecting unnecessary events
- **Clearer Contracts**: Define explicit result expectations

- **精确控制**：等待特定的完成事件
- **减少噪音**：过滤掉中间或调试事件
- **更好的性能**：避免收集不必要的事件
- **更清晰的契约**：定义明确的结果期望

### Dynamic Event Execution / 动态事件执行

The enhanced methods with `eventTypeName` and `eventJson` parameters enable dynamic event execution without requiring compile-time knowledge of event types. This is particularly useful for:

使用 `eventTypeName` 和 `eventJson` 参数的增强方法支持动态事件执行，无需在编译时了解事件类型。这在以下情况下特别有用：

- **API Integration**: Execute events from external APIs or web services
- **Dynamic Workflows**: Build event-driven workflows at runtime
- **Plugin Systems**: Allow plugins to execute events on GAgents
- **Serialization Flexibility**: Work with JSON data from various sources

- **API集成**：从外部API或Web服务执行事件
- **动态工作流**：在运行时构建事件驱动的工作流
- **插件系统**：允许插件在GAgent上执行事件
- **序列化灵活性**：处理来自各种来源的JSON数据

**Features / 特性:**
- **Type Discovery**: Automatically discovers event types from GAgent metadata
- **JSON Deserialization**: Converts JSON strings to strongly-typed events
- **Error Handling**: Provides detailed error messages for type resolution failures
- **Logging**: Comprehensive logging for debugging and monitoring

- **类型发现**：从GAgent元数据自动发现事件类型
- **JSON反序列化**：将JSON字符串转换为强类型事件
- **错误处理**：为类型解析失败提供详细的错误消息
- **日志记录**：用于调试和监控的综合日志记录

## Configuration / 配置

### Dependency Injection / 依赖注入

Register GAgentExecutor in your service configuration:

在服务配置中注册 GAgentExecutor：

```csharp
// Register required services
services.AddTransient<IGAgentService, GAgentService>();
services.AddTransient<IGAgentExecutor, GAgentExecutor>();

// Or if using the enhanced constructor with logging
services.AddTransient<IGAgentExecutor>(provider => 
    new GAgentExecutor(
        provider.GetRequiredService<IClusterClient>(),
        provider.GetRequiredService<IGAgentService>(),
        provider.GetRequiredService<ILogger<GAgentExecutor>>()
    ));
```

### Stream Provider / 流提供程序

GAgentExecutor uses the default Aevatar stream provider:

GAgentExecutor 使用默认的 Aevatar 流提供程序：

- Provider Name: `AevatarCoreConstants.StreamProvider`
- Namespace: `GAgentPluginConstants.GAgentPluginStreamNamespace`

## Best Practices / 最佳实践

1. **Error Handling**: Always wrap executor calls in try-catch blocks to handle TimeoutException
   **错误处理**：始终将执行器调用包装在 try-catch 块中以处理 TimeoutException

2. **Timeout Consideration**: The default 5-minute timeout is suitable for most operations, but consider your specific use case
   **超时考虑**：默认的 5 分钟超时适用于大多数操作，但请考虑您的具体用例

3. **Event Design**: Keep events small and focused on a single responsibility
   **事件设计**：保持事件小巧且专注于单一职责

4. **State Verification**: After execution, verify the GAgent state if needed
   **状态验证**：执行后，如需要可验证 GAgent 状态

5. **Reusability**: Reuse GAgentExecutor instances; they are thread-safe
   **可重用性**：重用 GAgentExecutor 实例；它们是线程安全的

## Troubleshooting / 故障排除

### Common Issues / 常见问题

1. **TimeoutException**
   - Cause: Event handler takes longer than 5 minutes
   - Solution: Optimize event handler logic or consider async processing
   - 原因：事件处理器执行超过 5 分钟
   - 解决方案：优化事件处理器逻辑或考虑异步处理

2. **Service Resolution Errors**
   - Cause: IGAgentFactory not registered in DI container
   - Solution: Ensure proper service registration in startup
   - 原因：IGAgentFactory 未在 DI 容器中注册
   - 解决方案：确保在启动时正确注册服务

3. **Stream Connection Issues**
   - Cause: Stream provider not configured correctly
   - Solution: Verify Orleans stream configuration
   - 原因：流提供程序配置不正确
   - 解决方案：验证 Orleans 流配置

## Integration with Plugin System / 与插件系统的集成

GAgentExecutor is a key component in the GAgent plugin system, enabling dynamic execution of event handlers loaded from plugins. When used with the plugin system:

GAgentExecutor 是 GAgent 插件系统中的关键组件，支持从插件加载的事件处理器的动态执行。与插件系统一起使用时：

1. Plugins can define custom GAgents with event handlers
2. GAgentExecutor can execute these handlers without compile-time knowledge
3. Results are collected and returned in a standardized format

1. 插件可以定义带有事件处理器的自定义 GAgent
2. GAgentExecutor 可以在没有编译时知识的情况下执行这些处理器
3. 结果以标准化格式收集和返回

## Performance Considerations / 性能考虑

- **Stream Overhead**: Each execution creates a new stream subscription, which has minimal overhead
- **Timeout Trade-off**: The 5-minute timeout provides safety but may be too long for high-frequency operations
- **Concurrent Executions**: Multiple executions can run concurrently without interference

- **流开销**：每次执行都会创建新的流订阅，开销很小
- **超时权衡**：5 分钟超时提供了安全性，但对于高频操作可能太长
- **并发执行**：多个执行可以并发运行而不会相互干扰

## Future Enhancements / 未来增强

Potential improvements being considered:

正在考虑的潜在改进：

- Configurable timeout values / 可配置的超时值
- Batch execution support / 批量执行支持
- Execution metrics and monitoring / 执行指标和监控
- Custom result serialization / 自定义结果序列化
- Retry mechanisms / 重试机制

## Related Components / 相关组件

- **IGAgentFactory**: Creates and manages GAgent instances / 创建和管理 GAgent 实例
- **IPublishingGAgent**: Handles event distribution / 处理事件分发
- **IResultGAgent**: Collects and reports execution results / 收集和报告执行结果
- **EventBase**: Base class for all events / 所有事件的基类

---

*This documentation reflects the current implementation of GAgentExecutor in the Aevatar Workshop framework. For the latest updates, refer to the source code and unit tests.*

*本文档反映了 Aevatar Workshop 框架中 GAgentExecutor 的当前实现。有关最新更新，请参考源代码和单元测试。*