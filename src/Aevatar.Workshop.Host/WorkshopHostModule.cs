using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP;
using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Plugins;
using Aevatar.GAgents.PsiOmni.Plugins.Services;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Microsoft.Extensions.Configuration;
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
        context.Services.AddSingleton<IGAgentExecutor, GAgentExecutor>();
        
        // Configure SystemLLMConfigOptions with merged configurations
        context.Services.Configure<SystemLLMConfigOptions>(options =>
        {
            var llmConfigs = new Dictionary<string, LLMConfig>();
            
            // Load from appsettings.json
            var section = configuration.GetSection("SystemLLMConfigs");
            if (section.Exists())
            {
                var configs = section.Get<Dictionary<string, LLMConfig>>();
                if (configs != null)
                {
                    foreach (var kvp in configs)
                    {
                        llmConfigs[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            // Configuration already includes both files due to Program.cs loading both
            // The second file (appsettings.secrets.json) will override any duplicate keys
            options.SystemLLMConfigs = llmConfigs;
            
            // Log loaded configurations for debugging
            Log.Information("Loaded {Count} SystemLLMConfigs:", llmConfigs.Count);
            foreach (var kvp in llmConfigs)
            {
                Log.Information("  - {Key}: {Provider} ({ModelName})", 
                    kvp.Key, 
                    kvp.Value.ProviderEnum,
                    kvp.Value.ModelName);
            }
        });
        
        context.Services.AddSemanticKernel();
        context.Services.AddSingleton<IKernelFactory, KernelFactory>();
        context.Services.AddSingleton<IKernelFunctionRegistry, KernelFunctionRegistry>();
        
        // Register web search services
        context.Services.AddHttpClient<WebContentFetcher>();
        context.Services.AddSingleton<IWebContentFetcher, WebContentFetcher>();
        
        // Register all search engines
        context.Services
            .AddSingleton<ISearchEngine, GoogleSearchEngine>(); // GoogleSearchEngine now uses built-in GoogleTextSearch
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