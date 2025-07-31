# Smart Home Demo

This demonstration showcases the power of AI GAgent system in managing smart home devices through natural language interactions.

## Overview

The Smart Home Demo integrates multiple intelligent agents (GAgents) to create a cohesive smart home experience. Each device is represented by its own GAgent, and a central AI GAgent coordinates all interactions using natural language processing.

## System Architecture

### Core Components

1. **HomeAIGAgent**: Central AI coordinator that processes natural language commands
2. **LightGAgent**: Manages smart lighting systems
3. **ThermostatGAgent**: Controls heating and cooling systems
4. **SecurityGAgent**: Handles security system operations
5. **CurtainGAgent**: Manages automated window coverings

### Technology Stack

- **Orleans Framework**: Distributed actor model for GAgent implementation
- **AI Integration**: Large Language Model (LLM) integration for natural language understanding
- **Event-Driven Architecture**: Real-time communication between GAgents
- **Multi-language Support**: Complete internationalization (English/Chinese)

## Features

### Natural Language Control
- Process commands in both English and Chinese
- Understand complex, contextual requests
- Support for conversational interactions

### Device Management
- **Smart Lights**: On/off control, brightness adjustment (0-100%)
- **Thermostat**: Temperature setting, mode switching (heat/cool/auto/off)
- **Security System**: Arm/disarm functionality, motion detection alerts
- **Smart Curtains**: Position control (0-100% open/closed)

### Real-time Synchronization
- Instant device status updates
- Live feedback on command execution
- Synchronized state across all interfaces

### Multi-language Interface
- Automatic language detection from browser settings
- Manual language switching
- Localized error messages and responses

## How It Works

1. **Initialization**: The system creates and registers all GAgents
2. **Event Subscription**: The AI GAgent subscribes to events from all device GAgents
3. **Command Processing**: Natural language commands are processed by the AI GAgent
4. **Tool Calling**: The AI GAgent calls appropriate device GAgent methods
5. **State Updates**: Device states are updated through event sourcing
6. **Response Generation**: User-friendly responses are generated in the selected language

## Example Commands

### English Commands
- "Turn on the living room lights"
- "Set temperature to 22 degrees"
- "Arm the security system"
- "Close the curtains halfway"
- "Dim the lights to 30%"

### Chinese Commands
- "打开客厅的灯"
- "把温度设置为22度"
- "启动安防系统"
- "把窗帘关到一半"
- "把灯光调暗到30%"

## Technical Implementation

### GAgent Communication
```csharp
// Example: AI GAgent calling Light GAgent
var lightGAgent = await GAgentFactory.GetGAgentAsync<ILightGAgent>(lightId);
await lightGAgent.TurnOnAsync();
```

### Event Sourcing
```csharp
// State changes are tracked through events
RaiseEvent(new LightTurnedOnEvent { Brightness = 100 });
await ConfirmEvents();
```

### Internationalization
```csharp
// Localized responses based on user language
var message = _localizationService.GetText("light_turned_on", userLanguage);
```

## Getting Started

1. Click "Open Smart Home Demo" to launch the standalone interface
2. Initialize the system by clicking "Initialize System"
3. Try natural language commands in the command input area
4. Use manual controls for direct device interaction
5. Switch languages using the language selector in the top-right corner

## API Endpoints

The demo exposes RESTful APIs for external integration:

- `POST /api/smarthome/initialize` - Initialize the smart home system
- `POST /api/smarthome/command` - Process natural language commands
- `GET /api/smarthome/status` - Get current device states
- `POST /api/smarthome/device/{type}` - Direct device control
- `GET /api/localization/current-language` - Get user's preferred language

This demonstration showcases how AI GAgents can create intelligent, responsive, and user-friendly smart home experiences with minimal technical complexity for end users. 