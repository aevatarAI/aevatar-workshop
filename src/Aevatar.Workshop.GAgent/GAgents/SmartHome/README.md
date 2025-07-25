# Smart Home Demo - Aevatar Multi-Agent System

## 🎯 Overview

This Smart Home Demo showcases the power of Aevatar's multi-agent system through a practical, relatable example. It demonstrates how multiple autonomous agents can collaborate through event-driven communication to create an intelligent home automation system.

## 🏗️ Architecture

### Core Components

#### 1. **HomeAIGAgent** (AI Brain) 🤖
- **Base Class**: `AIGAgentBase` (AI-enabled)
- **Role**: Natural language interface
- **Capabilities**:
  - Understands commands like "turn on the lights"
  - Converts natural language to structured events
  - Uses Semantic Kernel for NLP
  - Provides conversational feedback

#### 2. **LightGAgent** (Device) 💡
- **Base Class**: `GAgentBase`
- **State**: On/Off, Brightness (0-100%)
- **Events Handled**:
  - `TurnOnLightCommand`
  - `TurnOffLightCommand`
  - `SetBrightnessCommand`
- **Events Published**:
  - `LightStateChangedEvent`

#### 3. **ThermostatGAgent** (Device) 🌡️
- **Base Class**: `GAgentBase`
- **State**: Current/Target Temperature, Mode
- **Events Handled**:
  - `SetTemperatureCommand`
  - `ChangeModeCommand`
- **Events Published**:
  - `TemperatureChangedEvent`
  - `ModeChangedEvent`

#### 4. **SecurityGAgent** (Device) 🔒
- **Base Class**: `GAgentBase`
- **State**: Armed/Disarmed, Motion Detection
- **Events Handled**:
  - `ArmSecurityCommand`
  - `DisarmSecurityCommand`
- **Events Published**:
  - `SecurityStateChangedEvent`
  - `MotionDetectedEvent`

#### 5. **HomeCoordinatorGAgent** (Coordinator) 🎯
- **Base Class**: `GAgentBase`
- **Role**: Central orchestrator
- **Capabilities**:
  - Routes AI commands to appropriate devices
  - Implements scenes (e.g., "goodnight", "away")
  - Manages device registry

## 🌟 Key Features Demonstrated

### 1. **Event-Driven Architecture**
```
User Command → AI Agent → Intent Event → Coordinator → Device Command → Device → State Change Event
```

### 2. **Multi-Agent Collaboration**
- Each device is an independent agent
- Agents communicate through events, not direct calls
- Loose coupling enables flexibility

### 3. **AI Integration**
- Natural language processing without complexity
- Semantic Kernel integration for intelligent command parsing
- Tool/Function calling for device control

### 4. **State Management**
- Event sourcing for complete history
- Orleans-based persistence
- Each agent maintains its own state

## 🚀 Usage Examples

### Basic Setup
```csharp
// Get agent factory
var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();

// Create agents
var aiAgent = await gAgentFactory.GetGAgentAsync<IHomeAIGAgent>("home-ai");
var coordinator = await gAgentFactory.GetGAgentAsync<IHomeCoordinatorGAgent>("coordinator");
var light = await gAgentFactory.GetGAgentAsync<ILightGAgent>("living-room");

// Register devices with coordinator
await coordinator.RegisterAsync(light);
await coordinator.RegisterAsync(aiAgent);

// Initialize AI
await aiAgent.InitializeAsync("OpenAI");
```

### Natural Language Control
```csharp
// User commands
var response = await aiAgent.ProcessCommandAsync("Turn on the living room lights");
// AI: "I've sent the command to turn on the light in the living room."

response = await aiAgent.ProcessCommandAsync("Set temperature to 22 degrees");
// AI: "I've sent the command to set the thermostat temperature."

response = await aiAgent.ProcessCommandAsync("Goodnight");
// AI: "I've activated the goodnight scene for you."
```

### Direct Event Control
```csharp
// Direct device control via events
await coordinator.PublishEventAsync(new TurnOnLightCommand { LightId = "bedroom" });

// Scene execution
await coordinator.ExecuteSceneAsync("away");
```

## 📊 Event Flow Example

### Scenario: "Turn on the lights"
1. User says: "Turn on the lights"
2. HomeAIGAgent processes with Semantic Kernel
3. AI publishes `DeviceControlIntentEvent`
4. HomeCoordinatorGAgent receives intent
5. Coordinator publishes `TurnOnLightCommand`
6. LightGAgent receives command
7. Light updates state (IsOn = true)
8. Light publishes `LightStateChangedEvent`
9. All subscribers notified of state change

## 🧪 Testing

The demo includes comprehensive tests demonstrating:
- Basic device control
- Scene execution
- AI natural language processing
- Event flow visualization

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~SmartHomeDemo"
```

## 🎓 Learning Points

### 1. **Agent Independence**
Each device operates independently. If the thermostat crashes, lights continue working.

### 2. **Event Sourcing Benefits**
Complete history of all home activities. Can replay events to see "what happened when".

### 3. **Extensibility**
Adding new devices is simple - just create a new GAgent:
```csharp
[GAgent("coffeeMaker", "smarthome")]
public class CoffeeMakerGAgent : GAgentBase<CoffeeMakerState, CoffeeMakerStateLogEvent>
{
    // Implementation
}
```

### 4. **No Direct Dependencies**
Devices don't know about each other. All communication through events.

## 🔧 Configuration

### AI Configuration
Configure in `appsettings.json`:
```json
{
  "SystemLLMConfig": {
    "OpenAI": {
      "ApiKey": "your-api-key",
      "Model": "gpt-4"
    }
  }
}
```

### Scene Configuration
Scenes are defined in `HomeCoordinatorGAgent`:
- **goodnight**: Lights off, temperature 20°C, security armed
- **goodmorning**: Lights on, temperature 22°C, security disarmed
- **away**: Lights off, temperature 18°C, security armed

## 📈 Future Enhancements

1. **More Devices**: Smart locks, cameras, appliances
2. **Advanced Scenes**: Time-based, sensor-triggered
3. **Mobile App Integration**: Real-time UI updates
4. **Voice Assistant**: Direct voice control
5. **Machine Learning**: Learn user patterns

## 🏁 Conclusion

This Smart Home Demo illustrates how Aevatar's multi-agent system makes complex coordination simple. Each component is independent yet works seamlessly together through events, creating a robust and extensible smart home system.

The power lies in:
- **Simplicity**: Each agent has a single responsibility
- **Flexibility**: Easy to add/modify agents
- **Reliability**: Failure isolation
- **Intelligence**: AI integration without complexity 