using System.Threading.Tasks;
using Campr.Server.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Campr.Server.Tests.IntegrationTests.Fixtures
{
    public class RethinkDbFixture : IAsyncLifetime
    {
        public async Task InitializeAsync()
        {
            // Resolve an instance of the bucket configurator.
            var configurator = ServiceProvider.Current.GetService<DbConfigurator>();

            // Use it to get the Db in a clean state.
            await configurator.Reset();
        }

        public Task DisposeAsync()
        {
            return Task.FromResult(false);
        }
    }
}