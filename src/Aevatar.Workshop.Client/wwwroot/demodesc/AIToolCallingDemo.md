# AI Tool Calling Demo

This demo showcases how AIGAgentBase enables AI agents to discover and use other GAgents as tools through the Semantic Kernel framework.

## Features

### 🔧 Available Tools

- **MathGAgent**: Performs complex mathematical calculations
  - Supports basic arithmetic, trigonometry, logarithms, and more
  - Handles natural language math expressions like "square root of 16"
  
- **TimeConverterGAgent**: Handles time-related operations
  - Converts times between different timezones
  - Shows current time in any timezone
  - Calculates time differences

### 🤖 How It Works

1. **Automatic Discovery**: The AI agent automatically discovers available GAgents through the `IGAgentService`
2. **Tool Registration**: Each GAgent's event handlers are registered as Kernel Functions
3. **Smart Selection**: The LLM decides when and how to use these tools based on your requests
4. **Natural Language**: Just ask in plain English - the AI will figure out which tool to use

### 💡 Example Queries

Try asking the AI agent questions like:
- "What's 15% of 250?"
- "Calculate the square root of 144 plus 25"
- "What time is it in Tokyo right now?"
- "Convert 3 PM EST to PST"
- "How many hours between 9 AM and 5:30 PM?"
- "Solve this: (12 + 8) * 3 / 2"

### 🚀 Getting Started

1. **Initialize Agent**: Choose your LLM system and click "Initialize"
2. **Ask Questions**: Type any math or time-related question
3. **Watch Tools in Action**: See which tools the AI uses and their results
4. **View History**: Check the tool call history to understand the AI's decision process

The AI agent will explain which tool it's using and why, giving you transparency into its decision-making process. 