# Aevatar Workshop

欢迎来到 Aevatar Workshop！这是一个综合性的演示和学习环境，用于展示 Aevatar 框架的 GAgent 协作、事件驱动架构和 AI 集成能力。

## 🎯 什么是 Aevatar？

Aevatar 是一个强大的框架，用于构建使用代理（GAgents）的分布式、事件驱动系统。基于 Microsoft Orleans 构建，它支持：
- **分布式代理**：GAgent 可以跨多个节点运行
- **事件驱动通信**：代理通过事件进行协作
- **AI 集成**：原生支持具有工具调用能力的 AI 驱动代理
- **MCP 支持**：模型上下文协议集成，用于外部工具

---

## 📋 前置要求

- 安装 [.NET 9.0 SDK](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)
- Git 和 Unix-like shell（推荐 macOS/Linux）
- （可选）Azure OpenAI 或 OpenAI API 密钥（用于 AI 演示）

---

## 🚀 快速开始

```bash
git clone git@github.com:aevatarAI/aevatar-workshop.git
cd aevatar-workshop
./quickstart.sh
```

`quickstart.sh` 脚本将：
- 构建所有项目
- 启动 Host 服务（后端）- 日志在 `host.log`
- 启动 Client 服务（前端）- 日志在 `client.log`
- **自动打开 Web 界面** http://localhost:5000

> **提示：** 使用 `./shutdown.sh` 停止所有服务

---

## 🎮 可用演示

Workshop 包含两类演示：

### 基础演示

1. **事件处理器演示** 🎯
   - GAgent 事件处理的交互式演示
   - 展示代理如何定义和处理自定义事件
   - 显示实时事件流和统计信息
   - 完美理解事件驱动架构

2. **GAgent 服务演示** 🔧
   - 基本 GAgent 服务能力
   - 演示代理生命周期和状态管理
   - 展示代理间通信模式

3. **AI 工具调用演示** 🤖
   - AI 代理使用其他 GAgent 作为工具
   - 演示数学和时间转换代理集成
   - 展示如何构建 AI 驱动的工作流

4. **MCP 演示** 🔌
   - 模型上下文协议集成
   - 外部工具集成能力
   - 展示如何使用外部服务扩展代理

### 高级演示

1. **动态 AI MCP 集成** ⚡
   - 动态工具注册和发现
   - 复杂的 AI 编排模式
   - 实时工具适配

2. **PsiGAgent 演示** 🧠
   - 具有心理建模的高级 AI 代理
   - 复杂的推理和决策
   - AI 多代理协作

---

## ⚙️ 配置

### AI 配置（AI 演示必需）

编辑 `src/Aevatar.Workshop.Host/appsettings.json`：

```json
{
  "SystemLLMConfigs": {
    "OpenAI": {
      "ProviderEnum": "Azure",      // "Azure" 或 "OpenAI"
      "ModelIdEnum": "OpenAI",
      "ModelName": "gpt-4o",
      "Endpoint": "YOUR_ENDPOINT",   // Azure: https://xxx.openai.azure.com/
      "ApiKey": "YOUR_API_KEY"
    }
  }
}
```

---

## 🏗️ 项目结构

```
aevatar-workshop/
├── src/
│   ├── Aevatar.Workshop.Host/      # 后端 Orleans Silo
│   ├── Aevatar.Workshop.Client/    # 前端 Web API 和 UI
│   └── Aevatar.Workshop.GAgent/    # 自定义 GAgent 实现
├── test/                           # 单元和集成测试
├── docs/                          # 文档
│   └── demodesc/                  # 演示描述（中英文）
├── quickstart.sh                  # 启动脚本
├── shutdown.sh                    # 停止脚本
```

---

## 🛠️ 创建你自己的 GAgent

### 步骤 1：定义你的 GAgent

在 `src/Aevatar.Workshop.GAgent/GAgents/` 中创建新文件：

