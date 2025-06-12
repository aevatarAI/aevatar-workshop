using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.Router.GAgents;
using Aevatar.GAgents.Router.GEvents;
using Aevatar.Workshop.AIRouterWorkflowGAgent.Researcher;
using Aevatar.Workshop.AIRouterWorkflowGAgent.Writer;

namespace Aevatar.Workshop.Client;

public static class RouterDemo
{
    public static async Task RunAsync(IGAgentFactory gAgentFactory)
    {
        var routerGAgent = await gAgentFactory.GetGAgentAsync<IRouterGAgent>();
        await routerGAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a router agent",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });

        var researcherGAgent = await gAgentFactory.GetGAgentAsync<IResearcherGAgent>();
        await researcherGAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a researcher",
            LLMConfig = new LLMConfigDto { SystemLLM = "OpenAI" }
        });
        var researcherGAgentEvents = await researcherGAgent.GetAllSubscribedEventsAsync();
        await routerGAgent.AddAgentDescription(researcherGAgent.GetType(), researcherGAgentEvents);

        var writerGAgent = await gAgentFactory.GetGAgentAsync<IWriterGAgent>();
        await writerGAgent.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a writer",
            LLMConfig = new LLMConfigDto() { SystemLLM = "OpenAI" }
        });
        var writerGAgentEvents = await writerGAgent.GetAllSubscribedEventsAsync();
        await routerGAgent.AddAgentDescription(writerGAgent.GetType(), writerGAgentEvents);

        var publisher = await gAgentFactory.GetGAgentAsync<IPublishingGAgent>(Guid.NewGuid());
        await publisher.PublishEventAsync(new BeginTaskGEvent
        {
            TaskDescription = "Research AI agent and write a brief report about it."
        }, routerGAgent, researcherGAgent, writerGAgent);

        var researchResult = string.Empty;
        while (researchResult.IsNullOrWhiteSpace())
        {
            researchResult = await researcherGAgent.GetResultAsync();
            if (researchResult.IsNullOrWhiteSpace())
            {
                await Task.Delay(5000);
                continue;
            }

            Console.WriteLine("Research result:");
            Console.WriteLine(researchResult);
            break;
        }

        Console.WriteLine();

        var article = string.Empty;
        while (article.IsNullOrWhiteSpace())
        {
            article = await writerGAgent.GetArticleAsync();
            if (article.IsNullOrWhiteSpace())
            {
                await Task.Delay(5000);
                continue;
            }

            Console.WriteLine("Report:");
            Console.WriteLine(article);
            break;
        }
    }
}