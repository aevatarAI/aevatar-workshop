# Smart Home Demo - Aevatar Multi-Agent System

## 🎯 Overview

This Smart Home Demo showcases the power of Aevatar's multi-agent system through a practical, relatable example. It demonstrates how multiple autonomous agents can collaborate through event-driven communication to create an intelligent home automation system with natural language processing capabilities.

## 🏗️ System Architecture

### Core Components

#### 1. **HomeAIGAgent** (AI Brain) 🤖
- **Base Class**: `AIGAgentBase` (AI-enabled with Semantic Kernel integration)
- **Role**: Natural language interface and central intelligence
- **Capabilities**:
  - Understands commands like "turn on the lights", "set temperature to 22 degrees"
  - Converts natural language to structured device commands
  - Supports scene automation (Good Morning, Good Night, Movie Mode)
  - Provides conversational feedback and status updates
  - Integrates with multiple LLM providers (OpenAI, Azure OpenAI, etc.)
- **Key Methods**:
  - `InitializeAsync(string llmSystem)` - Initialize with LLM configuration
  - `ProcessCommandAsync(string userInput)` - Process natural language commands
  - `GetChatHistoryAsync()` - Retrieve conversation history
  - `IsInitializedAsync()` - Check initialization status

#### 2. **LightGAgent** (Smart Lighting) 💡
- **Base Class**: `GAgentBase<LightState, LightStateLogEvent, EventBase, LightConfiguration>`
- **State**: On/Off status, Brightness (0-100%), Location, Last change timestamp
- **Configuration**: Requires `LightConfiguration` with LightId and Location
- **Commands Handled**:
  - `TurnOnLightCommand` - Turn light on
  - `TurnOffLightCommand` - Turn light off
  - `SetBrightnessCommand` - Adjust brightness (0-100%)
- **Events Published**:
  - `LightStateChangedEvent` - Notifies state changes to other agents
- **Key Methods**:
  - `TurnOnAsync()` / `TurnOffAsync()` - Basic on/off control
  - `SetBrightnessAsync(int brightness)` - Brightness control with validation
  - `IsOnAsync()` / `GetBrightnessAsync()` - State queries

#### 3. **ThermostatGAgent** (Climate Control) 🌡️
- **Base Class**: `GAgentBase<ThermostatState, ThermostatStateLogEvent>`
- **State**: Current/Target Temperature, Mode (Auto/Heating/Cooling/Off), Location
- **Features**:
  - Automatic temperature simulation with realistic fluctuations
  - Support for different operating modes
  - Temperature validation and range checking
- **Commands Handled**:
  - `SetTemperatureCommand` - Set target temperature
  - `ChangeModeCommand` - Change operating mode
- **Events Published**:
  - `TemperatureChangedEvent` - Temperature updates
  - `ModeChangedEvent` - Mode changes
- **Key Methods**:
  - `SetTargetTemperatureAsync(double temperature)` - Set target temperature
  - `GetCurrentTemperatureAsync()` / `GetTargetTemperatureAsync()` - Temperature queries
  - `GetModeAsync()` - Current mode query
  - `SimulateTemperatureChangeAsync()` - Simulate realistic temperature changes

#### 4. **SecurityGAgent** (Security System) 🔒
- **Base Class**: `GAgentBase<SecurityState, SecurityStateLogEvent>`
- **State**: Armed/Disarmed status, Motion detection history, Location tracking
- **Features**:
  - Motion detection simulation with random events
  - Location-based motion tracking
  - Motion detection count and history
- **Commands Handled**:
  - `ArmSecurityCommand` - Arm the security system
  - `DisarmSecurityCommand` - Disarm the security system
- **Events Published**:
  - `SecurityStateChangedEvent` - Security status changes
  - `MotionDetectedEvent` - Motion detection alerts
- **Key Methods**:
  - `ArmAsync()` / `DisarmAsync()` - Security control
  - `IsArmedAsync()` - Armed status query
  - `GetLastMotionTimeAsync()` / `GetLastMotionLocationAsync()` - Motion history
  - `SimulateMotionAsync(string location)` - Manual motion simulation

#### 5. **CurtainGAgent** (Window Coverings) 🪟
- **Base Class**: `GAgentBase<CurtainState, CurtainStateLogEvent>`
- **State**: Current/Target Position (0-100%), Movement status, Operation timestamp
- **Features**:
  - Realistic movement simulation with configurable speed
  - Position validation and clamping
  - Movement interruption support
