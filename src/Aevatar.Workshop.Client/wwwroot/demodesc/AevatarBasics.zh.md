# Aevatar 基础概念 - 开发者入门指南

欢迎来到 **Aevatar** 框架！本文档将为您介绍构建分布式多智能体系统的核心概念。

## 🎯 概述

Aevatar 是一个基于 **Orleans Virtual Actor** 模型的分布式多智能体框架，专为构建可扩展、高并发的智能体系统而设计。如果您熟悉 C# 和基本的 Agent 概念，那么您已经具备了开始使用 Aevatar 的基础。

### 核心优势
- ✅ **高并发**：基于 Orleans Virtual Actor 模型，天然支持大规模并发
- ✅ **自动扩容**：无需手动管理实例，Orleans 自动处理负载均衡
- ✅ **事件驱动**：通过 Orleans Streaming 实现松耦合的智能体通信
- ✅ **状态持久化**：基于 Event Sourcing 的可靠状态管理

---

## 🤖 1. GAgent - 智能体的核心

### 什么是 GAgent？

**GAgent** 是 Aevatar 中智能体的基础抽象。每个 GAgent 都是一个独立的、有状态的计算单元，类似于一个"数字员工"，能够：

- 🧠 **独立思考**：处理业务逻辑和决策
- 💾 **记住信息**：维护自己的状态数据
- 📡 **与他人通信**：通过事件与其他 GAgent 协作
- ⚡ **并发工作**：同时处理多个任务

```csharp
// 定义一个简单的 GAgent
[GAgent("calculator", "math")]
public class CalculatorGAgent : GAgentBase<CalculatorState, CalculatorStateLogEvent>, ICalculatorGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("一个能够执行数学计算的智能体");
    
    // GAgent 的业务逻辑
    public async Task<double> CalculateAsync(string expression)
    {
        var result = EvaluateExpression(expression);
        
        // 通过事件更新状态
        RaiseEvent(new CalculationPerformedEvent 
        { 
            Expression = expression, 
            Result = result 
        });
        
        await ConfirmEvents();
        return result;
    }
}
```

### Orleans Virtual Actor 模型

GAgent 基于 **Orleans Virtual Actor** 模型实现，这意味着：

1. **位置透明性**：您无需关心 GAgent 运行在哪台服务器上
2. **自动激活**：Orleans 根据需要自动创建和销毁实例
3. **单线程保证**：每个 GAgent 实例同时只处理一个请求，避免并发问题
4. **故障转移**：系统自动处理节点故障和负载均衡

> 📚 **深入学习**：[Orleans Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/) - Virtual Actor 模型的官方文档

### 并发和扩容

由于基于 Orleans，Aevatar 天然支持：

- **水平扩容**：添加更多服务器节点即可提升处理能力
- **智能负载均衡**：Orleans 自动分配 GAgent 到最优节点
- **弹性伸缩**：根据负载自动调整资源使用

---

## 📡 2. Event Handler - 智能体间的通信机制

### 事件驱动架构

在 Aevatar 中，GAgent 之间主要通过 **事件（Events）** 进行通信，而不是直接调用。这种模式被称为**事件驱动架构**。

### 定义和使用 Event Handler

#### 1. 定义事件

```csharp
// 定义一个事件
[GenerateSerializer]
public record CalculationRequestEvent : EventBase
{
    [Id(0)] public string Expression { get; init; } = string.Empty;
    [Id(1)] public string RequestId { get; init; } = string.Empty;
}
```

#### 2. 实现 Event Handler

```csharp
public class CalculatorGAgent : GAgentBase<CalculatorState, CalculatorStateLogEvent>
{
    // 使用 [EventHandler] 特性标记事件处理方法
    [EventHandler]
    public async Task HandleCalculationRequestAsync(CalculationRequestEvent request)
    {
        Logger.LogInformation("收到计算请求: {Expression}", request.Expression);
        
        // 执行计算逻辑
        var result = await CalculateAsync(request.Expression);
        
        // 发布结果事件
        await PublishAsync(new CalculationCompletedEvent 
        { 
            RequestId = request.RequestId,
            Result = result 
        });
    }
}
```

#### 3. 发布事件

```csharp
// 从一个 GAgent 向其他 GAgent 发送事件
await PublishAsync(new CalculationRequestEvent 
{ 
    Expression = "2 + 3 * 4",
    RequestId = Guid.NewGuid().ToString()
});
```

### Orleans Streaming 底层机制

Aevatar 的事件系统基于 **Orleans Streaming** 实现：

