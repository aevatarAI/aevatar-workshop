# Router Demo

This demo showcases a powerful workflow where a **Router Agent** intelligently coordinates between two specialized AI Agents, **Researcher** and **Writer**, to accomplish a multi-step task. It demonstrates how a complex process can be broken down and delegated to the right agent at the right time.

### The Workflow: From Task to Report

The process you initiate with a single click unfolds in a precise, managed sequence:

1.  **Task Initiation**: When you run the demo, a `BeginTaskGEvent` is created with the initial goal: `"Research AI agent and write a brief report about it."` This event isn't sent to a specific agent, but to the central **Router Agent**.

2.  **Intelligent Routing (Step 1)**: The Router Agent analyzes the task description. It determines that the first logical step is "research" and automatically routes the task by creating and sending a `ResearchEvent` to the appropriate specialist: the **Researcher Agent**.

3.  **The Researcher's Role**: The Researcher Agent activates upon receiving the `ResearchEvent`. Its job is to execute the research portion of the task, guided by its simple, focused prompt from `ResearchPromptTemplate.cs`:
    > You are a researcher, you only need to complete the research according to the research content and output the research results.

4.  **Passing the Baton**: Once its research is complete, the Researcher Agent publishes its findings in a `RouteNextGEvent`. This signals to the Router Agent that the first step is complete and the results are ready.

5.  **Intelligent Routing (Step 2)**: The Router Agent, knowing the research is done, proceeds to the next logical step: "writing". It creates a `WriteEvent`, populates it with the research results, and sends it to the **Writer Agent**.

6.  **The Writer's Role**: The Writer Agent activates. It takes the content from the `WriteEvent` and uses its own specialized prompt from `WriterPromptTemplate.cs` to generate the final output:
    > You are a writer and you need to output articles based on the content provided.

You can observe this entire chain of events, including the prompts sent to each agent and their final outputs, in the "AI Messages" panel. This demonstrates a loosely coupled but highly effective multi-agent system, orchestrated by a central router.

- Demonstrates AI-powered routing and multi-agent orchestration.
- You must configure your API key in `src/Aevatar.Workshop.Host/appsettings.json` before running.
- Select the LLM system before running the demo.
- Host Log shows orchestration details, Client Log shows the final report.
- Great for exploring AI-driven agent workflows. 