- **Commands Handled**:
  - `OpenCurtainCommand` - Fully open (100%)
  - `CloseCurtainCommand` - Fully close (0%)
  - `SetCurtainPositionCommand` - Set specific position
  - `StopCurtainCommand` - Stop current movement
- **Events Published**:
  - `CurtainStateChangedEvent` - Position and movement updates
- **Key Methods**:
  - `SetPositionAsync(int position)` - Set curtain position with movement simulation
  - `OpenAsync()` / `CloseAsync()` - Convenience methods for full open/close
  - `StopAsync()` - Stop movement immediately

## 🌟 Key Features Demonstrated

### 1. **Event-Driven Architecture**
```
User Command → AI Agent → Intent Recognition → Device Command → Device Action → State Change Event → System Update
```

### 2. **Multi-Agent Collaboration**
- Each device is an independent, stateful agent
- Agents communicate through typed events, not direct method calls
- Decoupled architecture allows for easy addition of new device types
- Central AI coordinator manages complex multi-device scenarios

### 3. **Natural Language Processing**
- **Semantic Kernel Integration**: Advanced NLP capabilities
- **Intent Recognition**: Parse complex commands like "dim the lights and set temperature to 22"
- **Context Awareness**: Understand device relationships and dependencies
- **Conversational Interface**: Natural dialogue with users

### 4. **Scene Automation**
Pre-configured scenarios for common use cases:

#### **Good Morning Scene**
- Lights: Turn on at 100% brightness
- Temperature: Set to comfortable 22°C
- Security: Disarm system
- Curtains: Fully open for natural light

#### **Good Night Scene**
- Lights: Turn off all lights
- Temperature: Lower to 20°C for sleep
- Security: Arm full security system
- Curtains: Fully close for privacy

#### **Movie Mode Scene**
- Lights: Dim to 20% for ambient lighting
- Temperature: Set to comfortable 21°C
- Security: Disarm to prevent interruptions
- Curtains: Close to block external light

### 5. **State Persistence & Event Sourcing**
- **Orleans Event Sourcing**: Complete audit trail of all device changes
- **State Recovery**: Agents can recover their full state from event logs
- **Timestamp Tracking**: All state changes include precise timing information
- **Configuration Support**: Devices can be configured with specific parameters

### 6. **Realistic Device Simulation**
- **Temperature Fluctuation**: Thermostat simulates realistic temperature changes
- **Motion Detection**: Security system generates random motion events when armed
- **Curtain Movement**: Gradual position changes with configurable speed
- **Light Persistence**: Brightness settings maintained across on/off cycles

## 🚀 Getting Started

### Basic Setup
```csharp
// 1. Create and configure devices
var aiAgent = await gAgentFactory.GetGAgentAsync<IHomeAIGAgent>(AI_AGENT_ID);

var lightConfig = new LightConfiguration 
{ 
    LightId = "living-room-light", 
    Location = "Living Room" 
};
var light = await gAgentFactory.GetGAgentAsync<ILightGAgent>(LIGHT_ID, lightConfig);

var thermostat = await gAgentFactory.GetGAgentAsync<IThermostatGAgent>(THERMOSTAT_ID);
var security = await gAgentFactory.GetGAgentAsync<ISecurityGAgent>(SECURITY_ID);
var curtain = await gAgentFactory.GetGAgentAsync<ICurtainGAgent>(CURTAIN_ID);

// 2. Register devices with AI coordinator
await aiAgent.RegisterAsync(light);
await aiAgent.RegisterAsync(thermostat);
await aiAgent.RegisterAsync(security);
await aiAgent.RegisterAsync(curtain);

// 3. Initialize AI with LLM system
await aiAgent.InitializeAsync("OpenAI");

// 4. Start using natural language commands
var response = await aiAgent.ProcessCommandAsync("Turn on the lights and set temperature to 22 degrees");
```

