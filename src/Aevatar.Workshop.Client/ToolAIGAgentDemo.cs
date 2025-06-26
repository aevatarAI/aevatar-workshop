using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.Workshop.Client;

public static class ToolAIGAgentDemo
{
    public static async Task RunAsync(IGAgentFactory gAgentFactory, string task = "Write a comprehensive report about artificial intelligence in 2024", string systemLLM = "OpenAI")
    {
        // Create the ExampleToolAIGAgent
        var toolAIGAgent = await gAgentFactory.GetGAgentAsync<IExampleToolAIGAgent>();
        await toolAIGAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are an intelligent AI coordinator that can call other agents to complete complex tasks.",
            LLMConfig = new LLMConfigDto { SystemLLM = systemLLM }
        });

        // Create supporting agents that the ToolAIGAgent might call
        var recorder = await gAgentFactory.GetGAgentAsync<IStateGAgent<RecorderGAgentState>>();
        Common.SetRecorder("ToolAIGAgentDemo", recorder);

        // Create a publisher to coordinate the agents
        var publisher = await gAgentFactory.GetGAgentAsync<IPublishingGAgent>();
        await publisher.RegisterAsync(toolAIGAgent);
        await publisher.RegisterAsync(recorder);

        // Log the start of the demo
        await publisher.PublishEventAsync(new RecordEvent
        {
            Message = $"ToolAIGAgent Demo started using LLM: {systemLLM}",
        });

        await publisher.PublishEventAsync(new RecordEvent
        {
            Message = $"Task assigned to ToolAIGAgent: {task}",
        });

        // Process the complex task using the ToolAIGAgent
        var result = await toolAIGAgent.ProcessComplexTaskAsync(task);

        // Log the completion
        await publisher.PublishEventAsync(new RecordEvent
        {
            Message = $"ToolAIGAgent Demo completed. Result: {result}",
        });
    }
}