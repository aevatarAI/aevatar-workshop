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

### 🎯 Intelligent Coordination
- **Scene Management**: Pre-configured scenes like "Good Morning" or "Good Night"
- **Smart Orchestration**: The coordinator agent manages complex multi-device operations
- **Flexible Control**: Natural language or direct device control

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

## Try These Commands

- **Basic Control**: "Turn on the lights", "Set temperature to 22 degrees"
- **Scene Activation**: "Good night", "I'm leaving home"
- **Complex Commands**: "Turn off all lights and arm the security system"
- **Status Queries**: "What's the current temperature?", "Are the lights on?"

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