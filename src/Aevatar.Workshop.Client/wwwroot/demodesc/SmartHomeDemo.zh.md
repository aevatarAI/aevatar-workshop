# 智能家居演示

本演示展示了 AI GAgent 系统通过自然语言交互管理智能家居设备的强大能力。

## 概述

智能家居演示整合了多个智能代理（GAgents）来创建一个统一的智能家居体验。每个设备由其自己的 GAgent 表示，中央 AI GAgent 使用自然语言处理协调所有交互。

## 系统架构

### 核心组件

1. **HomeAIGAgent**: 处理自然语言命令的中央 AI 协调器
2. **LightGAgent**: 管理智能照明系统
3. **ThermostatGAgent**: 控制供暖和制冷系统
4. **SecurityGAgent**: 处理安防系统操作
5. **CurtainGAgent**: 管理自动化窗帘

### 技术栈

- **Orleans 框架**: GAgent 实现的分布式 Actor 模型
- **AI 集成**: 大语言模型（LLM）集成，用于自然语言理解
- **事件驱动架构**: GAgents 之间的实时通信
- **多语言支持**: 完整的国际化（中英文）

## 功能特点

### 自然语言控制
- 处理中文和英文命令
- 理解复杂的上下文请求
- 支持对话式交互

### 设备管理
- **智能灯光**: 开关控制、亮度调节（0-100%）
- **恒温器**: 温度设置、模式切换（制热/制冷/自动/关闭）
- **安防系统**: 布防/撤防功能、运动检测警报
- **智能窗帘**: 位置控制（0-100% 开启/关闭）

### 实时同步
- 即时设备状态更新
- 命令执行的实时反馈
- 所有界面的状态同步

### 多语言界面
- 从浏览器设置自动检测语言
- 手动语言切换
- 本地化错误消息和响应

## 工作原理

1. **初始化**: 系统创建并注册所有 GAgents
2. **事件订阅**: AI GAgent 订阅来自所有设备 GAgents 的事件
3. **命令处理**: AI GAgent 处理自然语言命令
4. **工具调用**: AI GAgent 调用适当的设备 GAgent 方法
5. **状态更新**: 通过事件溯源更新设备状态
6. **响应生成**: 以选定语言生成用户友好的响应

## 示例命令

### 中文命令
- "打开客厅的灯"
- "把温度设置为22度"
- "启动安防系统"
- "把窗帘关到一半"
- "把灯光调暗到30%"

### 英文命令
- "Turn on the living room lights"
- "Set temperature to 22 degrees"
- "Arm the security system"
- "Close the curtains halfway"
- "Dim the lights to 30%"

## 技术实现

### GAgent 通信
```csharp
// 示例：AI GAgent 调用 Light GAgent
var lightGAgent = await GAgentFactory.GetGAgentAsync<ILightGAgent>(lightId);
await lightGAgent.TurnOnAsync();
```

### 事件溯源
```csharp
// 通过事件跟踪状态更改
RaiseEvent(new LightTurnedOnEvent { Brightness = 100 });
await ConfirmEvents();
```

### 国际化
```csharp
// 基于用户语言的本地化响应
var message = _localizationService.GetText("light_turned_on", userLanguage);
```

## 快速开始

1. 点击"打开智能家居演示"启动独立界面
2. 点击"初始化系统"初始化系统
3. 在命令输入区域尝试自然语言命令
4. 使用手动控制进行直接设备交互
5. 使用右上角的语言选择器切换语言

## API 端点

演示提供用于外部集成的 RESTful API：

- `POST /api/smarthome/initialize` - 初始化智能家居系统
- `POST /api/smarthome/command` - 处理自然语言命令
- `GET /api/smarthome/status` - 获取当前设备状态
- `POST /api/smarthome/device/{type}` - 直接设备控制
- `GET /api/localization/current-language` - 获取用户首选语言

## 特色亮点

### 智能理解
系统能够理解复杂的语义，例如：
- "把所有灯都关掉并启动安防"
- "如果温度太热就打开空调"
- "睡觉时间到了，帮我关灯拉窗帘"

### 上下文感知
AI 可以维护对话上下文：
- 用户："把灯打开"
- AI："好的，已经为您打开了客厅的灯"
- 用户："调亮一点"
- AI："已将客厅灯光亮度调整到80%"

### 场景模式
支持复合场景控制：
- "回家模式"：开灯、调温、关闭安防
- "离家模式"：关灯、启动安防、关窗帘
- "睡眠模式"：调暗灯光、降低温度、关窗帘

## 架构优势

- **可扩展性**: 通过创建新的 GAgent 轻松添加新设备
- **可靠性**: 故障隔离 - 每个代理独立运行
- **可维护性**: 每个代理的职责清晰明确
- **可扩展性**: 添加新功能无需修改现有代码

本演示展示了 AI GAgents 如何为最终用户创建智能、响应迅速且用户友好的智能家居体验，同时保持最小的技术复杂性。 