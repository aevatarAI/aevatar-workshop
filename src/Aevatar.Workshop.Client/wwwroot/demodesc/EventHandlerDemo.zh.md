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

### 3. 演示 GAgents

#### NotificationGAgent（通知处理器）
- 处理通知事件
- 维护通知历史记录
- 按级别跟踪通知统计
- 为错误通知触发数据处理

#### ProcessingGAgent（数据处理器）
- 支持优先级队列的数据处理
- 模拟异步任务处理
- 发送完成通知
- 演示 [EventHandler] 和方法名约定两种方式

#### CoordinatorDemoGAgent（协调器）
- 协调多个 GAgent 完成复杂任务
- 管理参与代理的任务接受情况
- 跟踪协调成功率
- 演示代理间通信

#### EventLoggerGAgent（事件记录器）
- 使用 [AllEventHandler] 记录系统中的所有事件
- 提供事件搜索和过滤功能
- 计算事件统计信息
- 展示如何全局处理所有事件

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
2. **选择 GAgent**：点击任意 GAgent 卡片查看详情
3. **触发事件**：使用事件触发表单发送事件
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

GAgent 可以向其订阅者发布事件：
```csharp
await PublishAsync(new NotificationEvent { 
    Title = "任务完成",
    Message = "处理成功完成"
});
```

### 订阅管理

GAgent 可以订阅其他 GAgent：
```csharp
await agent1.SubscribeToAsync(agent2);
```

## 优势

- **松耦合**：GAgent 通过事件通信，没有直接依赖
- **可扩展性**：事件驱动架构与 Orleans 自然扩展
- **弹性**：事件溯源提供自然的恢复机制
- **灵活性**：易于添加新的事件类型和处理器
- **可观察性**：所有事件都可以被记录和监控

这个演示为理解如何使用 Aevatar 框架的 GAgent 架构构建复杂的事件驱动系统提供了基础。 