using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.Workshop.Client;

public static class MultiGAgentDemo
{
    public static async Task RunAsync(IGAgentFactory gAgentFactory, int number = 42, string systemLLM = "OpenAI")
    {
        // Create Alice and Bob
        var alice = await gAgentFactory.GetGAgentAsync<IAliceGAgent>();
        await alice.InitializeAsync(new InitializeDto
        {
            Instructions = "You are Alice, a guessing game player.",
            LLMConfig = new LLMConfigDto { SystemLLM = systemLLM }
        });

        var bob = await gAgentFactory.GetGAgentAsync<IBobGAgent>();
        await bob.InitializeAsync(new InitializeDto
        {
            Instructions = "You are Bob, a guessing game player. Try to guess Alice's number.",
            LLMConfig = new LLMConfigDto { SystemLLM = systemLLM }
        });

        // Assign to static field
        var recorder = await gAgentFactory.GetGAgentAsync<IStateGAgent<RecorderGAgentState>>();
        Common.SetRecorder("MultiGAgentDemo", recorder);

        var publisher = await gAgentFactory.GetGAgentAsync<IPublishingGAgent>();
        await publisher.RegisterAsync(alice);
        await publisher.RegisterAsync(bob);
        await publisher.RegisterAsync(recorder);

        await publisher.PublishEventAsync(new RecordEvent
        {
            Message = $"Using API Key of: {systemLLM}.",
        });

        await alice.PrepareAsync(number);
        await bob.StartGuessingAsync();
    }
}