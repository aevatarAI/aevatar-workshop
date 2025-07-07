# Dynamic AI with MCP & GAgent Tools

This enhanced demo showcases how an AI agent can dynamically utilize both MCP (Model Context Protocol) services and other GAgent tools to perform complex tasks.

## Features

### 🔌 MCP Services
- **Filesystem**: Read, write, and manage files
- **Memory**: Store and retrieve temporary data
- **Sequential Thinking**: Break down complex problems
- **Everything**: Various test tools

### 🛠️ GAgent Tools
- **MathGAgent**: Perform mathematical calculations
- **TimeConverterGAgent**: Convert times between timezones
- **Other Tool GAgents**: Any GAgent in the "tools" namespace

## How to Use

1. **Initialize AI Agent**
   - Select your preferred LLM (DeepSeek by default)
   - Click "Initialize AI Agent"

2. **Connect MCP Servers** (Optional)
   - Select the MCP servers you want to use
   - Click "Connect Selected Servers"

3. **Configure GAgent Tools**
   - Select the GAgent tools you want to make available
   - Click "Configure Selected GAgents"

4. **Start Chatting**
   - Ask the AI to perform tasks using any available tools
   - The AI will automatically choose the right tool for the job

## Example Queries

### Math Calculations (using MathGAgent)
- "Calculate 250 * 15%"
- "What's the square root of 144?"
- "Solve: (100 + 50) * 2 / 5"

### Time Operations (using TimeConverterGAgent)
- "What time is it in Tokyo?"
- "Convert 3 PM EST to Pacific time"
- "Show me the current time in London"

### File Operations (using MCP Filesystem)
- "List files in /tmp directory"
- "Create a file called test.txt in /tmp"
- "Read the contents of /tmp/test.txt"

### Combined Operations
- "Calculate 15% of 250 and save the result to /tmp/calculation.txt"
- "What time is it in Tokyo? Convert that to New York time"

## Technical Details

The AI agent uses Semantic Kernel to:
- Dynamically discover available GAgents through `IGAgentService`
- Register both MCP tools and GAgent event handlers as kernel functions
- Let the LLM decide which tools to use based on the user's request
- Execute tools and return results in a unified way

This demonstrates the power of combining different tool types in a single AI agent for maximum flexibility and capability. 