- **可靠传递**：确保事件不会丢失
- **顺序保证**：同一 GAgent 发送的事件按顺序处理
- **背压控制**：自动处理消费者处理能力不足的情况
- **持久化流**：支持事件的持久化存储

> 📚 **深入学习**：[Orleans Streams Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/streaming/) - Orleans 流处理的官方文档

### Event Handler 的优势

1. **松耦合**：GAgent 之间无需直接依赖
2. **异步处理**：事件异步传递，提升系统响应性
3. **可扩展性**：容易添加新的事件处理逻辑
4. **可观测性**：事件流提供了清晰的系统行为轨迹

---

## 💾 3. Event Sourcing - 状态管理的艺术

### 什么是 Event Sourcing？

**Event Sourcing**（事件溯源）是一种状态管理模式，其核心思想是：**不直接存储当前状态，而是存储导致状态变化的事件序列**。

### 传统方式 vs Event Sourcing

```csharp
// ❌ 传统方式（避免）
public void UpdateBalance(decimal amount)
{
    State.Balance += amount;  // 直接修改状态
}

// ✅ Event Sourcing 方式
public async Task DepositAsync(decimal amount)
{
    // 1. 创建事件
    RaiseEvent(new MoneyDepositedEvent { Amount = amount });
    
    // 2. 提交事件
    await ConfirmEvents();
}

// 3. 在状态转换方法中应用事件
protected override void GAgentTransitionState(AccountState state, StateLogEventBase<AccountStateLogEvent> @event)
{
    switch (@event)
    {
        case MoneyDepositedEvent depositEvent:
            state.Balance += depositEvent.Amount;
            state.LastTransactionTime = DateTime.UtcNow;
            break;
    }
}
```

### Event Sourcing 的完整流程

#### 1. 定义状态类

```csharp
[GenerateSerializer]
public class AccountState : StateBase
{
    [Id(0)] public decimal Balance { get; set; }
    [Id(1)] public DateTime LastTransactionTime { get; set; }
    [Id(2)] public List<string> TransactionHistory { get; set; } = new();
}
```

#### 2. 定义状态日志事件

```csharp
[GenerateSerializer]
public abstract record AccountStateLogEvent : StateLogEventBase<AccountStateLogEvent>;

[GenerateSerializer]
public record MoneyDepositedEvent : AccountStateLogEvent
{
    [Id(0)] public decimal Amount { get; init; }
}

[GenerateSerializer]
public record MoneyWithdrawnEvent : AccountStateLogEvent
{
    [Id(0)] public decimal Amount { get; init; }
}
```

#### 3. 实现状态转换逻辑

```csharp
protected override void GAgentTransitionState(AccountState state, StateLogEventBase<AccountStateLogEvent> @event)
{
    switch (@event)
    {
        case MoneyDepositedEvent deposit:
            state.Balance += deposit.Amount;
            state.TransactionHistory.Add($"存入: {deposit.Amount:C}");
            state.LastTransactionTime = DateTime.UtcNow;
            break;
            
        case MoneyWithdrawnEvent withdrawal:
            state.Balance -= withdrawal.Amount;
            state.TransactionHistory.Add($"取出: {withdrawal.Amount:C}");
            state.LastTransactionTime = DateTime.UtcNow;
            break;
    }
}
```

### Event Sourcing 的优势

1. **完整审计日志**：每个状态变化都有完整记录
2. **时间旅行**：可以重播事件序列，查看任意时间点的状态
3. **调试友好**：通过事件序列可以完整追踪系统行为
4. **数据一致性**：避免了并发修改导致的数据不一致问题

> 📚 **深入学习**：[Event Sourcing Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing) - 微软官方的 Event Sourcing 模式文档

---

## 🔄 完整的工作流程

让我们通过一个完整的示例来理解这三个概念是如何协作的：

```csharp
// 1. 智能家居灯光控制 GAgent
[GAgent("light", "smarthome")]
public class LightGAgent : GAgentBase<LightState, LightStateLogEvent>, ILightGAgent
{
    // 2. Event Handler - 处理开灯事件
    [EventHandler]
    public async Task HandleTurnOnLightAsync(TurnOnLightEvent lightEvent)
    {
        Logger.LogInformation("收到开灯指令: {Location}", lightEvent.Location);
        
        // 3. Event Sourcing - 通过事件更新状态
        RaiseEvent(new LightTurnedOnEvent 
        { 
            Location = lightEvent.Location,
            Brightness = lightEvent.Brightness ?? 100
        });
        
        await ConfirmEvents();
        
        // 4. 发布状态变化事件给其他 GAgent
        await PublishAsync(new LightStateChangedEvent 
        { 
            Location = lightEvent.Location,
            IsOn = true,
            Brightness = lightEvent.Brightness ?? 100
        });
    }
    
    // 状态转换逻辑
    protected override void GAgentTransitionState(LightState state, StateLogEventBase<LightStateLogEvent> @event)
    {
        switch (@event)
        {
            case LightTurnedOnEvent turnedOn:
                state.IsOn = true;
                state.Brightness = turnedOn.Brightness;
                state.LastModified = DateTime.UtcNow;
                break;
        }
    }
}
```

