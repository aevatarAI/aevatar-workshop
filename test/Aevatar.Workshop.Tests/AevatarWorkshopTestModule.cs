using Aevatar.GAgents.Executor;
using Aevatar.Workshop.GAgent;
using Aevatar.Workshop.TestBase;
using Microsoft.Extensions.DependencyInjection;
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

        // Register mock configuration handler
        context.Services.AddSingleton<IConfigurationHandler, MockConfigurationHandler>();
    }
}