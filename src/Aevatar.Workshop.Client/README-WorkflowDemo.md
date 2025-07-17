# Workflow Demo Guide

## Quick Start

### Step 1: Create a Workflow
1. Enter a workflow name and description (optional)
2. Click the **"Create Workflow"** button
3. The canvas hint will disappear, indicating you can now add nodes

### Step 2: Add Nodes
1. **Drag** any agent from the left sidebar
2. **Drop** it onto the canvas
3. Repeat to add more nodes

### Step 3: Connect Nodes
1. Click the **output port** (right side) of a node
2. Click the **input port** (left side) of another node
3. A connection line will appear

### Step 4: Configure & Run
1. Click **"Configure"** to set up the workflow
2. Click **"▶ Start"** to execute
3. Watch nodes change color as they process

## Troubleshooting

### "Cannot drag agents to canvas"
- **Solution**: Click "Create Workflow" first! You'll see a prompt in the center of the canvas.

### "Unknown work unit type" error
- **Cause**: Agent types were renamed in recent updates
- **Solution**: Refresh the page and try again

### "Failed to add node" error
- **Possible causes**:
  - Workflow not created yet
  - Backend service not running
  - Agent type mismatch

## Architecture Changes

The workflow demo has been updated to match the latest GAgent architecture:

- `IMathGAgent` → `IWorkflowMathGAgent`
- `ITimeConverterGAgent` → `IWorkflowTimeConverterGAgent`
- `GroupMemberGAgentBase` → `MemberGAgentBase`
- Event publishing now uses `IPublishingGAgent`

## Example Workflows

### Simple Calculation Pipeline
```
MathAgent → TimeConverter → DataProcessor
```
- Math: Calculate expression
- Time: Get current time
- Data: Aggregate results

### AI-Powered Analysis
```
DataProcessor → AIAgent → DataProcessor
```
- Filter data
- AI analysis
- Format output

## Technical Details

### Node Types

1. **WorkflowMathGAgent**
   - Performs mathematical calculations
   - Extracts expressions from natural language

2. **WorkflowTimeConverterGAgent**
   - Converts between time zones
   - Cross-platform compatible

3. **DataProcessorGAgent**
   - Three modes: transform, filter, aggregate
   - Processes text data

4. **AIGAgent**
   - Customizable with system prompts
   - Integrates with chosen LLM

### Backend API

The WorkflowDemoController provides:
- `POST /api/WorkflowDemo/create` - Create new workflow
- `POST /api/WorkflowDemo/{id}/units` - Add work units
- `POST /api/WorkflowDemo/{id}/configure` - Configure connections
- `POST /api/WorkflowDemo/{id}/start` - Start execution
- `GET /api/WorkflowDemo/{id}/status` - Check status
- `POST /api/WorkflowDemo/{id}/reset` - Reset workflow

### State Management

Workflows go through these states:
1. **Created** - Initial state
2. **Configured** - Nodes and connections set
3. **Running** - Execution in progress
4. **Completed** - All nodes finished

### Data Flow

1. Initial input provided at start
2. Each node processes and passes data via Blackboard
3. Downstream nodes receive upstream results
4. Terminal nodes complete the workflow 