### Device Control Examples
```csharp
// Direct device control
await light.TurnOnAsync();
await light.SetBrightnessAsync(75);
await thermostat.SetTargetTemperatureAsync(23.5);
await security.ArmAsync();
await curtain.SetPositionAsync(50);

// Natural language control via AI
await aiAgent.ProcessCommandAsync("Good morning"); // Executes morning scene
await aiAgent.ProcessCommandAsync("Dim the lights to 30%");
await aiAgent.ProcessCommandAsync("Set temperature to 21 degrees");
await aiAgent.ProcessCommandAsync("Close the curtains halfway");
```

## 🧪 Testing

The Smart Home Demo includes comprehensive test coverage in `SmartHomeDemoTests.cs`:

### Test Categories
- **Unit Tests**: Individual GAgent functionality
- **Integration Tests**: Multi-device scenarios and scenes
- **Performance Tests**: Concurrent operations handling
- **Error Handling Tests**: Boundary conditions and invalid inputs

### Key Test Scenarios
- Device state management and persistence
- Event-driven communication between agents
- Scene automation (Good Morning, Good Night, Movie Mode)
- Concurrent multi-device operations
- Natural language processing accuracy
- Error handling and recovery

### Running Tests
```bash
dotnet test --filter "SmartHomeDemoTests"
```

## 📁 File Structure
```
SmartHome/
├── README.md                 # This documentation
├── HomeAIGAgent.cs          # AI coordinator with NLP capabilities
├── LightGAgent.cs           # Smart lighting control
├── ThermostatGAgent.cs      # Climate control system
├── SecurityGAgent.cs        # Security and motion detection
└── CurtainGAgent.cs         # Window covering automation
```

## 🔧 Implementation Details

### State Management
Each GAgent uses Orleans event sourcing for state management:
- **State Classes**: Immutable state objects with `[GenerateSerializer]`
- **State Log Events**: Typed events for state transitions
- **Event Handlers**: Process commands and update state through events
- **State Transitions**: `GAgentTransitionState` method handles state updates

### Configuration System
Devices support configuration through `ConfigurationBase`:
- **LightConfiguration**: Device ID and location settings
- **Runtime Configuration**: Initialize with specific parameters
- **State Initialization**: Configuration applied during GAgent activation

### Event Communication
Inter-agent communication uses strongly-typed events:
- **Command Events**: Trigger device actions
- **State Events**: Notify state changes to interested parties
- **Subscription Management**: Automatic event routing between registered agents

## 🎯 Advanced Features

### 1. **AI Tool Integration**
- Automatic tool registration for each device
- Dynamic command parsing and routing
- Context-aware device selection
- Multi-step command execution

### 2. **Error Handling**
- Graceful degradation when devices are offline
- Validation of parameters and ranges
- Retry mechanisms for failed operations
- Comprehensive logging and diagnostics

### 3. **Extensibility**
- Easy addition of new device types
- Pluggable AI providers
- Custom scene definitions
- Event-driven integrations

## 🎨 UI Integration

The Smart Home Demo includes a modern web interface:
- **Real-time Updates**: Live device status monitoring
- **Interactive Controls**: Direct device manipulation
- **Natural Language Input**: Chat-style command interface
- **Event Visualization**: Real-time event stream display
- **Multi-language Support**: English and Chinese interfaces

## 🔮 Future Enhancements

### Planned Features
- **Device Groups**: Control multiple devices as a unit
- **Scheduling**: Time-based automation rules
- **Energy Monitoring**: Power consumption tracking
- **External Integrations**: Weather, calendar, and IoT platform connections
- **Voice Control**: Speech-to-text integration
- **Mobile App**: Dedicated mobile interface

### Extensibility Points
- **Custom Device Types**: Framework for adding new device categories
- **AI Providers**: Support for additional LLM services
- **Event Sources**: External event integration (webhooks, IoT messages)
- **User Preferences**: Personalized automation behaviors

---

## 🎉 Conclusion

The Smart Home Demo represents a comprehensive example of modern multi-agent system architecture, combining:

- **Distributed Intelligence**: Each device operates autonomously
- **Natural Communication**: AI-powered language understanding
- **Event-Driven Design**: Loosely coupled, scalable architecture
- **Real-world Simulation**: Realistic device behaviors and constraints
- **Production-Ready Patterns**: Robust error handling and state management

This demo serves as both a practical application and a reference implementation for building sophisticated multi-agent systems with Aevatar.

**Ready to explore?** Start with the web interface at `/demos/smart-home-demo.html` or dive into the test suite to see all capabilities in action! 🚀 