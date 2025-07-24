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
using Aevatar.Workshop.Host.Options;
using Aevatar.Workshop.Host.Services;
using Aevatar.Workshop.GAgent;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Volo.Abp;

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
        
        // Configure MCPServerOptions
        context.Services.Configure<MCPServerOptions>(options =>
        {
            var mcpServers = new Dictionary<string, MCPServerConfig>();
            
            // Load from appsettings.json
            var section = configuration.GetSection("MCPServers");
            if (section.Exists())
            {
                var configs = section.Get<Dictionary<string, MCPServerConfig>>();
                if (configs != null)
                {
                    foreach (var kvp in configs)
                    {
                        mcpServers[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            options.MCPServers = mcpServers;
            
            // Log loaded MCP server configurations
            Log.Information("Loaded {Count} MCPServers:", mcpServers.Count);
            foreach (var kvp in mcpServers)
            {
                Log.Information("  - {Key}: {Command} ({Description})", 
                    kvp.Key, 
                    kvp.Value.Command,
                    kvp.Value.Description ?? "No description");
            }
        });
        
        // Register configuration services
        context.Services.AddSingleton<IConfigurationUpdateService, ConfigurationUpdateService>();
        context.Services.AddSingleton<IConfigurationHandler, WorkshopConfigurationHandler>();
        
        context.Services.AddSemanticKernel();
        context.Services.AddSingleton<IKernelFactory, KernelFactory>();
        context.Services.AddSingleton<IKernelFunctionRegistry, KernelFunctionRegistry>();
        
        // Register web content fetcher service
        context.Services.AddHttpClient<WebContentFetcher>();
        context.Services.AddSingleton<IWebContentFetcher, WebContentFetcher>();
        
        // Note: Additional search engines and providers configuration has been removed
        // as they are not essential for the configuration management functionality
    }

    // Remove OnApplicationInitialization override since we don't need Web API endpoints
}