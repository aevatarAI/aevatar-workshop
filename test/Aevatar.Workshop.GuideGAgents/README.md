# 电商订单处理系统 - GAgent 实现

这是一个完整的电商订单处理系统实现，展示了 GAgent 开发指南中描述的多智能体协作模式。

## 📋 系统概述

这个系统实现了一个完整的电商订单处理流程，包括：

- **订单管理** - 处理订单提交、状态跟踪和生命周期管理
- **库存管理** - 产品库存跟踪、预留和自动扣减
- **支付处理** - 支付验证、处理和退款功能
- **通知服务** - 多渠道通知发送和用户偏好管理
- **系统协调** - 中央协调器管理所有 GAgent 协作

## 🏗️ 架构设计

### GAgent 组件

1. **OrderGAgent** - 订单管理智能体
    - 处理订单提交和状态管理
    - 维护客户订单历史
    - 响应库存验证和支付事件

2. **InventoryGAgent** - 库存管理智能体
    - 产品库存跟踪
    - 库存预留和释放
    - 自动库存验证

3. **PaymentGAgent** - 支付处理智能体
    - 多种支付方式支持
    - 支付验证和处理
    - 退款功能

4. **NotificationGAgent** - 通知服务智能体
    - 多渠道通知发送（Email, SMS, Push）
    - 用户通知偏好管理
    - 事件驱动的自动通知

5. **ECommerceCoordinatorGAgent** - 系统协调器
    - 系统初始化和健康检查
    - GAgent 注册和通信协调
    - 测试数据管理

### 事件流程

```
订单提交 → 库存验证 → 支付处理 → 发货通知 → 订单完成
    ↓         ↓         ↓         ↓         ↓
  通知发送   通知发送   通知发送   通知发送   通知发送
```

## 📁 文件结构

```
src/Aevatar.Workshop.GuideGAgents/
├── Events/
│   └── ECommerceEvents.cs           # 共享事件定义
├── GAgents/
│   ├── OrderGAgent.cs               # 订单管理 GAgent
│   ├── InventoryGAgent.cs           # 库存管理 GAgent
│   ├── PaymentGAgent.cs             # 支付处理 GAgent
│   ├── NotificationGAgent.cs        # 通知服务 GAgent
│   └── ECommerceCoordinatorGAgent.cs # 系统协调器
└── README.md                        # 本文档
```

## 🚀 核心特性

### 事件驱动架构
- 所有 GAgent 通过强类型事件通信
- 完整的事件溯源状态管理
- 自动化的业务流程协调

### 状态管理
- 所有状态变化通过事件记录
- 完整的审计跟踪
- 支持状态回溯和重放

### 错误处理
- 自动重试机制
- 优雅的降级处理
- 完整的错误日志记录

### 扩展性设计
- 模块化的 GAgent 设计
- 易于添加新的业务逻辑
- 支持水平扩展

## 💻 使用示例

### 1. 系统初始化

```csharp
// 注册服务
services.AddSingleton<ECommerceService>();

// 初始化系统
var ecommerceService = serviceProvider.GetService<ECommerceService>();
await ecommerceService.InitializeAsync();
```

### 2. 处理订单

```csharp
var orderRequest = new SubmitOrderRequest
{
    CustomerId = "customer-123",
    CustomerEmail = "customer@example.com",
    Items = new List<OrderItemRequest>
    {
        new OrderItemRequest
        {
            ProductId = "prod-001",
            ProductName = "iPhone 15 Pro",
            Quantity = 1,
            Price = 8999.00m,
            SKU = "IPH15PRO-256GB"
        }
    },
    ShippingAddress = new Address
    {
        Street = "123 Main St",
        City = "北京",
        State = "Beijing",
        PostalCode = "100000",
        Country = "China"
    }
};

// 提交订单
var orderId = await ecommerceService.ProcessOrderAsync(orderRequest);

// 查询订单状态
var status = await ecommerceService.GetOrderStatusAsync(orderId);
```

### 3. 支付处理

```csharp
var paymentRequest = new PaymentRequest
{
    OrderId = orderId,
    Amount = 8999.00m,
    Currency = "CNY",
    PaymentMethod = new PaymentMethod
    {
        Type = PaymentMethodType.CreditCard,
        CardNumber = "4111111111111111",
        ExpiryDate = "12/25",
        CVV = "123",
        CardHolderName = "John Doe"
    }
};

var paymentResult = await ecommerceService.ProcessPaymentAsync(orderId, paymentRequest);
```

