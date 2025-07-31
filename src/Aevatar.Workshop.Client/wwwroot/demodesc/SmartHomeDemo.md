# Smart Home Multi-Agent Demo

## Overview

Experience the power of Aevatar's multi-agent system through an intuitive smart home control interface. This demo showcases how multiple autonomous agents collaborate through event-driven communication to create an intelligent home automation system.

## Key Features

### 🤖 Natural Language Control
- **AI-Powered Interface**: Control your entire home with natural language commands
- **Intelligent Understanding**: The AI agent understands context and intent
- **Real-time Feedback**: Get instant responses from the AI assistant

### 🏠 Multi-Agent Architecture
- **Independent Agents**: Each device operates as an autonomous agent
- **Event-Driven Communication**: Agents communicate through events, not direct calls
- **Fault Tolerance**: If one device fails, others continue working independently

### 🎯 Smart Device Control
- **Multiple Device Types**: Control lights, thermostat, security system, and smart curtains
- **Manual Controls**: Direct device interaction through intuitive interface
- **Smart Coordination**: AI can control multiple devices with a single command

## How It Works

1. **Natural Language Processing**
   - User says: "Turn on the living room lights"
   - HomeAIGAgent processes the command using Semantic Kernel
   - Converts natural language to structured events

2. **Event Flow**
   - AI publishes `DeviceControlIntentEvent`
   - HomeCoordinatorGAgent receives and routes the event
   - Specific device agent (LightGAgent) executes the command
   - State changes are published as events

3. **State Management**
   - Each agent maintains its own state independently
   - Event sourcing provides complete history
   - Orleans framework ensures reliability and scalability

## Device Control Options

### 💡 Smart Lighting
- **On/Off Control**: Turn lights on or off
- **Brightness Adjustment**: Fine-tune lighting levels (0-100%)
- **Voice Commands**: "Turn on the lights", "Set brightness to 50%"

### 🌡️ Smart Thermostat
- **Temperature Control**: Adjust target temperature (16-30°C)
- **Mode Selection**: Auto, Heat, Cool modes
- **Voice Commands**: "Set temperature to 22 degrees", "Switch to cool mode"

### 🔒 Security System
- **Arm/Disarm**: Control home security status
- **Activity Monitoring**: Track last security events
- **Voice Commands**: "Arm security", "Disarm the alarm"

### 🪟 Smart Curtains
- **Position Control**: Adjust curtain openness (0-100%)
- **Quick Presets**: Fully open, half open, fully closed
- **Voice Commands**: "Open the curtains", "Close curtains halfway"

## Try These Commands

- **Basic Control**: "Turn on the lights", "Set temperature to 22 degrees"
- **Complex Commands**: "Turn off all lights and arm the security system"
- **Status Queries**: "What's the current temperature?", "Are the lights on?"
- **Multi-device Control**: "Turn on lights and open curtains"

## Architecture Benefits

- **Scalability**: Easy to add new devices - just create a new GAgent
- **Reliability**: Failure isolation - each agent runs independently
- **Maintainability**: Clear responsibilities for each agent
- **Extensibility**: Add new features without modifying existing code

## Technical Highlights

- **Orleans Virtual Actors**: Each device is a grain with persistent state
- **Event Sourcing**: Complete audit trail of all home activities
- **AI Integration**: Semantic Kernel for natural language understanding
- **Real-time Updates**: Event-driven architecture ensures instant feedback 