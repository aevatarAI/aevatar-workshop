using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.MCP;
using Aevatar.GAgents.MCP.Provider;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Aevatar.Workshop.GAgent;
using Aevatar.Workshop.AIRouterWorkflowGAgent.Researcher;
using Microsoft.Extensions.Configuration;
using PsiGAgent.Common.Interfaces;
using PsiGAgent.Plugins;
using PsiGAgent.Plugins.Services;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Aws;

namespace Aevatar.Workshop.Host;

[DependsOn(
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule),
    typeof(AevatarModule),
    typeof(AbpBlobStoringModule),
    typeof(AevatarGAgentsMCPModule)
)]
public class WorkshopHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<WorkshopHostModule>(); });
        context.Services.AddHostedService<AevatarWorkshopHostedService>();
        context.Services.AddSerilog(_ => { },
            true, writeToProviders: true);
        context.Services.AddHttpClient();
        context.Services.AddSingleton<IEventDispatcher, DefaultEventDispatcher>();
        context.Services.AddSingleton<IBlobContainer, MockBlobContainer>();
        context.Services.Configure<SystemLLMConfigOptions>(configuration);
        context.Services.AddSemanticKernel();
        context.Services.AddSingleton<IKernelFactory, KernelFactory>();
        context.Services.AddSingleton<IKernelFunctionRegistry, KernelFunctionRegistry>();
        
        // Register web search services
        context.Services.AddHttpClient<WebContentFetcher>();
        context.Services.AddSingleton<IWebContentFetcher, WebContentFetcher>();
        
        // Register all search engines
        context.Services.AddSingleton<ISearchEngine, GoogleSearchEngine>(); // GoogleSearchEngine now uses built-in GoogleTextSearch
        context.Services.AddHttpClient<DuckDuckGoSearchEngine>();
        context.Services.AddHttpClient<BingSearchEngine>();
        context.Services.AddSingleton<ISearchEngine, DuckDuckGoSearchEngine>();
        context.Services.AddSingleton<ISearchEngine, BingSearchEngine>();
        
        // Register main web search service
        context.Services.AddSingleton<IWebSearchService, WebSearchService>();
        Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.ConfigureDefault(container =>
            {
                var configSection = configuration.GetSection("AwsS3");
                container.UseAws(o =>
                {
                    o.AccessKeyId = configSection.GetValue<string>("AccessKeyId");
                    o.SecretAccessKey = configSection.GetValue<string>("SecretAccessKey");
                    o.Region = configSection.GetValue<string>("Region");
                    o.ContainerName = configSection.GetValue<string>("ContainerName");
                });
            });
        });
    }
}