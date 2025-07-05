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

## Use Cases

This functionality is essential for:

- Building dynamic workflows that discover and use GAgents at runtime
- Creating AI agents that can dynamically invoke other agents based on capabilities
- Implementing service discovery patterns in distributed systems
- Building administrative tools for monitoring and managing GAgents 