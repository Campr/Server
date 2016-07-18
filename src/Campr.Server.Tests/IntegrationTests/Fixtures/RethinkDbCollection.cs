using Xunit;

namespace Campr.Server.Tests.IntegrationTests.Fixtures
{
    [CollectionDefinition("RethinkDb")]
    public class RethinkDbCollection : ICollectionFixture<RethinkDbFixture>
    {
    }
}