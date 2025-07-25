# Aevatar Workshop

Welcome to the Aevatar Workshop! This is a comprehensive demonstration and learning environment for the Aevatar framework, showcasing GAgent collaboration, event-driven architecture, and AI integration capabilities.

## 🎯 What is Aevatar?

Aevatar is a powerful framework for building distributed, event-driven systems using agents (GAgents). Built on Microsoft Orleans, it enables:
- **Distributed Agents**: GAgents that can run across multiple nodes
- **Event-Driven Communication**: Agents collaborate through events
- **AI Integration**: Native support for AI-powered agents with tool calling
- **MCP Support**: Model Context Protocol integration for external tools

---

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed
- Git and a Unix-like shell (macOS/Linux recommended)
- (Optional) Azure OpenAI or OpenAI API key for AI-powered demos

---

## 🚀 Quick Start

```bash
git clone git@github.com:aevatarAI/aevatar-workshop.git
cd aevatar-workshop
./quickstart.sh
```

The `quickstart.sh` script will:
- Build all projects
- Start the Host service (backend) - logs in `host.log`
- Start the Client service (frontend) - logs in `client.log`
- Display URL for the web interface: http://localhost:5000

> **Tip:** To stop all services, use `./shutdown.sh`

---

## 🎮 Available Demos

The workshop includes two categories of demos:

### Basic Demos

1. **Event Handler Demo** 🎯
   - Interactive demonstration of GAgent event handling
   - Shows how agents define and handle custom events
   - Displays real-time event flow and statistics
   - Perfect for understanding event-driven architecture

2. **GAgent Service Demo** 🔧
   - Basic GAgent service capabilities
   - Demonstrates agent lifecycle and state management
   - Shows inter-agent communication patterns

3. **AI Tool Calling Demo** 🤖
   - AI agents using other GAgents as tools
   - Demonstrates Math and TimeConverter agent integration
   - Shows how to build AI-powered workflows

4. **MCP Demo** 🔌
   - Model Context Protocol integration
   - External tool integration capabilities
   - Shows how to extend agents with external services

### Advanced Demos

1. **Dynamic AI MCP Integration** ⚡
   - Dynamic tool registration and discovery
   - Complex AI orchestration patterns
   - Real-time tool adaptation

2. **PsiGAgent Demo** 🧠
   - Advanced AI agent with psychological modeling
   - Complex reasoning and decision-making
   - Multi-agent collaboration with AI

---

## ⚙️ Configuration

### AI Configuration (Required for AI Demos)

Edit `src/Aevatar.Workshop.Host/appsettings.json`:

```json
{
  "SystemLLMConfigs": {
    "OpenAI": {
      "ProviderEnum": "Azure",      // "Azure" or "OpenAI"
      "ModelIdEnum": "OpenAI",
      "ModelName": "gpt-4o",
      "Endpoint": "YOUR_ENDPOINT",   // Azure: https://xxx.openai.azure.com/
      "ApiKey": "YOUR_API_KEY"
    }
  }
}
```

---

## 🏗️ Project Structure

```
aevatar-workshop/
├── src/
│   ├── Aevatar.Workshop.Host/      # Backend Orleans Silo
│   ├── Aevatar.Workshop.Client/    # Frontend Web API & UI
│   └── Aevatar.Workshop.GAgent/    # Custom GAgent implementations
├── test/                           # Unit and integration tests
├── docs/                          # Documentation
│   └── demodesc/                  # Demo descriptions (EN/ZH)
├── quickstart.sh                  # Start script
├── shutdown.sh                    # Stop script
```

---

## 🛠️ Creating Your Own GAgent

### Step 1: Define Your GAgent

Create a new file in `src/Aevatar.Workshop.GAgent/GAgents/`:

```csharp
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using System.ComponentModel;

[GAgent("mycustom", "workshop")]
public class MyCustomGAgent : GAgentBase<MyCustomState, MyCustomStateLogEvent>
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("My custom GAgent for demonstration");

    [EventHandler]
    public async Task HandleMyEventAsync(MyCustomEvent @event)
    {
        Logger.LogInformation("Received event: {Message}", @event.Message);
        
        // Update state
        await RaiseStateEvent(new MyCustomStateLogEvent 
        { 
            Message = @event.Message 
        });
        
        // Publish response event
        await PublishAsync(new MyResponseEvent 
        { 
            Response = $"Processed: {@event.Message}" 
        });
    }
}
```

### Step 2: Define Your Events

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

### Step 3: Define Your State

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

### Step 4: Use Your GAgent

Your custom GAgent will automatically appear in the Event Handler Demo if placed in the Demo namespace, thanks to the reflection-based discovery system.

---

## 🔍 Key Features

### Event-Driven Architecture
- Agents communicate through strongly-typed events
- Support for event handlers with attributes
- Event propagation through agent hierarchies

### AI Integration
- Native support for AI-powered agents
- Tool calling capabilities
- MCP (Model Context Protocol) support

### Development Tools
- **GAgent Reflection Extensions**: Automatic discovery of agents and their capabilities
- **JsonConversionHelper**: Unified JSON serialization for Orleans
- **Interactive Web UI**: Real-time monitoring and interaction

### Testing Support
- Comprehensive unit test examples
- Integration test patterns
- Orleans TestKit integration

---

## 📚 Learning Path

1. **Start with Event Handler Demo** - Understand basic event-driven patterns
2. **Explore GAgent Service Demo** - Learn about agent lifecycle
3. **Try AI Tool Calling Demo** - See AI integration in action
4. **Experiment with MCP Demo** - Understand external tool integration
5. **Create your own GAgent** - Apply what you've learned

---

## 🐛 Troubleshooting

### Services won't start
- Check if ports 5000 (Client) and 11111 (Orleans) are available
- Ensure .NET 9.0 SDK is installed: `dotnet --version`
- Check logs: `tail -f host.log` and `tail -f client.log`

### AI demos not working
- Verify API keys in `appsettings.json`
- Check if endpoint URLs are correct
- Ensure network connectivity to AI services

### Build errors
- Run `dotnet restore` to restore packages
- If in dev mode, ensure all submodules are cloned
- Switch to release mode if framework source is not available

---

## 🤝 Contributing

We welcome contributions! Please:
1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Submit a pull request

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🔗 Resources

- [Aevatar Documentation](https://docs.aevatar.ai)
- [Orleans Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/)
- [Discord Community](https://discord.gg/aevatar)

---

Happy coding with Aevatar! 🚀 