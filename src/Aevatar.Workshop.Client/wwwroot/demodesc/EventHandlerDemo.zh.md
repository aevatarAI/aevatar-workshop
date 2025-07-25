# GAgent 事件处理器演示

## 概述

事件处理器演示展示了 Aevatar 框架中 GAgent 的事件驱动架构。这个演示说明了 GAgent 如何处理事件、相互通信，以及通过事件溯源维护状态。

## 核心概念

### 1. 事件处理器
GAgent 可以通过两种方式定义事件处理器：
- **[EventHandler] 特性**：显式标记方法为事件处理器
- **方法名约定**：名为 `HandleEventAsync` 的方法会被自动识别

### 2. 事件类型
演示包含多种事件类型：
- **NotificationEvent（通知事件）**：用于发送不同严重级别的通知
- **DataProcessingEvent（数据处理事件）**：用于触发支持优先级的数据处理任务
- **CoordinationRequestEvent（协调请求事件）**：用于协调多个 GAgent 协同工作
- **EventLoggedEvent（事件记录事件）**：用于记录事件处理活动

### 3. PublishingGAgent - 事件中心
演示使用 `PublishingGAgent` 作为中央父代理：
```csharp
public interface IPublishingGAgent : IGAgent
{
    Task PublishEventAsync<T>(T @event) where T : EventBase;
}
```
- 作为所有演示 GAgent 的父代理
- 将事件转发给所有注册的子代理
- 提供集中的事件分发点

### 4. 演示 GAgents

#### NotificationGAgent
- 处理和记录通知事件的演示 GAgent
- 维护通知历史记录
- 按级别跟踪通知统计
- 为错误通知触发数据处理

#### ProcessingGAgent
- 处理数据处理任务的演示 GAgent，支持优先级队列
- 模拟异步任务处理
- 发送完成通知
- 演示 [EventHandler] 和方法名约定两种方式

#### CoordinatorDemoGAgent
- 协调多个 GAgent 协同工作的演示 GAgent
- 管理参与代理的任务接受情况
- 跟踪协调成功率
- 演示代理间通信

#### EventLoggerGAgent
- 记录和分析系统中所有事件的演示 GAgent
- 使用 [AllEventHandler] 记录系统中的所有事件
- 可以注册为任何代理的子代理来记录其事件
- 当注册到 PublishingGAgent 时，可以看到系统中流动的所有事件
- 提供事件搜索和过滤功能
- 计算事件统计信息

## 功能特性

### 交互式界面
- **GAgent 选择**：点击任意 GAgent 查看其事件处理器和统计信息
- **事件触发器**：手动触发不同类型的事件
- **实时更新**：事件日志自动更新
- **场景模拟**：运行预配置场景查看代理的实际运行

### 事件流可视化
1. 从 UI 触发事件
2. 事件发布到订阅的 GAgent
3. 每个 GAgent 根据其处理器处理事件
4. 处理结果触发新事件
5. EventLogger 记录所有活动

### 状态管理
- 每个 GAgent 维护自己的状态
- 通过事件溯源更新状态
- 从累积状态计算统计信息

## 使用方法

1. **初始化演示**：点击"初始化Demo"创建并设置所有演示 GAgent
   - 创建一个 `PublishingGAgent` 作为父代理
   - 将所有演示 GAgent（`NotificationGAgent`、`ProcessingGAgent`、`CoordinatorDemoGAgent`）注册为子代理
   - `EventLoggerGAgent` 订阅其他代理以记录它们的事件
2. **选择 GAgent**：点击任意 GAgent 卡片查看详情
3. **触发事件**：使用事件触发表单发送事件
   - 事件通过父代理 `PublishingGAgent` 发布
   - 父代理将事件转发给所有注册的子代理
   - 子代理也可以向上发布事件到父代理
4. **运行场景**：点击"模拟场景"进行自动演示
5. **监控活动**：观察事件日志的实时更新

## 技术细节

### 事件处理器模式

```csharp
// 显式特性
[EventHandler]
public async Task HandleNotificationAsync(NotificationEvent @event) { }

// 方法名约定
public Task HandleEventAsync(EventBase @event) { }

// 处理所有事件
[AllEventHandler]
public Task LogAllEventsAsync(EventWrapperBase eventWrapper) { }
```

### 事件发布

事件可以通过多种方式发布：

```csharp
// 1. 从 GAgent 内部（发布到父代理和自身）
await PublishAsync(new NotificationEvent { 
    Title = "任务完成",
    Message = "处理成功完成"
});

// 2. 通过 PublishingGAgent 父代理（广播到所有子代理）
var publishingAgent = await gAgentFactory.GetGAgentAsync<IPublishingGAgent>(publishingAgentId);
await publishingAgent.PublishEventAsync(new DataProcessingEvent {
    DataType = "用户数据",
    Priority = ProcessingPriority.High
});

// 3. 直接事件发布到特定目标
await PublishEventAsync(myEvent, targetGAgent);
```

### 父子关系和事件流

GAgent 使用层级父子结构进行事件通信：

#### 建立关系
```csharp
// 将子代理注册到父代理
await parentAgent.RegisterAsync(childAgent1);
await parentAgent.RegisterAsync(childAgent2);

// 或一次注册多个
await parentAgent.RegisterManyAsync(new List<IGAgent> { child1, child2, child3 });
```

当调用 `RegisterAsync` 时：
1. 子代理被添加到父代理的 `State.Children` 列表
2. 子代理的 `State.Parent` 设置为父代理的 GrainId
3. 子代理自动订阅父代理的事件流

#### 事件流向

**1. 向上事件发布（子 → 父）**
```csharp
// 在子 GAgent 中，向父代理发布事件
await PublishAsync(new NotificationEvent { 
    Title = "任务完成",
    Message = "处理已完成"
});
// 此事件会向上传递到父代理
```

**2. 向下事件转发（父 → 子）**
```csharp
// 带有 [AllEventHandler] 的父 GAgent 会自动将事件转发给子代理
[AllEventHandler(allowSelfHandling: true)]
protected virtual async Task ForwardEventAsync(EventWrapperBase eventWrapper)
{
    // 事件自动转发给所有子代理
    await SendEventDownwardsAsync(eventWrapper);
}
```

**3. 定向事件发布**
```csharp
// 父代理可以发布事件，所有子代理都会接收到
await parentAgent.PublishEventAsync(new CoordinationRequestEvent {
    TaskName = "处理数据",
    RequiredAgents = new List<string> { "agent1", "agent2" }
});
```

#### 事件流示例
```
                  父 GAgent
                  /    |    \
               /       |       \
         子代理1    子代理2    子代理3
           ↑          ↑          ↑
           └──────────┴──────────┘
          事件从子代理向上流动
                    并且
          事件从父代理向下流动
```

#### 注销和清理
```csharp
// 从父代理移除子代理
await parentAgent.UnregisterAsync(childAgent);

// 这将会：
// 1. 从父代理的 State.Children 中移除子代理
// 2. 清除子代理的 State.Parent
// 3. 取消子代理对父代理事件流的订阅
```

## 优势

- **松耦合**：GAgent 通过事件通信，没有直接依赖
- **可扩展性**：事件驱动架构与 Orleans 自然扩展
- **弹性**：事件溯源提供自然的恢复机制
- **灵活性**：易于添加新的事件类型和处理器
- **可观察性**：所有事件都可以被记录和监控

这个演示为理解如何使用 Aevatar 框架的 GAgent 架构构建复杂的事件驱动系统提供了基础。 