### 执行流程：

1. **事件触发**：AI GAgent 发送 `TurnOnLightEvent`
2. **Event Handler 响应**：Light GAgent 的 `HandleTurnOnLightAsync` 被调用
3. **Event Sourcing 更新**：通过 `LightTurnedOnEvent` 更新内部状态
4. **状态广播**：发布 `LightStateChangedEvent` 通知其他关注的 GAgent

---

## 🏗️ 最佳实践

### 1. GAgent 设计原则
- **单一职责**：每个 GAgent 专注于一个业务领域
- **无状态方法**：业务方法不应直接修改状态
- **异步优先**：使用 `async/await` 处理所有 I/O 操作

### 2. Event Handler 最佳实践
- **幂等性**：确保重复处理同一事件不会产生副作用
- **快速处理**：Event Handler 应该快速完成，避免阻塞
- **错误处理**：妥善处理异常，避免影响其他事件

### 3. Event Sourcing 注意事项
- **事件不变性**：一旦创建，事件内容不应修改
- **向后兼容**：新版本的事件结构应与旧版本兼容
- **适度粒度**：事件粒度要适中，既不过细也不过粗

---

## 🎮 实践体验

想要亲手体验这些概念？请访问我们的 **[Smart Home Demo](http://localhost:5000/demos/smart-home-demo.html)**：

1. 🏠 观察 AI GAgent 如何理解用户指令
2. 📡 查看事件如何在不同的设备 GAgent 之间传递
3. 💾 体验 Event Sourcing 如何更新设备状态
4. 🔄 理解完整的事件驱动架构流程

> 💡 **提示**：即使没有配置 LLM API Key，您也可以通过手动操作设备来观察 GAgent 的事件交互过程！

---

## 📚 术语表

| 术语 | 英文 | 说明 |
|------|------|------|
| **GAgent** | Generic Agent | Aevatar 框架中的智能体基础类，封装了状态管理和事件处理 |
| **Virtual Actor** | Virtual Actor | Orleans 框架的核心概念，提供位置透明的分布式对象模型 |
| **Event Handler** | Event Handler | 处理特定事件类型的方法，使用 `[EventHandler]` 特性标记 |
| **Event Sourcing** | Event Sourcing | 通过存储事件序列而不是直接存储状态来管理数据的模式 |
| **State Transition** | State Transition | 基于事件将当前状态转换为新状态的过程 |
| **Orleans Streaming** | Orleans Streaming | Orleans 框架提供的流处理基础设施，支持可靠的异步消息传递 |
| **Grain** | Grain | Orleans 中 Virtual Actor 的具体实现，GAgent 基于此构建 |
| **Event Log** | Event Log | 存储所有状态变化事件的日志，用于状态重建和审计 |
| **State Log Event** | State Log Event | 专用于状态管理的内部事件，不同于 GAgent 间通信的外部事件 |
| **Event Sourcing Pattern** | Event Sourcing Pattern | 一种架构模式，将数据存储为事件序列而非最终状态 |
| **Immutable Event** | Immutable Event | 不可变事件，一旦创建就不能修改，确保历史记录的完整性 |
| **Event Replay** | Event Replay | 重新执行事件序列以重建特定时间点的状态 |

---

## 🔗 扩展阅读

### Orleans 相关
- [Orleans 官方文档](https://docs.microsoft.com/en-us/dotnet/orleans/) - Orleans Virtual Actor 框架完整文档
- [Orleans Streams](https://docs.microsoft.com/en-us/dotnet/orleans/streaming/) - Orleans 流处理文档

### Event Sourcing
- [Event Sourcing Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing) - 微软架构指南
- [Event Sourcing by Martin Fowler](https://martinfowler.com/eaaDev/EventSourcing.html) - Martin Fowler 的经典文章

### 分布式系统
- [Azure Architecture Guide](https://learn.microsoft.com/en-us/azure/architecture/guide/) - Azure 应用架构基础指南
- [Actor Model](https://en.wikipedia.org/wiki/Actor_model) - Actor 模型的理论基础

---

**准备好开始您的 Aevatar 之旅了吗？** 

从 Smart Home Demo 开始，亲身体验这些概念是如何协同工作的！🚀 