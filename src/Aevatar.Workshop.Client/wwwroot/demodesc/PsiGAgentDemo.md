# PsiGAgent Demo

## Overview
The PsiGAgent Demo showcases a revolutionary self-organizing AI agent system that can dynamically adapt its operation mode based on the complexity of tasks. Unlike traditional AI agents, PsiGAgent automatically decides whether to operate as an Orchestrator (delegating tasks to specialized child agents) or as a Specialized agent (directly executing specific tasks).

## Key Features
- **Self-Organization**: Agents automatically determine their optimal operation mode
- **Dynamic Hierarchy**: Complex tasks trigger the creation of child agent hierarchies
- **Real-time Visualization**: Watch the agent network grow and evolve as it processes tasks
- **Flexible Task Handling**: From simple queries to complex multi-step projects

## How It Works
1. **Task Analysis**: When given a task, PsiGAgent analyzes its complexity and requirements
2. **Mode Selection**: Based on the analysis, it decides to either:
   - **Orchestrate**: Break down complex tasks and delegate to specialized child agents
   - **Specialize**: Directly handle specific, well-defined tasks
3. **Dynamic Scaling**: As needed, agents can create child agents, forming a task-solving hierarchy
4. **Result Aggregation**: Orchestrator agents collect and synthesize results from their children

## Example Use Cases
- **Party Planning**: Breaks down into venue research, catering options, entertainment planning
- **Research Projects**: Creates specialized agents for data gathering, analysis, and summarization
- **Content Creation**: Delegates research, writing, and editing to different specialized agents
- **Problem Solving**: Decomposes complex problems into manageable sub-problems

## Technical Highlights
- Built on Orleans Actor Framework for distributed agent management
- Uses Semantic Kernel for AI capabilities
- Real-time state synchronization and visualization
- Hierarchical task decomposition with automatic agent creation

## Getting Started
1. Click "Create New Agent" to initialize a root PsiGAgent
2. Enter a task or select from the example tasks
3. Watch as the agent analyzes the task and creates its execution strategy
4. Observe the agent hierarchy visualization update in real-time
5. View the final results in the chat history

This demo represents a significant advancement in autonomous AI systems, showcasing how agents can self-organize to tackle complex, multi-faceted challenges without explicit programming for each scenario. 