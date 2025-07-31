# Aevatar Workshop

[🇨🇳 中文版](README.zh.md) | English

Welcome to the **Aevatar Workshop** - your gateway to building intelligent multi-agent systems! This comprehensive learning environment demonstrates the power of GAgent collaboration, event-driven architecture, and AI integration.

## 🎯 What is Aevatar?

**Aevatar** is a cutting-edge framework for building distributed, event-driven systems using intelligent agents (GAgents). Built on Microsoft Orleans, it empowers developers to create scalable, high-concurrency multi-agent applications with:

- 🤖 **Intelligent Agents (GAgents)**: Autonomous entities that think, remember, and collaborate
- ⚡ **Event-Driven Communication**: Seamless agent interaction through Orleans Streaming
- 🧠 **AI Integration**: Native support for LLM-powered agents with tool calling
- 🌐 **Distributed Architecture**: Auto-scaling across multiple nodes
- 📊 **Event Sourcing**: Reliable state management and complete audit trails

---

## 🚀 Quick Start

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Git and Unix-like shell (macOS/Linux recommended)
- (Optional) OpenAI or Azure OpenAI API key for AI demos

### Installation & Launch
```bash
git clone git@github.com:aevatarAI/aevatar-workshop.git
cd aevatar-workshop
./quickstart.sh
```

The workshop will start automatically:
- 🖥️ **Web Interface**: http://localhost:5000
- 📝 **Backend Logs**: `host.log`
- 🌐 **Frontend Logs**: `client.log`
- 🛑 **Stop Services**: `./shutdown.sh`

---

## 📚 Learning Journey

### 🎓 Start Here: Aevatar Basics

**Perfect for newcomers!** Begin with our comprehensive foundation course that covers:

- **GAgent Fundamentals**: Understanding intelligent agents and their capabilities
- **Orleans Virtual Actor Model**: How distributed agents work at scale
- **Event-Driven Architecture**: Agent communication patterns and best practices  
- **State Management**: Event sourcing and persistent agent memory
- **Hands-on Examples**: Interactive code samples and exercises

**👆 Click "Aevatar Basics" in the workshop interface to start learning!**

### 🏠 Flagship Demo: Smart Home Experience

**See Aevatar in action!** Our Smart Home Demo showcases a complete multi-agent system:

#### Key Features
- 🗣️ **Natural Language Control**: Speak to your smart home in English or Chinese
- 🏡 **Multi-Device Management**: Lights, thermostat, security, and curtains
- 🤖 **AI-Powered Coordination**: Central AI agent orchestrates all devices
- 📱 **Real-time Updates**: Instant feedback and synchronized states
- 🌍 **Multi-language Support**: Complete internationalization

#### What You'll Experience
- **Voice Commands**: "Turn on the living room lights", "Set temperature to 22°"
- **Agent Collaboration**: Watch GAgents communicate through real-time event flows
- **Manual Controls**: Direct device interaction alongside AI commands
- **Event Monitoring**: Live visualization of agent interactions

**👆 Click "Smart Home Demo" to experience the future of smart home control!**

---

## 🎮 Additional Demos

Explore advanced capabilities after mastering the basics:

### Core Concepts
- **GAgent Service Demo**: Agent lifecycle and service patterns

### AI Integration  
- **AI Tool Calling Demo**: How AI agents use other agents as tools
- **MCP Demo**: Model Context Protocol integration for external tools

### Advanced Topics
- **Dynamic AI MCP Integration**: Real-time tool discovery and adaptation
- **PsiGAgent Demo**: Advanced AI agents with psychological modeling

---

## ⚙️ AI Configuration (Recommended)

To unlock AI-powered demos, configure your LLM provider in `src/Aevatar.Workshop.Host/appsettings.json`:

```json
{
  "SystemLLMConfigs": {
    "OpenAI": {
      "ProviderEnum": "OpenAI",        // or "Azure"
      "ModelIdEnum": "OpenAI", 
      "ModelName": "gpt-4o",
      "Endpoint": "https://api.openai.com/v1/",  // or Azure endpoint
      "ApiKey": "YOUR_API_KEY_HERE"
    }
  }
}
```

> **Note**: The Smart Home Demo works with manual controls even without AI configuration!

---

## 🏗️ Project Architecture

```
aevatar-workshop/
├── src/
│   ├── Aevatar.Workshop.Host/      # Orleans Silo (Backend)
│   ├── Aevatar.Workshop.Client/    # Web API & UI (Frontend)  
│   └── Aevatar.Workshop.GAgent/    # Custom GAgent implementations
├── test/                           # Comprehensive test suite
├── docs/                           # Documentation and guides
└── scripts/                        # Automation scripts
```

---

## 🛠️ Building Your First GAgent

After completing the basics tutorial, try creating your own GAgent:

```csharp
[GAgent("myagent", "workshop")]
public class MyGAgent : GAgentBase<MyState, MyStateLogEvent>, IMyGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("My first intelligent agent");

    [EventHandler]
    public async Task HandleMyEventAsync(MyEvent @event)
    {
        // Process the event
        Logger.LogInformation("Received: {Message}", @event.Message);
        
        // Update state through events
        RaiseEvent(new MyStateLogEvent { Data = @event.Message });
        await ConfirmEvents();
        
        // Publish response
        await PublishAsync(new MyResponseEvent 
        { 
            Response = $"Processed: {@event.Message}" 
        });
    }
}
```

---

## 🐛 Troubleshooting

### Common Issues
- **Services won't start**: Check ports 5000 and 11111 are free
- **Build errors**: Run `dotnet restore` and ensure .NET 9.0 is installed
- **AI demos not working**: Verify API keys in configuration

### Getting Help
- Check logs: `tail -f host.log` and `tail -f client.log`
- Review documentation in the workshop interface
- Visit our [Discord Community](https://discord.gg/aevatar)

---

## 🌟 Why Choose Aevatar?

✅ **Developer Friendly**: Familiar C# and .NET ecosystem  
✅ **Production Ready**: Built on proven Orleans technology  
✅ **AI Native**: Seamless LLM integration and tool calling  
✅ **Highly Scalable**: Automatic load balancing and distribution  
✅ **Event Sourcing**: Complete auditability and state recovery  

---

## 🔗 Resources

- 📖 [Aevatar Documentation](https://docs.aevatar.ai)
- 🛠️ [GAgent Development Guide](docs/gagent-development-guide.md)
- 🏛️ [Orleans Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/)
- 💬 [Discord Community](https://discord.gg/aevatar)
- 🐙 [GitHub Repository](https://github.com/aevatarAI/aevatar-station)

---

**Ready to build the future with intelligent agents?** 

🎯 Start with **Aevatar Basics** → Experience the **Smart Home Demo** → Build your own GAgent!

Happy coding! 🚀 