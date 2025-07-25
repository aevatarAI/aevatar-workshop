namespace Aevatar.Workshop.TestBase;

[CollectionDefinition(Name)]
public class ClusterCollection : ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}