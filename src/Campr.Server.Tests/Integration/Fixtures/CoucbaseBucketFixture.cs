using System.Threading.Tasks;
using Campr.Server.Tests.TestInfrastructure;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Campr.Server.Tests.Integration.Fixtures
{
    public class CouchbaseBucketFixture : IAsyncLifetime
    {
        public async Task InitializeAsync()
        {
            // Resolve an instance of the bucket configurator.
            var configurator = ServiceProvider.Current.GetService<BucketConfigurator>();

            // Use it to get the Db in a clean state.
            await configurator.Reset();
        }

        public Task DisposeAsync()
        {
            return Task.FromResult(false);
        }
    }
}