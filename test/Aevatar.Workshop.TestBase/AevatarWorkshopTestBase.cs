using System.Reflection;
using Orleans.TestingHost;
using Volo.Abp.Modularity;
using Volo.Abp.Testing;

namespace Aevatar.Workshop.TestBase;

public abstract class AevatarWorkshopTestBase<TStartupModule> : AbpIntegratedTest<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    protected AevatarWorkshopTestBase() 
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}