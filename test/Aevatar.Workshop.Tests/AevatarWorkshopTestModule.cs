using Aevatar.GAgents.Executor;
using Aevatar.Workshop.GAgent.Services;
using Aevatar.Workshop.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BlobStoring;
using Volo.Abp.Modularity;

namespace Aevatar.Workshop.Tests;

[DependsOn(
    typeof(AevatarModule),
    typeof(AevatarWorkshopTestBaseModule)
)]
public class AevatarWorkshopTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Register test services
        context.Services.AddSingleton<IGAgentExecutor, GAgentExecutor>();
        context.Services.AddSingleton<IGAgentService, GAgentService>();
        context.Services.AddSingleton<IBlobContainer, MockBlobContainer>();
        
        // Register session file manager service for testing
        context.Services.AddSingleton<ISessionFileManagerService, SessionFileManagerService>();
    }
}