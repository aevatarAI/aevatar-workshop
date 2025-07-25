# Dynamic AI MCP Integration

This demo showcases an advanced integration between AI agents and MCP (Model Context Protocol) servers, where the AI can dynamically discover and use tools from any configured MCP server.

## Key Features

### 🤖 Dynamic Tool Discovery
Unlike traditional setups with hardcoded tools, this AI agent can:
- Connect to any MCP server at runtime
- Automatically discover available tools
- Register them with Semantic Kernel for AI use

### 🔧 Flexible Tool Usage
The AI can intelligently:
- Understand tool descriptions and parameters
- Choose the right tool for the task
- Execute tools with proper arguments
- Interpret and explain results

### 🌐 Multiple MCP Servers
Connect to various MCP servers simultaneously:
- **Filesystem Server**: Access and manage files
- **Memory Server**: Store and retrieve data
- **Sequential Thinking**: Break down complex problems
- **Everything Server**: Demo various MCP features
- **Context7 Server**: General-purpose server with database, web search, and text utilities

## How It Works

1. **Initialize AI Agent**: Creates a new AI agent instance with dynamic tool capabilities
2. **Configure MCP Servers**: Select which MCP servers to connect to
3. **Automatic Tool Registration**: The agent discovers and registers all available tools
4. **Natural Language Interaction**: Chat with the AI and it will use the appropriate tools

## Example Use Cases

- **File Operations**: "List all files in the /tmp directory"
- **Data Storage**: "Store my shopping list in memory"
- **Complex Problem Solving**: "Help me plan a project step by step"
- **Multi-tool Workflows**: "Read a file, analyze its content, and store the summary"
- **Web Search** (Context7): "Search for recent developments in AI"
- **Text Processing** (Context7): "Extract key points from this document"
- **Database Operations** (Context7): "Query and manage data efficiently"

## Technical Details

The integration uses:
- **DynamicToolAIGAgent**: Custom agent that dynamically registers MCP tools
- **Semantic Kernel**: Microsoft's SDK for AI orchestration
- **KernelFunctionFactory**: Dynamic function creation at runtime
- **MCP Protocol**: Standard protocol for tool communication

Try it out and see how AI can dynamically adapt to available tools! 