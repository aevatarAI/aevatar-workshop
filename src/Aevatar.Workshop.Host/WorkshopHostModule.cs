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