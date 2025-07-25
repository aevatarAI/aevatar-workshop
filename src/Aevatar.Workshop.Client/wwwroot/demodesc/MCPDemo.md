# MCP Demo

This demo showcases the Model Context Protocol (MCP) integration with Aevatar GAgents framework. MCP is a protocol that enables AI assistants to interact with external tools and services through a standardized interface.

## Overview

The MCP GAgent allows you to:
- Configure and connect to multiple MCP servers
- Discover available tools from connected servers
- Execute tools with specified parameters
- View server connection states and tool call history

## Available MCP Servers

This demo includes three example MCP servers:

### 1. Filesystem Server
- Access and manipulate files in the `/tmp` directory
- Read, write, and list files
- Useful for file-based operations

### 2. Time Server
- Get current time in different timezones
- Convert times between zones
- Perform time-related calculations

### 3. Memory Server
- Store and retrieve key-value pairs
- Maintain persistent memory across sessions
- Useful for state management

## How to Use

1. **Select Servers**: Choose which MCP servers to connect to
2. **Initialize**: Click the initialize button to establish connections
3. **View Status**: Check server connection states and available tools
4. **Call Tools**: Select a tool, provide arguments in JSON format, and execute
5. **View History**: Track all tool calls and their results

## Technical Details

- MCP servers run as separate processes using stdio communication
- Tools are discovered dynamically after connection
- All tool calls are event-driven through the GAgent system
- Results are returned asynchronously and displayed in real-time 