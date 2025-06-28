using Aevatar.Workshop.TestBase;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Workshop.Tests;

[DependsOn(
    typeof(AevatarWorkshopTestBaseModule),
    typeof(AbpEventBusModule),
    typeof(AbpPermissionManagementDomainModule)
)]
public class AevatarWorkshopTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarWorkshopTestModule>(); });
    }
}