### 4. 系统健康检查

```csharp
var healthStatus = await ecommerceService.GetSystemHealthAsync();
Console.WriteLine($"系统健康状态: {healthStatus.IsHealthy}");

foreach (var component in healthStatus.ComponentStatuses)
{
    Console.WriteLine($"{component.Key}: {component.Value.Message}");
}
```

## 📊 业务流程详解

### 订单处理流程

1. **订单提交** (`OrderSubmittedEvent`)
    - 客户提交订单
    - 订单 GAgent 创建订单记录
    - 发布订单提交事件

2. **库存验证** (`OrderValidatedEvent`)
    - 库存 GAgent 接收订单事件
    - 检查商品可用性
    - 预留库存或返回验证失败

3. **支付处理** (`PaymentProcessedEvent`)
    - 支付 GAgent 处理支付请求
    - 验证支付信息
    - 返回支付结果

4. **库存扣减**
    - 支付成功后自动扣减库存
    - 释放预留转为实际扣减

5. **通知发送** (`OrderNotificationEvent`)
    - 各个阶段自动发送通知
    - 支持多种通知渠道
    - 基于用户偏好发送

### 状态管理模式

每个 GAgent 都遵循相同的状态管理模式：

```csharp
// 1. 触发事件
RaiseEvent(new SomeStateLogEvent { Data = newData });

// 2. 确认事件
await ConfirmEvents();

// 3. 状态转换在 GAgentTransitionState 中处理
protected override void GAgentTransitionState(State state, StateLogEventBase @event)
{
    switch (@event)
    {
        case SomeStateLogEvent e:
            state.SomeProperty = e.Data;
            break;
    }
}
```

## 🎯 关键实现要点

### 1. 遵循 GAgent 开发指南
- ✅ 所有 State 和 StateLogEvent 定义为 `class`
- ✅ 使用 `IGAgentFactory` 而非 `IGrainFactory`
- ✅ 通过 `RaiseEvent` + `ConfirmEvents` 修改状态
- ✅ 在 `GAgentTransitionState` 中处理状态转换
- ✅ 正确的事件处理器模式

### 2. 事件驱动协作
- 所有 GAgent 通过事件进行通信
- 使用协调器注册所有 GAgent
- 事件自动路由到相关处理器

### 3. 错误处理和弹性
- 优雅的错误处理和日志记录
- 支付和通知的重试机制
- 库存不足的自动回滚

### 4. 可扩展性
- 模块化的 GAgent 设计
- 易于添加新的业务功能
- 支持系统健康监控

## 🧪 测试数据

系统启动时会自动创建测试产品：

- **iPhone 15 Pro** (prod-001) - ¥8,999, 库存: 50
- **MacBook Pro M3** (prod-002) - ¥15,999, 库存: 30
- **AirPods Pro** (prod-003) - ¥1,999, 库存: 100

## 📚 扩展指南

### 添加新的 GAgent

1. 创建接口继承 `IStateGAgent<TState>`
2. 定义状态类继承 `StateBase`
3. 定义状态日志事件继承 `StateLogEventBase<T>`
4. 实现 GAgent 继承 `GAgentBase<TState, TStateLogEvent>`
5. 在协调器中注册新的 GAgent

### 添加新的业务事件

1. 在 `Events/ECommerceEvents.cs` 中定义事件
2. 在相关 GAgent 中添加事件处理器
3. 使用 `[EventHandler]` 特性标记处理方法

### 添加新的通知类型

1. 在 `NotificationType` 枚举中添加新类型
2. 在 `NotificationGAgent` 中添加处理逻辑
3. 更新通知偏好设置

## 🎉 总结

这个电商订单处理系统完整展示了：

- **多 GAgent 协作**：4个专业化 GAgent + 1个协调器
- **事件驱动架构**：完整的事件流和状态管理
- **业务流程自动化**：从订单到发货的完整流程
- **错误处理和弹性**：优雅的错误处理和恢复
- **可扩展性设计**：易于添加新功能和 GAgent

这是一个生产级别的 GAgent 系统实现示例，可以作为构建复杂业务系统的参考模板！🚀 