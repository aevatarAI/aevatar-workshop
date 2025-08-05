using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP;
using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Plugins;
using Aevatar.GAgents.PsiOmni.Plugins.Services;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Aevatar.Workshop.GAgent.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.BlobStoring;

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
        
        // Configure SystemLLMConfigOptions using standard configuration binding
        // This will automatically work with IOptionsMonitor and respond to configuration changes
        context.Services.Configure<SystemLLMConfigOptions>(configuration.GetSection("SystemLLMConfigs"));

        context.Services.AddSemanticKernel();
        context.Services.AddSingleton<IKernelFactory, KernelFactory>();
        context.Services.AddSingleton<IKernelFunctionRegistry, KernelFunctionRegistry>();
        
        // Register web content fetcher service
        context.Services.AddHttpClient<WebContentFetcher>();
        context.Services.AddSingleton<IWebContentFetcher, WebContentFetcher>();
        
        // Register session file manager service for theory markdown export
        context.Services.AddSingleton<ISessionFileManagerService, SessionFileManagerService>();
        
        // Note: Additional search engines and providers configuration has been removed
        // as they are not essential for the configuration management functionality
    }

    // Remove OnApplicationInitialization override since we don't need Web API endpoints
}