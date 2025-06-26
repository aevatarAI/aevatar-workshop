# ToolAI GAgent Demo

This demo showcases the **ToolAIGAgent** - an intelligent AI coordinator that can call other agents in the system to complete complex tasks. It demonstrates how Large Language Models (LLMs) can intelligently orchestrate multiple agents through tool calling mechanisms.

### What is ToolAIGAgent?

The **ToolAIGAgent** is an advanced AI agent that extends the basic `AIGAgentBase` with tool calling capabilities. It integrates with Semantic Kernel to provide LLMs with the ability to:

- **Analyze complex tasks** and break them down into steps
- **Call other GAgents** in the system as tools
- **Coordinate multiple agents** to achieve comprehensive results
- **Provide intelligent routing** of subtasks to specialized agents

### How It Works

1. **Task Reception**: The ToolAIGAgent receives a complex task from the user
2. **Task Analysis**: The LLM analyzes the task and determines what steps are needed
3. **Tool Selection**: Based on the analysis, it decides which tools (other GAgents) to call
4. **Execution**: It calls the appropriate GAgents with specific instructions
5. **Result Integration**: It combines the results from different agents into a comprehensive response

### Available Tools

The ToolAIGAgent has access to several built-in tools:

- **call_gagent**: Call any GAgent in the system by its identifier
- **research**: Call ResearcherGAgent for information gathering tasks
- **write**: Call WriterGAgent for content creation tasks  
- **record**: Call RecorderGAgent to log important information

### Example Workflow

When you give the ToolAIGAgent a task like *"Write a comprehensive report about artificial intelligence in 2024"*, it might:

1. First call the **research** tool to gather information about AI trends in 2024
2. Then call the **write** tool to create a structured report based on the research
3. Finally call the **record** tool to log the completion of the task

You can watch this intelligent coordination process unfold in the "AI Messages" panel below, where you'll see how the LLM makes decisions about which tools to use and how it processes the results.

### Benefits

- **Intelligent Task Decomposition**: Complex tasks are automatically broken down
- **Specialized Agent Utilization**: Each subtask is handled by the most appropriate agent
- **Seamless Integration**: All agents work together as a unified system
- **Transparent Process**: You can observe the decision-making and coordination process

Try it out with different types of complex tasks to see how the ToolAIGAgent intelligently coordinates other agents!