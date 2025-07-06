# AI Tool Calling Demo

This demo showcases how AI agents can discover and use other GAgents as tools through the Semantic Kernel framework.

## Overview

The AI Tool Calling Demo demonstrates:
- How AI agents use MathGAgent and TimeConverterGAgent as tools
- How Semantic Kernel's function calling enables automatic tool selection
- Real-time interaction between AI agents and GAgent tools
- Transparent tool usage with explanations

## Architecture

### Controller: AIToolCallingDemoController

Located at: `src/Aevatar.Workshop.Client/Controllers/AIToolCallingDemoController.cs`

Key endpoints:
- `POST /api/AIToolCalling/initialize` - Initialize AI agent with tool support
- `POST /api/AIToolCalling/chat` - Chat with AI agent that can use tools
- `GET /api/AIToolCalling/history/{agentId}` - Get tool call history
- `POST /api/AIToolCalling/test-tool` - Test tools directly

### AI Agent: ToolCallingAIGAgent

Located at: `src/Aevatar.Workshop.GAgent/ToolCallingAIGAgent.cs`

Features:
- Custom AI agent implementation specifically for tool calling demo
- Direct integration with MathGAgent and TimeConverterGAgent
- Uses Semantic Kernel's `ToolCallBehavior.AutoInvokeKernelFunctions` for automatic tool invocation
- Maintains chat history and tool registration state

### Available Tools

1. **MathGAgent** (`calculate_math`)
   - Performs mathematical calculations
   - Handles expressions like "25 * 4", "sqrt(16)", etc.

2. **TimeConverterGAgent**
   - `convert_time` - Converts time between timezones
   - `get_time_in_zone` - Gets current time in a specific timezone

## How It Works

1. **Initialization**:
   - Creates a new ToolCallingAIGAgent instance
   - Initializes with selected LLM system (OpenAI, Anthropic, etc.)
   - Registers MathGAgent and TimeConverterGAgent as Kernel functions
   - Uses `Plugins.AddFromFunctions()` to add tools to Semantic Kernel

2. **Tool Registration**:
   ```csharp
   var calculateFunc = KernelFunctionFactory.CreateFromMethod(
       method: async (string expression) => {
           var result = await _mathGAgent.CalculateAsync(expression);
           return $"The result of {expression} is {result}";
       },
       functionName: "calculate_math",
       description: "Calculate a mathematical expression"
   );
   ```

3. **Chat Processing**:
   - User message is processed with `ToolCallBehavior.AutoInvokeKernelFunctions`
   - LLM automatically decides when to use tools based on the query
   - Tools are invoked transparently during response generation
   - Results are incorporated into the final response

4. **Tool Tracking**:
   - Simple heuristic-based tracking of tool usage
   - Monitors response content for tool-related keywords
   - Maintains history of tool calls per agent session

## Example Usage

### Initialize Agent
```json
POST /api/AIToolCalling/initialize
{
  "llmSystem": "OpenAI"
}
```

### Chat with Tool Usage
```json
POST /api/AIToolCalling/chat
{
  "agentId": "generated-guid",
  "message": "What is 250 * 15% ?"
}
```

The AI will automatically use the `calculate_math` tool and respond with something like:
"I'll calculate 250 * 15% for you. The result of 250 * 0.15 is 37.5"

### Test Tools Directly
```json
POST /api/AIToolCalling/test-tool
{
  "toolName": "MathGAgent",
  "input": "sqrt(144) + 25"
}
```

## Frontend Integration

The demo is integrated into the main workshop page with:
- Agent initialization panel
- Chat interface
- Tool usage history
- Direct tool testing

Access the demo at: http://localhost:5000 and select "AI Tool Calling Demo" from the menu.

## Key Benefits

1. **Automatic Tool Selection**: LLM decides which tool to use based on context
2. **Transparent Usage**: AI explains what tools it's using and why
3. **Type-Safe Integration**: Strongly-typed function parameters and return values
4. **Extensible Design**: Easy to add more GAgent tools

## Implementation Notes

- Uses Semantic Kernel's modern plugin API (`Plugins.AddFromFunctions`)
- Avoids reflection-based approaches for better maintainability
- Simple state management using Orleans event sourcing
- Tool tracking based on response content analysis 