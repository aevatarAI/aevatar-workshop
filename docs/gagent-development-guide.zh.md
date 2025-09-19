# GAgent 开发指南

中文版 | [🇺🇸 English](gagent-development-guide.md)

基于 Aevatar 框架构建智能代理的完整指南。

---

## 📚 目录

1. [快速入门](#-快速入门)
2. [第一个 GAgent](#-第一个-gagent)
3. [状态管理](#-状态管理)
4. [事件系统](#-事件系统)
5. [AI 集成](#-ai-集成)
6. [高级主题](#-高级主题)
7. [最佳实践](#-最佳实践)
8. [完整示例](#-完整示例)

---

## 🚀 快速入门

### 什么是 GAgent？

**GAgent**（Grain Agent，粒度代理）是 Aevatar 框架中的智能自主实体，具备以下能力：

- 🧠 **思考与记忆**：在交互过程中保持持久化状态
- 💬 **通信协作**：与其他 GAgent 发送和接收事件
- ⚡ **自动响应**：自动处理事件和状态变化
- 🤖 **AI 驱动**：集成大语言模型进行智能决策

### 核心概念

- **事件驱动**：GAgent 通过强类型事件进行通信
- **事件溯源**：所有状态变化都通过事件记录，确保完整的审计跟踪
- **基于 Orleans**：构建在 Microsoft Orleans 之上，支持大规模扩展
- **类型安全**：完整的 C# 类型安全和编译时检查

---

## 🏗️ 第一个 GAgent

让我们一步步构建一个简单的计数器 GAgent。

### 步骤 1：定义接口

```csharp
public interface ICounterGAgent : IStateGAgent<CounterState>
{
    Task IncrementAsync(int amount = 1);
    Task DecrementAsync(int amount = 1);
    Task<int> GetCurrentValueAsync();
}
```

### 步骤 2：定义状态

```csharp
[GenerateSerializer]
public class CounterState : StateBase
{
    [Id(0)] public int Value { get; set; }
    [Id(1)] public DateTime LastUpdated { get; set; }
    [Id(2)] public List<string> History { get; set; } = new();
}
```

### 步骤 3：定义状态日志事件

```csharp
[GenerateSerializer]
public abstract class CounterStateLogEvent : StateLogEventBase<CounterStateLogEvent>;

[GenerateSerializer]
public class CounterIncrementedEvent : CounterStateLogEvent
{
    [Id(0)] public int Amount { get; init; }
    [Id(1)] public DateTime Timestamp { get; init; }
}

[GenerateSerializer]
public class CounterDecrementedEvent : CounterStateLogEvent
{
    [Id(0)] public int Amount { get; init; }
    [Id(1)] public DateTime Timestamp { get; init; }
}
```

### 步骤 4：实现 GAgent

```csharp
[GAgent("counter", "workshop")]
public class CounterGAgent : GAgentBase<CounterState, CounterStateLogEvent>, ICounterGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("一个简单的计数器，可以增加和减少数值");

    public async Task IncrementAsync(int amount = 1)
    {
        // 触发事件来改变状态
        RaiseEvent(new CounterIncrementedEvent 
        { 
            Amount = amount,
            Timestamp = DateTime.UtcNow
        });
        
        // 确认事件以应用状态变化
        await ConfirmEvents();
        
        Logger.LogInformation("计数器增加了 {Amount}", amount);
    }

    public async Task DecrementAsync(int amount = 1)
    {
        RaiseEvent(new CounterDecrementedEvent 
        { 
            Amount = amount,
            Timestamp = DateTime.UtcNow
        });
        
        await ConfirmEvents();
        
        Logger.LogInformation("计数器减少了 {Amount}", amount);
    }

    public Task<int> GetCurrentValueAsync()
        => Task.FromResult(State.Value);

    // 处理状态转换
    protected override void GAgentTransitionState(CounterState state, StateLogEventBase<CounterStateLogEvent> @event)
    {
        switch (@event)
        {
            case CounterIncrementedEvent e:
                state.Value += e.Amount;
                state.LastUpdated = e.Timestamp;
                state.History.Add($"在 {e.Timestamp} 增加了 {e.Amount}");
                break;
                
            case CounterDecrementedEvent e:
                state.Value -= e.Amount;
                state.LastUpdated = e.Timestamp;
                state.History.Add($"在 {e.Timestamp} 减少了 {e.Amount}");
                break;
        }
    }
}
```

### 步骤 5：使用你的 GAgent

```csharp
// 通过工厂获取 GAgent
var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();
var counter = await gAgentFactory.GetGAgentAsync<ICounterGAgent>(Guid.NewGuid());

// 使用它
await counter.IncrementAsync(5);
await counter.DecrementAsync(2);
var currentValue = await counter.GetCurrentValueAsync(); // 返回 3
```

---

## 🗂️ 状态管理

### 事件溯源模式

GAgent 不直接修改状态，而是使用 **事件溯源**：

1. **触发事件**：创建一个描述发生了什么的事件
2. **确认事件**：将所有待处理的事件应用到状态
3. **状态转换**：在 `GAgentTransitionState` 中处理事件

### 为什么使用事件溯源？

✅ **完整的审计跟踪**：记录每一个状态变化  
✅ **时间旅行**：重放事件以重现任何历史状态  
✅ **一致性**：在分布式系统中原子化状态更新  
✅ **调试友好**：完全可见什么时候改变了什么，为什么改变  

### 使用配置进行状态初始化

对于需要初始化参数的 GAgent：

```csharp
// 步骤 1：定义配置类
[GenerateSerializer]
public class CounterConfiguration : ConfigurationBase
{
    [Id(0)] public int InitialValue { get; set; }
    [Id(1)] public string Name { get; set; } = string.Empty;
}

// 步骤 2：定义初始化事件
[GenerateSerializer]
public class CounterInitializedEvent : CounterStateLogEvent
{
    [Id(0)] public int InitialValue { get; init; }
    [Id(1)] public string Name { get; init; }
}

// 步骤 3：使用四参数 GAgentBase
public class CounterGAgent : GAgentBase<CounterState, CounterStateLogEvent, EventBase, CounterConfiguration>, ICounterGAgent
{
    protected override async Task PerformConfigAsync(CounterConfiguration configuration)
    {
        // 通过事件初始化状态
        RaiseEvent(new CounterInitializedEvent
        {
            InitialValue = configuration.InitialValue,
            Name = configuration.Name
        });
        
        await ConfirmEvents();
    }
    
    // 在状态转换中处理
    protected override void GAgentTransitionState(CounterState state, StateLogEventBase<CounterStateLogEvent> @event)
    {
        switch (@event)
        {
            case CounterInitializedEvent e:
                state.Value = e.InitialValue;
                state.Id = this.GetGrainId().Key.ToString() ?? "default";
                state.History = new List<string> { $"初始化为 '{e.Name}'，初始值 {e.InitialValue}" };
                break;
            // ... 其他事件
        }
    }
}

// 使用配置
var config = new CounterConfiguration 
{ 
    InitialValue = 100, 
    Name = "我的计数器" 
};
var counter = await gAgentFactory.GetGAgentAsync<ICounterGAgent>(Guid.NewGuid(), config);
```

### 常见状态管理模式

#### ✅ 正确做法
```csharp
// 正确：使用事件修改状态
RaiseEvent(new ValueChangedEvent { NewValue = 42 });
await ConfirmEvents();

// 始终在状态中初始化集合
[GenerateSerializer]
public class MyState : StateBase
{
    [Id(0)] public List<string> Items { get; set; } = new();
    [Id(1)] public Dictionary<string, int> Counters { get; set; } = new();
}
```

#### ❌ 错误做法
```csharp
// 错误：直接状态修改
State.Value = 42; // 这个改变会丢失！

// 错误：未初始化的集合
public List<string> Items { get; set; } // 会是 null！
```

---

## 📡 事件系统

事件是 GAgent 之间的主要通信机制。

### 事件类型

#### 1. 业务事件（用于通信）
```csharp
[GenerateSerializer]
public class OrderCreatedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public decimal Amount { get; init; }
    [Id(2)] public string CustomerId { get; init; } = string.Empty;
}
```

#### 2. 状态日志事件（用于状态变化）
```csharp
[GenerateSerializer]
public class OrderStateChangedEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public OrderStatus NewStatus { get; init; }
}
```

### 事件处理器

#### 基于特性的处理器
```csharp
[EventHandler]
public async Task HandleOrderCreatedAsync(OrderCreatedEvent @event)
{
    Logger.LogInformation("处理订单 {OrderId}", @event.OrderId);
    
    // 更新状态
    RaiseEvent(new OrderProcessingStartedEvent { OrderId = @event.OrderId });
    await ConfirmEvents();
    
    // 通知其他代理
    await PublishAsync(new OrderProcessingNotificationEvent 
    { 
        OrderId = @event.OrderId,
        Status = "开始处理"
    });
}
```

#### 约定式处理器
```csharp
// 方法名为 "HandleEventAsync" 会自动识别
public async Task HandleEventAsync(OrderCreatedEvent @event)
{
    // 处理事件
}
```

#### 全事件处理器
```csharp
[AllEventHandler(allowSelfHandling: false)]
public Task HandleAllEventsAsync(EventWrapperBase eventWrapper)
{
    Logger.LogDebug("收到事件：{EventType}", eventWrapper.Event.GetType().Name);
    return Task.CompletedTask;
}
```

### 事件通信设置

GAgent 必须在同一个通信组中才能交换事件：

```csharp
// 创建协调器 GAgent
var coordinator = await gAgentFactory.GetGAgentAsync<ICoordinatorGAgent>();

// 将其他 GAgent 注册到协调器
await coordinator.RegisterAsync(orderProcessor);
await coordinator.RegisterAsync(inventoryManager);
await coordinator.RegisterAsync(notificationService);

// 现在它们可以通过事件通信
await orderProcessor.PublishAsync(new OrderCreatedEvent { /* ... */ });
```

### 事件发布模式

#### 广播给所有
```csharp
await PublishAsync(new SystemMaintenanceEvent { Message = "系统即将维护" });
```

#### 定向发布
```csharp
await PublishAsync(targetGrainId, new PersonalNotificationEvent { Message = "你好！" });
```

#### 事件订阅
```csharp
// 订阅另一个 GAgent 的事件
await SubscribeToAsync(otherGAgent);

// 不再需要时取消订阅
await UnsubscribeFromAsync(otherGAgent);
```

---

## 🤖 AI 集成

将你的 GAgent 转换为具有 AI 能力的智能代理。

### 创建 AI 增强的 GAgent

#### 步骤 1：定义 AI 接口
```csharp
public interface IOrderAIGAgent : IStateGAgent<OrderAIState>, IAIGAgent
{
    Task<string> ProcessCustomerRequestAsync(string request);
    Task<bool> ValidateOrderAsync(string orderDetails);
}
```

#### 步骤 2：继承自 AIGAgentBase
```csharp
[GAgent("order-ai", "workshop")]
public class OrderAIGAgent : AIGAgentBase<OrderAIState, OrderAIStateLogEvent>, IOrderAIGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("AI 驱动的订单处理代理");

    // 使用 AIGAgentTransitionState 而不是 GAgentTransitionState
    protected override void AIGAgentTransitionState(OrderAIState state, StateLogEventBase<OrderAIStateLogEvent> @event)
    {
        switch (@event)
        {
            case OrderProcessedEvent e:
                state.ProcessedOrders.Add(e.OrderId);
                state.LastProcessedAt = e.Timestamp;
                break;
        }
    }
}
```

### AI 初始化

```csharp
// 使用 AI 能力进行初始化
var initDto = new InitializeDto
{
    LLMConfig = new LLMConfigDto
    {
        SystemLLM = "gpt-4o"  // 引用系统级配置
    },
    Instructions = "你是一个订单处理专家。帮助客户处理他们的订单。",
    EnableGAgentTools = true,  // 允许 AI 调用其他 GAgent
    EnableMCPTools = true,     // 启用模型上下文协议工具
    SelectedGAgents = new List<string> { "inventory-manager", "payment-processor" }
};

var aiAgent = await gAgentFactory.GetGAgentAsync<IOrderAIGAgent>(Guid.NewGuid());
await aiAgent.InitializeAsync(initDto);
```

### 工具注册

#### GAgent 工具
你的 AI 代理可以自动调用其他 GAgent 作为工具：

```csharp
var initDto = new InitializeDto
{
    EnableGAgentTools = true,
    SelectedGAgents = new List<string> 
    { 
        "inventory-manager",    // AI 可以检查库存
        "payment-processor",    // AI 可以处理支付
        "notification-service"  // AI 可以发送通知
    }
};
```

#### MCP（模型上下文协议）工具
集成外部工具和服务：

```csharp
var initDto = new InitializeDto
{
    EnableMCPTools = true,
    MCPServers = new List<MCPServerConfig>
    {
        new MCPServerConfig
        {
            ServerName = "filesystem",
            Command = "npx",
            Arguments = new List<string> { "@modelcontextprotocol/server-filesystem", "/workspace" }
        },
        new MCPServerConfig
        {
            ServerName = "weather",
            Command = "python",
            Arguments = new List<string> { "weather_server.py" }
        }
    }
};
```

### 使用 AI 聊天

```csharp
public async Task<string> ProcessCustomerRequestAsync(string request)
{
    var response = await ChatAsync(new ChatRequestDto
    {
        Prompt = request,
        ChatId = Guid.NewGuid().ToString(),
        // AI 会根据需要自动使用可用工具
    });

    // 工具调用会自动跟踪
    Logger.LogInformation("AI 响应：{Response}，工具调用次数：{ToolCount}", 
        response.Response, response.ToolCalls?.Count ?? 0);

    return response.Response;
}
```

---

## 🔧 高级主题

### 定时器注册

#### 无状态定时器
```csharp
private IGrainTimer? _heartbeatTimer;

protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
{
    // 注册心跳定时器
    _heartbeatTimer = this.RegisterGrainTimer(
        async (token) => await SendHeartbeatAsync(),
        new GrainTimerCreationOptions
        {
            DueTime = TimeSpan.FromSeconds(30),
            Period = TimeSpan.FromSeconds(30),
            Interleave = true  // 允许在定时器执行期间进行其他调用
        }
    );
    
    return base.OnGAgentActivateAsync(cancellationToken);
}

private async Task SendHeartbeatAsync()
{
    await PublishAsync(new HeartbeatEvent 
    { 
        AgentId = this.GetGrainId().ToString(),
        Timestamp = DateTime.UtcNow
    });
}
```

#### 有状态定时器
```csharp
private IGrainTimer? _processTimer;

protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
{
    var timerState = new { BatchSize = 10, MaxRetries = 3 };
    
    _processTimer = this.RegisterGrainTimer(
        timerState,
        async (state, token) => await ProcessBatchAsync(state.BatchSize, state.MaxRetries),
        new GrainTimerCreationOptions
        {
            DueTime = TimeSpan.FromMinutes(1),
            Period = TimeSpan.FromMinutes(5)
        }
    );
    
    return base.OnGAgentActivateAsync(cancellationToken);
}
```

#### 定时器清理
```csharp
public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
{
    _heartbeatTimer?.Dispose();
    _processTimer?.Dispose();
    return base.OnDeactivateAsync(reason, cancellationToken);
}
```

### 服务访问

```csharp
// 服务访问的延迟属性
private IGAgentFactory GAgentFactory => 
    ServiceProvider.GetRequiredService<IGAgentFactory>();

private IMyCustomService CustomService =>
    ServiceProvider.GetRequiredService<IMyCustomService>();

// 使用
public async Task ProcessWithOtherAgentAsync()
{
    var otherAgent = await GAgentFactory.GetGAgentAsync<IOtherGAgent>(targetId);
    await otherAgent.DoSomethingAsync();
}
```

### 重写生命周期方法

```csharp
protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
{
    Logger.LogInformation("GAgent {GrainId} 正在激活", this.GetGrainId());
    // 初始化资源、定时器等
    return base.OnGAgentActivateAsync(cancellationToken);
}

public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
{
    Logger.LogInformation("GAgent {GrainId} 由于 {Reason} 正在停用", 
        this.GetGrainId(), reason);
    // 清理资源
    return base.OnDeactivateAsync(reason, cancellationToken);
}

protected override Task HandleStateChangedAsync()
{
    Logger.LogDebug("状态在 {GrainId} 中发生了变化", this.GetGrainId());
    // 响应状态变化
    return Task.CompletedTask;
}

protected override Task OnRegisterAgentAsync(GrainId agentGuid)
{
    Logger.LogInformation("代理 {AgentId} 已注册到 {GrainId}", 
        agentGuid, this.GetGrainId());
    return Task.CompletedTask;
}
```

---

## ✅ 最佳实践

### 项目结构

为了可维护性，组织你的 GAgent 代码：

```
src/
├── Events/                    # 共享事件（EventBase 派生）
│   ├── OrderEvents.cs
│   ├── NotificationEvents.cs
│   └── SystemEvents.cs
└── GAgents/                   # GAgent 实现
    ├── OrderGAgent.cs         # 一体化：接口、状态、事件、实现
    ├── InventoryGAgent.cs
    ├── PaymentGAgent.cs
    └── NotificationGAgent.cs
```

### 一体化文件模式

将相关代码保持在一起以提高可读性：

```csharp
// OrderGAgent.cs - 所有内容在一个文件中

// 接口
public interface IOrderGAgent : IStateGAgent<OrderState>
{
    Task<string> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderStatus> GetOrderStatusAsync(string orderId);
}

// 状态
[GenerateSerializer]
public class OrderState : StateBase
{
    [Id(0)] public Dictionary<string, Order> Orders { get; set; } = new();
    [Id(1)] public DateTime LastOrderTime { get; set; }
}

// 状态日志事件
[GenerateSerializer]
public abstract class OrderStateLogEvent : StateLogEventBase<OrderStateLogEvent>;

[GenerateSerializer]
public class OrderCreatedLogEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public Order Order { get; init; } = new();
}

// 实现
[GAgent("order", "ecommerce")]
public class OrderGAgent : GAgentBase<OrderState, OrderStateLogEvent>, IOrderGAgent
{
    // 实现代码...
}
```

### 常见陷阱和解决方案

#### ❌ 问题：直接状态修改
```csharp
// 错误
public Task AddItemAsync(string item)
{
    State.Items.Add(item); // 这个改变会丢失！
    return Task.CompletedTask;
}
```

#### ✅ 解决方案：事件驱动的状态变化
```csharp
// 正确
public async Task AddItemAsync(string item)
{
    RaiseEvent(new ItemAddedEvent { Item = item });
    await ConfirmEvents();
}
```

#### ❌ 问题：使用 IGrainFactory
```csharp
// 错误
var grainFactory = ServiceProvider.GetRequiredService<IGrainFactory>();
var grain = grainFactory.GetGrain<IMyGAgent>(id);
```

#### ✅ 解决方案：使用 IGAgentFactory
```csharp
// 正确
var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
var agent = await gAgentFactory.GetGAgentAsync<IMyGAgent>(id);
```

#### ❌ 问题：构造函数依赖
```csharp
// 错误
public MyGAgent(ILogger<MyGAgent> logger, IMyService service)
{
    // 这在 Orleans 中不起作用！
}
```

#### ✅ 解决方案：服务定位器模式
```csharp
// 正确
private IMyService MyService => 
    ServiceProvider.GetRequiredService<IMyService>();
```

### 性能提示

1. **批处理状态变化**：在 `ConfirmEvents()` 之前将多个事件分组
2. **合理的定时器间隔**：不要用频繁的定时器压垮系统
3. **正确的资源清理**：始终在 `OnDeactivateAsync` 中释放定时器和资源
4. **事件大小**：保持事件小而专注
5. **异步/等待**：始终使用正确的异步模式

### 测试你的 GAgent

```csharp
[Test]
public async Task CounterGAgent_Should_Increment_Correctly()
{
    // 准备
    var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();
    var counter = await gAgentFactory.GetGAgentAsync<ICounterGAgent>(Guid.NewGuid());

    // 执行
    await counter.IncrementAsync(5);
    var result = await counter.GetCurrentValueAsync();

    // 断言
    Assert.AreEqual(5, result);
}
```

---

## 📖 完整示例

### 示例 1：电商订单处理系统

这个示例展示了一个完整的多 GAgent 订单处理系统：

```csharp
// === 订单事件 ===
[GenerateSerializer]
public class OrderSubmittedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public List<OrderItem> Items { get; init; } = new();
    [Id(2)] public string CustomerId { get; init; } = string.Empty;
}

[GenerateSerializer]
public class OrderValidatedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public bool IsValid { get; init; }
    [Id(2)] public string ValidationMessage { get; init; } = string.Empty;
}

[GenerateSerializer]
public class PaymentProcessedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public bool IsSuccessful { get; init; }
    [Id(2)] public decimal Amount { get; init; }
}

// === 订单 GAgent ===
public interface IOrderGAgent : IStateGAgent<OrderState>
{
    Task<string> SubmitOrderAsync(SubmitOrderRequest request);
    Task<OrderStatus> GetOrderStatusAsync(string orderId);
}

[GenerateSerializer]
public class OrderState : StateBase
{
    [Id(0)] public Dictionary<string, Order> Orders { get; set; } = new();
    [Id(1)] public Dictionary<string, OrderStatus> OrderStatuses { get; set; } = new();
}

[GenerateSerializer]
public abstract class OrderStateLogEvent : StateLogEventBase<OrderStateLogEvent>;

[GenerateSerializer]
public class OrderCreatedLogEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public Order Order { get; init; } = new();
}

[GenerateSerializer]
public class OrderStatusChangedLogEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public OrderStatus NewStatus { get; init; }
}

[GAgent("order", "ecommerce")]
public class OrderGAgent : GAgentBase<OrderState, OrderStateLogEvent>, IOrderGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("管理客户订单和订单生命周期");

    public async Task<string> SubmitOrderAsync(SubmitOrderRequest request)
    {
        var orderId = Guid.NewGuid().ToString();
        var order = new Order
        {
            Id = orderId,
            CustomerId = request.CustomerId,
            Items = request.Items,
            TotalAmount = request.Items.Sum(i => i.Price * i.Quantity),
            CreatedAt = DateTime.UtcNow
        };

        // 更新状态
        RaiseEvent(new OrderCreatedLogEvent { OrderId = orderId, Order = order });
        RaiseEvent(new OrderStatusChangedLogEvent { OrderId = orderId, NewStatus = OrderStatus.Submitted });
        await ConfirmEvents();

        // 通知其他代理
        await PublishAsync(new OrderSubmittedEvent
        {
            OrderId = orderId,
            Items = request.Items,
            CustomerId = request.CustomerId
        });

        Logger.LogInformation("客户 {CustomerId} 的订单 {OrderId} 已提交", 
            request.CustomerId, orderId);

        return orderId;
    }

    public Task<OrderStatus> GetOrderStatusAsync(string orderId)
    {
        return Task.FromResult(
            State.OrderStatuses.TryGetValue(orderId, out var status) 
                ? status 
                : OrderStatus.NotFound
        );
    }

    [EventHandler]
    public async Task HandleOrderValidatedAsync(OrderValidatedEvent @event)
    {
        var newStatus = @event.IsValid ? OrderStatus.Validated : OrderStatus.ValidationFailed;
        
        RaiseEvent(new OrderStatusChangedLogEvent 
        { 
            OrderId = @event.OrderId, 
            NewStatus = newStatus 
        });
        await ConfirmEvents();

        Logger.LogInformation("订单 {OrderId} 验证结果：{IsValid}", 
            @event.OrderId, @event.IsValid);
    }

    [EventHandler]
    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent @event)
    {
        var newStatus = @event.IsSuccessful ? OrderStatus.PaymentCompleted : OrderStatus.PaymentFailed;
        
        RaiseEvent(new OrderStatusChangedLogEvent 
        { 
            OrderId = @event.OrderId, 
            NewStatus = newStatus 
        });
        await ConfirmEvents();

        Logger.LogInformation("订单 {OrderId} 支付结果：{IsSuccessful}", 
            @event.OrderId, @event.IsSuccessful);
    }

    protected override void GAgentTransitionState(OrderState state, StateLogEventBase<OrderStateLogEvent> @event)
    {
        switch (@event)
        {
            case OrderCreatedLogEvent e:
                state.Orders[e.OrderId] = e.Order;
                break;
                
            case OrderStatusChangedLogEvent e:
                state.OrderStatuses[e.OrderId] = e.NewStatus;
                break;
        }
    }
}

// === 库存 GAgent ===
[GAgent("inventory", "ecommerce")]
public class InventoryGAgent : GAgentBase<InventoryState, InventoryStateLogEvent>, IInventoryGAgent
{
    [EventHandler]
    public async Task HandleOrderSubmittedAsync(OrderSubmittedEvent @event)
    {
        // 验证库存可用性
        bool isValid = await ValidateInventoryAsync(@event.Items);
        
        if (isValid)
        {
            // 预留库存
            await ReserveInventoryAsync(@event.OrderId, @event.Items);
        }

        // 通知订单系统
        await PublishAsync(new OrderValidatedEvent
        {
            OrderId = @event.OrderId,
            IsValid = isValid,
            ValidationMessage = isValid ? "库存充足" : "库存不足"
        });
    }

    // ... 实现细节
}

// === 支付 GAgent ===
[GAgent("payment", "ecommerce")]
public class PaymentGAgent : GAgentBase<PaymentState, PaymentStateLogEvent>, IPaymentGAgent
{
    [EventHandler]
    public async Task HandleOrderValidatedAsync(OrderValidatedEvent @event)
    {
        if (!@event.IsValid) return;

        // 处理支付
        bool paymentSuccessful = await ProcessPaymentAsync(@event.OrderId);

        // 获取订单金额
        var amount = await GetOrderAmountAsync(@event.OrderId);

        // 通知订单系统
        await PublishAsync(new PaymentProcessedEvent
        {
            OrderId = @event.OrderId,
            IsSuccessful = paymentSuccessful,
            Amount = amount
        });
    }

    // ... 实现细节
}

// === 通知 GAgent ===
[GAgent("notification", "ecommerce")]
public class NotificationGAgent : GAgentBase<NotificationState, NotificationStateLogEvent>, INotificationGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Sends notifications to customers via multiple channels");

    [EventHandler]
    public async Task HandleOrderNotificationAsync(OrderNotificationEvent @event)
    {
        Logger.LogInformation("Sending {Type} notification to customer {CustomerId} for order {OrderId}", 
            @event.Type, @event.CustomerId, @event.OrderId);

        // Check customer notification preferences
        var preferences = await GetCustomerPreferencesAsync(@event.CustomerId);
        
        if (!ShouldSendNotification(@event.Type, preferences))
        {
            Logger.LogInformation("Notification skipped due to customer preferences");
            return;
        }

        // Determine notification channel
        var channel = DetermineChannel(@event.Type, preferences);
        
        // Create notification record
        var notificationId = Guid.NewGuid().ToString();
        var notification = new NotificationRecord
        {
            Id = notificationId,
            CustomerId = @event.CustomerId,
            Type = @event.Type,
            Channel = channel,
            Subject = @event.Subject,
            Message = @event.Message,
            SentAt = DateTime.UtcNow,
            Status = NotificationStatus.Sent
        };

        // Send notification
        var success = await SendNotificationAsync(notification);
        if (!success)
        {
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = "Failed to send notification";
        }

        // Update state
        RaiseEvent(new NotificationSentLogEvent { Record = notification });
        await ConfirmEvents();

        Logger.LogInformation("Notification {NotificationId} processed with status {Status}", 
            notificationId, notification.Status);
    }

    private async Task<bool> SendNotificationAsync(NotificationRecord notification)
    {
        // Simulate sending notification via different channels
        await Task.Delay(100); // Simulate network call
        
        return notification.Channel switch
        {
            NotificationChannel.Email => await SendEmailAsync(notification),
            NotificationChannel.SMS => await SendSmsAsync(notification),
            NotificationChannel.Push => await SendPushNotificationAsync(notification),
            _ => false
        };
    }

    private Task<bool> SendEmailAsync(NotificationRecord notification)
    {
        // Email sending logic
        Logger.LogInformation("Sending email to customer {CustomerId}: {Subject}", 
            notification.CustomerId, notification.Subject);
        return Task.FromResult(new Random().NextDouble() > 0.05); // 95% success rate
    }

    private Task<bool> SendSmsAsync(NotificationRecord notification)
    {
        // SMS sending logic  
        Logger.LogInformation("Sending SMS to customer {CustomerId}: {Subject}", 
            notification.CustomerId, notification.Subject);
        return Task.FromResult(new Random().NextDouble() > 0.03); // 97% success rate
    }

    private Task<bool> SendPushNotificationAsync(NotificationRecord notification)
    {
        // Push notification logic
        Logger.LogInformation("Sending push notification to customer {CustomerId}: {Subject}", 
            notification.CustomerId, notification.Subject);
        return Task.FromResult(new Random().NextDouble() > 0.02); // 98% success rate
    }

    protected override void GAgentTransitionState(NotificationState state, StateLogEventBase<NotificationStateLogEvent> @event)
    {
        switch (@event)
        {
            case NotificationSentLogEvent e:
                // Add to customer notification history
                if (!state.CustomerNotifications.ContainsKey(e.Record.CustomerId))
                    state.CustomerNotifications[e.Record.CustomerId] = new List<NotificationRecord>();
                
                state.CustomerNotifications[e.Record.CustomerId].Add(e.Record);
                
                // Update statistics
                state.TotalNotificationsSent++;
                state.LastNotificationTime = e.Record.SentAt;
                break;
        }
    }

    // ... 实现细节
}

// === 使用 ===
public class ECommerceService
{
    private readonly IGAgentFactory _gAgentFactory;

    public async Task SetupSystemAsync()
    {
        // 创建协调器
        var coordinator = await _gAgentFactory.GetGAgentAsync<ICoordinatorGAgent>();

        // 创建并注册所有代理
        var orderAgent = await _gAgentFactory.GetGAgentAsync<IOrderGAgent>();
        var inventoryAgent = await _gAgentFactory.GetGAgentAsync<IInventoryGAgent>();
        var paymentAgent = await _gAgentFactory.GetGAgentAsync<IPaymentGAgent>();

        // 注册进行事件通信
        await coordinator.RegisterAsync(orderAgent);
        await coordinator.RegisterAsync(inventoryAgent);
        await coordinator.RegisterAsync(paymentAgent);

        Logger.LogInformation("电商系统设置完成");
    }

    public async Task<string> ProcessOrderAsync(SubmitOrderRequest request)
    {
        var orderAgent = await _gAgentFactory.GetGAgentAsync<IOrderGAgent>();
        return await orderAgent.SubmitOrderAsync(request);
    }
}
```

这个完整示例演示了：
- 多 GAgent 协调
- 事件驱动工作流
- 跨代理状态管理
- 错误处理和验证
- 真实世界的业务逻辑

---

## 🎯 快速实现清单

### 基础 GAgent
- [ ] 定义继承自 `IStateGAgent<TState>` 的接口
- [ ] 创建带有 `[GenerateSerializer]` 和 `[Id(n)]` 属性的状态类
- [ ] 定义继承自 `StateLogEventBase<T>` 的状态日志事件
- [ ] 实现带有 `[GAgent]` 特性的 GAgent
- [ ] 使用 `RaiseEvent` + `ConfirmEvents` 进行状态变化
- [ ] 在 `GAgentTransitionState` 中处理状态转换
- [ ] 在状态类中初始化集合
- [ ] 使用 `Logger` 属性（非注入）

### AI 增强的 GAgent  
- [ ] 接口同时继承 `IStateGAgent<TState>` 和 `IAIGAgent`
- [ ] 继承自 `AIGAgentBase<TState, TStateLogEvent>`
- [ ] 重写 `AIGAgentTransitionState`（不是 `GAgentTransitionState`）
- [ ] 使用 `InitializeDto` 调用 `InitializeAsync`
- [ ] 配置 LLM 设置和工具
- [ ] 使用 `ChatAsync` 进行 AI 交互

### 事件系统
- [ ] 定义继承自 `EventBase` 的事件
- [ ] 使用 `[EventHandler]` 特性或 `HandleEventAsync` 命名
- [ ] 使用 `PublishAsync` 发布事件
- [ ] 将 GAgent 注册到协调器进行通信
- [ ] 异步处理事件

### 高级功能
- [ ] 使用 `IGAgentFactory`（绝不使用 `IGrainFactory`）
- [ ] 在 `OnGAgentActivateAsync` 中注册定时器
- [ ] 在 `OnDeactivateAsync` 中释放定时器
- [ ] 通过 `ServiceProvider` 访问服务
- [ ] 使用配置类进行初始化

---

## 🔗 下一步

1. **尝试示例**：从计数器 GAgent 开始，然后构建更复杂的
2. **探索智能家居演示**：查看完整的多 GAgent 系统实际应用
3. **阅读 API 文档**：深入了解特定类和方法
4. **加入社区**：与其他使用 Aevatar 构建的开发者联系

祝你使用 GAgent 编码愉快！🚀 