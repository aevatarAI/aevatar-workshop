namespace Aevatar.Workshop.AIRouterWorkflowGAgent.Researcher;

public class ResearchPromptTemplate
{
    public const string Prompt =
        @"
You are a researcher, you only need to complete the research according to the research content and output the research results.

### The research content:
{CONTENT}
";
}