```csharp
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using System.ComponentModel;

[Description("我的自定义代理")]
[GAgent("mycustom", "workshop")]
public class MyCustomGAgent : GAgentBase<MyCustomState, MyCustomStateLogEvent>
{
    private readonly ILogger<MyCustomGAgent> _logger;
    
    public MyCustomGAgent(ILogger<MyCustomGAgent> logger)
    {
        _logger = logger;
    }
    
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("用于演示的自定义 GAgent");

    [EventHandler]
    public async Task HandleMyEventAsync(MyCustomEvent @event)
    {
        _logger.LogInformation("收到事件: {Message}", @event.Message);
        
        // 更新状态
        await RaiseStateEvent(new MyCustomStateLogEvent 
        { 
            Message = @event.Message 
        });
        
        // 发布响应事件
        await PublishAsync(new MyResponseEvent 
        { 
            Response = $"已处理: {@event.Message}" 
        });
    }
}
```

### 步骤 2：定义你的事件

```csharp
using Aevatar.Core.Abstractions;
using Orleans;

[GenerateSerializer]
public class MyCustomEvent : EventBase
{
    [Id(0)] public string Message { get; set; } = string.Empty;
}

[GenerateSerializer]
public class MyResponseEvent : EventBase
{
    [Id(0)] public string Response { get; set; } = string.Empty;
}
```

### 步骤 3：定义你的状态

```csharp
[GenerateSerializer]
public class MyCustomState : StateBase
{
    [Id(0)] public List<string> ProcessedMessages { get; set; } = new();
}

[GenerateSerializer]
public class MyCustomStateLogEvent : StateLogEventBase<MyCustomStateLogEvent>
{
    [Id(0)] public string Message { get; set; } = string.Empty;
    
    public override void Apply(MyCustomState state)
    {
        state.ProcessedMessages.Add(Message);
    }
}
```

### 步骤 4：使用你的 GAgent

如果放置在 Demo 命名空间中，你的自定义 GAgent 将自动出现在事件处理器演示中，这要归功于基于反射的发现系统。

---

## 🔍 关键特性

### 事件驱动架构
- 代理通过强类型事件通信
- 支持带属性的事件处理器
- 通过代理层次结构传播事件

### AI 集成
- 原生支持 AI 驱动的代理
- 工具调用能力
- MCP（模型上下文协议）支持

### 开发工具
- **GAgent 反射扩展**：自动发现代理及其能力
- **JsonConversionHelper**：Orleans 的统一 JSON 序列化
- **交互式 Web UI**：实时监控和交互

### 测试支持
- 综合单元测试示例
- 集成测试模式
- Orleans TestKit 集成

---

## 📚 学习路径

1. **从事件处理器演示开始** - 理解基本的事件驱动模式
2. **探索 GAgent 服务演示** - 了解代理生命周期
3. **尝试 AI 工具调用演示** - 查看 AI 集成的实际应用
4. **实验 MCP 演示** - 理解外部工具集成
5. **创建你自己的 GAgent** - 应用所学知识

---

## 🐛 故障排除

### 服务无法启动
- 检查端口 5000（Client）和 11111（Orleans）是否可用
- 确保已安装 .NET 9.0 SDK：`dotnet --version`
- 检查日志：`tail -f host.log` 和 `tail -f client.log`

### AI 演示无法工作
- 验证 `appsettings.json` 中的 API 密钥
- 检查端点 URL 是否正确
- 确保能够访问 AI 服务的网络连接

### 构建错误
- 运行 `dotnet restore` 恢复包
- 如果在开发模式下，确保所有子模块都已克隆
- 如果框架源代码不可用，切换到发布模式

---

## 🤝 贡献

我们欢迎贡献！请：
1. Fork 仓库
2. 创建功能分支
3. 为新功能添加测试
4. 提交 Pull Request

---

## 📄 许可证

本项目采用 MIT 许可证 - 详见 LICENSE 文件

---

## 🔗 资源

- [Aevatar 文档](https://docs.aevatar.ai)
- [Orleans 文档](https://docs.microsoft.com/zh-cn/dotnet/orleans/)
- [Discord 社区](https://discord.gg/aevatar)

---

祝你使用 Aevatar 编码愉快！🚀 