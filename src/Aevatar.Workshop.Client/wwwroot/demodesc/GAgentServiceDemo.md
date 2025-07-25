# GAgentService & GAgentExecutor Demo

This demo showcases the core functionality of **GAgentService** and **GAgentExecutor**, two fundamental components in the Aevatar GAgent framework.

## Overview

### GAgentService
The `IGAgentService` interface provides methods to discover and query information about available GAgents in the system:

- **GetAllAvailableGAgentInformation()** - Returns all registered GAgents and their supported event types
- **GetGAgentDetailInfoAsync()** - Retrieves detailed information about a specific GAgent including description and configuration
- **FindGAgentsByEventTypeAsync()** - Finds all GAgents that can handle a specific event type

### GAgentExecutor
The `IGAgentExecutor` interface enables dynamic execution of GAgent event handlers:

- **ExecuteGAgentEventHandler()** - Executes an event handler on a GAgent and returns the result

## Demo Features

This interactive demo allows you to:

1. **List All GAgents** - View all available GAgents in the system along with their supported events
2. **Get GAgent Details** - Query detailed information about a specific GAgent
3. **Find GAgents by Event** - Search for GAgents that support a particular event type
4. **Execute GAgent** - Dynamically execute an event on a selected GAgent with custom parameters

## How It Works

The demo provides a real-time interface to explore the GAgent ecosystem:

- **Discovery**: Automatically discovers all registered GAgents using reflection and Orleans grain metadata
- **Caching**: Implements smart caching to improve performance when querying GAgent information
- **Dynamic Execution**: Uses the GAgentExecutor to invoke event handlers with proper timeout handling
- **Type Safety**: Validates event types and parameters before execution

## Featured Tool: MathGAgent (tools.math)

### Mathematical Calculations Made Easy

The **MathGAgent** (accessible as `tools.math`) is a powerful mathematical calculation agent that you should try first. It can evaluate complex mathematical expressions including:

- **Basic Arithmetic**: Addition (+), Subtraction (-), Multiplication (*), Division (/)
- **Powers and Roots**: 
  - Powers: `10^3` or `10**3` for 10³
  - Square roots: `sqrt(16)` → 4
  - Cube roots: `cbrt(27)` → 3
  - Nth roots: `35^(1/3)` for ∛35
- **Trigonometry**: sin, cos, tan (e.g., `sin(3.14159/2)` → 1)
- **Logarithms**: log, ln (e.g., `ln(2.71828)` → 1)

### Try These Examples:
1. **Simple calculation**: `2 + 2 * 3` → 8
2. **Power calculation**: `10^3` → 1000
3. **Root calculation**: `35^(1/3)` → 3.271... (cube root of 35)
4. **Complex expression**: `sqrt(16) + sin(3.14159/2) * 10` → 14

### How to Use MathGAgent:
1. Click on "Execute GAgent" in the demo
2. Select "MathGAgent" or search for "tools.math"
3. Choose the "MathCalculateEvent" event type
4. Enter your mathematical expression in the parameters
5. Click Execute to see the result!

## Other Available Tools

Explore these additional GAgent tools at your own pace:

- **TimeConverterGAgent** (`tools.time`): Convert times between different timezones
  - Example: Get current time in different timezones
  - Supports timezone conversions and formatting
  
- **Other GAgents**: Discover more specialized agents by using the "List All GAgents" feature

## Use Cases

This functionality is essential for:

- Building dynamic workflows that discover and use GAgents at runtime
- Creating AI agents that can dynamically invoke other agents based on capabilities
- Implementing service discovery patterns in distributed systems
- Building administrative tools for monitoring and managing GAgents 