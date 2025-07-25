using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Plugins;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Web.Google;
using Microsoft.SemanticKernel.Plugins.Web.Tavily;

namespace Aevatar.Workshop.Host;

public class AevatarWorkshopHostedService : IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;

    public AevatarWorkshopHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        IServiceProvider serviceProvider)
    {
        _application = application;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _application.InitializeAsync(_serviceProvider);
        var functionRegistry = _serviceProvider.GetService(typeof(IKernelFunctionRegistry)) as IKernelFunctionRegistry;
        if (functionRegistry != null)
        {
            MathFunctionRegistration.RegisterAllMathFunctions(functionRegistry);

            // Register DemoPlugin functions
            var demoFunctions = new List<KernelFunction>
            {
                KernelFunctionFactory.CreateFromMethod((int a, int b) => DemoPlugin.AddNumbersAsync(a, b), "AddNumbers",
                    "Add two numbers"),
                // KernelFunctionFactory.CreateFromMethod(() => DemoPlugin.GetNYGDP2024Async(), "GetNYGDP2024",
                //     "Get New York GDP for 2024"),
                // KernelFunctionFactory.CreateFromMethod(() => DemoPlugin.GetCAGDP2024Async(), "GetCAGDP2024",
                //     "Get California GDP for 2024"),
                KernelFunctionFactory.CreateFromMethod(() => DemoPlugin.GetUSGDP2024Async(), "GetUSGDP2024",
                    "Get US GDP for 2024"),
                KernelFunctionFactory.CreateFromMethod((string stateCode) => DemoPlugin.GetStateGDP2024Async(stateCode), "GetStateGDP2024",
                    "Get GDP for a US state in 2024 by state code (e.g., 'NY', 'CA'), argument is the state code"),
                KernelFunctionFactory.CreateFromMethod(
                    (double part, double whole) => DemoPlugin.CalculatePercentageAsync(part, whole),
                    "CalculatePercentage", "Calculate percentage of part over whole")
            };
            var demoPlugin = KernelPluginFactory.CreateFromFunctions("DemoPlugin", demoFunctions);
            functionRegistry.RegisterPlugin("DemoPlugin", demoPlugin);
            functionRegistry.RegisterFunction("AddNumbers", demoPlugin["AddNumbers"]);
            // functionRegistry.RegisterFunction("GetUSGDP2024", demoPlugin["GetUSGDP2024"]);
            // functionRegistry.RegisterFunction("GetNYGDP2024", demoPlugin["GetNYGDP2024"]);
            functionRegistry.RegisterFunction("CalculatePercentage", demoPlugin["CalculatePercentage"]);

            // Register web search functions
            WebSearchFunctionRegistration.RegisterAllWebSearchFunctions(functionRegistry, _serviceProvider);
            
            // var tavilyTextSearch = GetTavilyTextSearch();
            // functionRegistry.RegisterPlugin("TavilyWebSearch",
            //     tavilyTextSearch.CreateWithGetTextSearchResults("TavilyWebSearch",
            //         "Search for web content using Tavily"));
        }
    }

    private GoogleTextSearch GetGoogleTextSearch()
    {
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        var searchEngineId = Environment.GetEnvironmentVariable("GOOGLE_SEARCH_ENGINE_ID");

        return new GoogleTextSearch(
            searchEngineId: searchEngineId,
            apiKey: apiKey
        );
    }

    private TavilyTextSearch GetTavilyTextSearch()
    {
        var apiKey = Environment.GetEnvironmentVariable("TAVILY_API_KEY");
        return new TavilyTextSearch(apiKey);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}