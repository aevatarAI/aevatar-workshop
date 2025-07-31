# Aevatar Workshop

欢迎来到 **Aevatar Workshop** - 您构建智能多智能体系统的入门之路！这个综合性学习环境展示了GAgent协作、事件驱动架构和AI集成的强大功能。

## 🎯 什么是 Aevatar？

**Aevatar** 是一个前沿的框架，用于构建基于智能代理（GAgents）的分布式事件驱动系统。基于Microsoft Orleans构建，它使开发者能够创建可扩展的高并发多智能体应用：

- 🤖 **智能代理 (GAgents)**：能够思考、记忆和协作的自主实体
- ⚡ **事件驱动通信**：通过Orleans Streaming实现无缝的智能体交互
- 🧠 **AI 集成**：原生支持具有工具调用能力的LLM驱动智能体
- 🌐 **分布式架构**：跨多个节点自动扩展
- 📊 **事件溯源**：可靠的状态管理和完整的审计跟踪

---

## 🚀 快速开始

### 前置要求
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Git 和类Unix shell环境（推荐macOS/Linux）
- （可选）OpenAI 或 Azure OpenAI API密钥用于AI演示

### 安装与启动
```bash
git clone git@github.com:aevatarAI/aevatar-workshop.git
cd aevatar-workshop
./quickstart.sh
```

Workshop将自动启动：
- 🖥️ **Web界面**：http://localhost:5000
- 📝 **后端日志**：`host.log`
- 🌐 **前端日志**：`client.log`
- 🛑 **停止服务**：`./shutdown.sh`

---

## 📚 学习之旅

### 🎓 从这里开始：Aevatar 基础

**新手的完美起点！** 从我们的综合基础课程开始，涵盖：

- **GAgent 基础知识**：理解智能代理及其能力
- **Orleans 虚拟Actor模型**：分布式智能体如何大规模工作
- **事件驱动架构**：智能体通信模式和最佳实践
- **状态管理**：事件溯源和持久化智能体记忆
- **实践示例**：交互式代码样例和练习

**👆 在workshop界面中点击"Aevatar 基础"开始学习！**

### 🏠 旗舰演示：智能家居体验

**看 Aevatar 实际应用！** 我们的智能家居演示展示了完整的多智能体系统：

#### 主要特性
- 🗣️ **自然语言控制**：用中文或英文与智能家居对话
- 🏡 **多设备管理**：灯光、温控器、安防和窗帘
- 🤖 **AI驱动协调**：中央AI智能体协调所有设备
- 📱 **实时更新**：即时反馈和同步状态
- 🌍 **多语言支持**：完整的国际化

#### 您将体验到
- **语音命令**："打开客厅的灯"，"把温度设置为22度"
- **智能体协作**：通过实时事件流观察GAgent通信
- **手动控制**：与AI命令并行的直接设备交互
- **事件监控**：智能体交互的实时可视化

**👆 点击"智能家居演示"体验智能家居控制的未来！**

---

## 🎮 其他演示

掌握基础知识后，探索高级功能：

### 核心概念
- **GAgent 服务演示**：智能体生命周期和服务模式

### AI 集成
- **AI 工具调用演示**：AI智能体如何使用其他智能体作为工具
- **MCP 演示**：模型上下文协议与外部工具集成

### 高级主题
- **动态 AI MCP 集成**：实时工具发现和适应
- **PsiGAgent 演示**：具有心理建模的高级AI智能体

---

## ⚙️ AI 配置（推荐）

要解锁AI驱动的演示，请在 `src/Aevatar.Workshop.Host/appsettings.json` 中配置您的LLM提供商：

```json
{
  "SystemLLMConfigs": {
    "OpenAI": {
      "ProviderEnum": "OpenAI",        // 或 "Azure"
      "ModelIdEnum": "OpenAI", 
      "ModelName": "gpt-4o",
      "Endpoint": "https://api.openai.com/v1/",  // 或Azure端点
      "ApiKey": "您的API密钥"
    }
  }
}
```

> **注意**：即使没有AI配置，智能家居演示也可以通过手动控制正常工作！

---

## 🏗️ 项目架构

```
aevatar-workshop/
├── src/
│   ├── Aevatar.Workshop.Host/      # Orleans Silo（后端）
│   ├── Aevatar.Workshop.Client/    # Web API & UI（前端）
│   └── Aevatar.Workshop.GAgent/    # 自定义GAgent实现
├── test/                           # 综合测试套件
├── docs/                           # 文档和指南
└── scripts/                        # 自动化脚本
```

---

## 🛠️ 构建您的第一个 GAgent

完成基础教程后，尝试创建自己的GAgent：

```csharp
[GAgent("myagent", "workshop")]
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("我的第一个智能代理");

    [EventHandler]
    public async Task HandleMyEventAsync(MyEvent @event)
    {
        // 处理事件
        Logger.LogInformation("收到消息：{Message}", @event.Message);
        
        // 通过事件更新状态
        RaiseEvent(new MyStateLogEvent { Data = @event.Message });
        await ConfirmEvents();
        
        // 发布响应
        await PublishAsync(new MyResponseEvent 
        { 
            Response = $"已处理：{@event.Message}" 
        });
    }
}
```

---

## 🐛 故障排除

### 常见问题
- **服务无法启动**：检查端口5000和11111是否被占用
- **构建错误**：运行 `dotnet restore` 并确保安装了.NET 9.0
- **AI演示无法工作**：验证配置中的API密钥

### 获取帮助
- 查看日志：`tail -f host.log` 和 `tail -f client.log`
- 查阅workshop界面中的文档
- 访问我们的 [Discord社区](https://discord.gg/aevatar)

---

## 🌟 为什么选择 Aevatar？

✅ **开发者友好**：熟悉的C#和.NET生态系统  
✅ **生产就绪**：基于成熟的Orleans技术构建  
✅ **AI原生**：无缝LLM集成和工具调用  
✅ **高度可扩展**：自动负载均衡和分发  
✅ **事件溯源**：完整的可审计性和状态恢复  

---

## 🔗 资源

- 📖 [Aevatar 文档](https://docs.aevatar.ai)
- 🛠️ [GAgent 开发指南](docs/gagent-development-guide.zh.md)
- 🏛️ [Orleans 文档](https://docs.microsoft.com/en-us/dotnet/orleans/)
- 💬 [Discord 社区](https://discord.gg/aevatar)
- 🐙 [GitHub 仓库](https://github.com/aevatarAI/aevatar-station)

---

**准备好用智能代理构建未来了吗？**

🎯 从 **Aevatar 基础** 开始 → 体验 **智能家居演示** → 构建您自己的GAgent！

编程愉快！🚀 