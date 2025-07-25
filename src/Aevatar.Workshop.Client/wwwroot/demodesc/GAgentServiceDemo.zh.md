# GAgentService & GAgentExecutor 演示

本演示展示了 **GAgentService** 和 **GAgentExecutor** 的核心功能，这是 Aevatar GAgent 框架中的两个基础组件。

## 概述

### GAgentService
`IGAgentService` 接口提供了发现和查询系统中可用 GAgent 信息的方法：

- **GetAllAvailableGAgentInformation()** - 返回所有已注册的 GAgent 及其支持的事件类型
- **GetGAgentDetailInfoAsync()** - 获取特定 GAgent 的详细信息，包括描述和配置
- **FindGAgentsByEventTypeAsync()** - 查找所有能处理特定事件类型的 GAgent

### GAgentExecutor
`IGAgentExecutor` 接口支持动态执行 GAgent 事件处理器：

- **ExecuteGAgentEventHandler()** - 在 GAgent 上执行事件处理器并返回结果

## 演示功能

这个交互式演示允许您：

1. **列出所有 GAgent** - 查看系统中所有可用的 GAgent 及其支持的事件
2. **获取 GAgent 详情** - 查询特定 GAgent 的详细信息
3. **按事件查找 GAgent** - 搜索支持特定事件类型的 GAgent
4. **执行 GAgent** - 使用自定义参数动态执行选定 GAgent 上的事件

## 工作原理

该演示提供了一个实时界面来探索 GAgent 生态系统：

- **发现机制**：使用反射和 Orleans grain 元数据自动发现所有已注册的 GAgent
- **缓存优化**：实现智能缓存以提高查询 GAgent 信息时的性能
- **动态执行**：使用 GAgentExecutor 调用事件处理器，并提供适当的超时处理
- **类型安全**：在执行前验证事件类型和参数

## 特色工具：MathGAgent (tools.math)

### 让数学计算变得简单

**MathGAgent**（可通过 `tools.math` 访问）是一个功能强大的数学计算代理，建议您首先体验。它可以计算复杂的数学表达式，包括：

- **基础算术**：加法 (+)、减法 (-)、乘法 (*)、除法 (/)
- **幂运算和开方**：
  - 幂运算：`10^3` 或 `10**3` 表示 10³
  - 平方根：`sqrt(16)` → 4
  - 立方根：`cbrt(27)` → 3
  - N 次方根：`35^(1/3)` 表示 ∛35
- **三角函数**：sin、cos、tan（例如：`sin(3.14159/2)` → 1）
- **对数函数**：log、ln（例如：`ln(2.71828)` → 1）

### 试试这些示例：
1. **简单计算**：`2 + 2 * 3` → 8
2. **幂运算**：`10^3` → 1000
3. **开方运算**：`35^(1/3)` → 3.271...（35 的立方根）
4. **复杂表达式**：`sqrt(16) + sin(3.14159/2) * 10` → 14

### 如何使用 MathGAgent：
1. 在演示中点击"执行 GAgent"
2. 选择"MathGAgent"或搜索"tools.math"
3. 选择"MathCalculateEvent"事件类型
4. 在参数中输入您的数学表达式
5. 点击执行查看结果！

## 其他可用工具

您可以自行探索这些额外的 GAgent 工具：

- **TimeConverterGAgent** (`tools.time`)：在不同时区之间转换时间
  - 示例：获取不同时区的当前时间
  - 支持时区转换和格式化
  
- **其他 GAgent**：使用"列出所有 GAgent"功能发现更多专用代理

## 使用场景

这些功能对以下场景至关重要：

- 构建在运行时发现和使用 GAgent 的动态工作流
- 创建能够根据功能动态调用其他代理的 AI 代理
- 在分布式系统中实现服务发现模式
- 构建用于监控和管理 GAgent 的管理工具 