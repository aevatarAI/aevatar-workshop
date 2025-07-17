# Workflow Demo

This demo showcases the powerful **WorkflowCoordinatorGAgent** that enables visual workflow orchestration similar to platforms like Dify. You can create, configure, and execute complex workflows by connecting different GAgents as workflow nodes.

## Features

### 🎯 Visual Workflow Editor
- **Drag & Drop Interface**: Simply drag agents from the sidebar to create workflow nodes
- **Connection Management**: Click output/input ports to connect nodes and define execution flow
- **Real-time Status**: Watch nodes change color as they process during execution
- **Node Configuration**: Double-click nodes to configure specific settings

### 🔧 Available Workflow Nodes

1. **Math Agent** 
   - Performs mathematical calculations
   - Extracts and evaluates expressions from input

2. **Time Converter**
   - Converts times between different time zones
   - Shows current time in various zones

3. **Data Processor**
   - Transform: Convert data to uppercase with timestamps
   - Filter: Extract lines containing numbers
   - Aggregate: Count words, lines, and characters

4. **AI Agent**
   - Custom AI-powered processing
   - Configurable system prompts
   - Integrates with your chosen LLM

### 🚀 How It Works

1. **Create Workflow**: Name your workflow and add a description
2. **Add Nodes**: Drag agents from the sidebar onto the canvas
3. **Connect Nodes**: Click output port → input port to create connections
4. **Configure**: Set up the workflow with initial parameters
5. **Execute**: Start the workflow and watch it process in real-time

### 💡 Example Workflows

**Data Processing Pipeline**:
- Math Agent (calculate metrics) → Data Processor (aggregate) → AI Agent (generate report)

**Time-based Calculations**:
- Time Converter (get current time) → Math Agent (calculate duration) → Data Processor (format output)

**Multi-step Analysis**:
- AI Agent (extract data) → Data Processor (filter) → Math Agent (calculate) → AI Agent (summarize)

## Technical Details

The WorkflowCoordinatorGAgent provides:
- **State Management**: Tracks workflow status (Pending, InProgress, Finished)
- **Dependency Resolution**: Ensures nodes execute in the correct order
- **Parallel Execution**: Runs independent nodes simultaneously
- **Data Sharing**: Uses Blackboard pattern for inter-node communication
- **Loop Detection**: Prevents infinite loops in workflow design

This demo demonstrates how Aevatar's multi-agent system can be used to build sophisticated workflow automation tools with minimal code. 