using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Campr.Server.Lib;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Connectors.Buckets;
using Campr.Server.Lib.Services;
using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using Couchbase.Core.Buckets;
using Couchbase.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Campr.Server.Couchbase
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var prog = new Program();
            prog.MainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            // Initialize dependency injection.
            var services = new ServiceCollection();
            
            // Register core components.
            services.AddSingleton<IExternalConfiguration, CouchbaseConfiguration>();
            services.AddSingleton<ILoggingService, LoggingService>();
            CamprCommonInitializer.Register(services);
            
            // Create the final container.
            this.serviceProvider = services.BuildServiceProvider();
        }

        private readonly IServiceProvider serviceProvider;

        private async Task MainAsync()
        {
            // Retrieve the full configuration.
            var bucketName = "camprdb-dev";
            var configuration = this.serviceProvider.GetService<IGeneralConfiguration>();
            var tentBuckets = this.serviceProvider.GetService<ITentBuckets>();

            // Configure the connection to the cluster.
            var cluster = new Cluster(new ClientConfiguration
            {
                Servers = configuration.CouchBaseServers.ToList()
            });

            var clusterManager = cluster.CreateManager(
                configuration.BucketAdministratorUsername,
                configuration.BucketAdministratorPassword);

            // Retrieve a list of buckets to check if we need to create a new one.
            var buckets = await clusterManager.ListBucketsAsync();

            // If needed, create the bucket.
            if (buckets.Success && buckets.Value.All(b => b.Name != bucketName))
            {
                await clusterManager.CreateBucketAsync(new BucketSettings
                {
                    Name = bucketName,
                    BucketType = BucketTypeEnum.Couchbase,
                    RamQuota = 200,
                    AuthType = AuthType.Sasl
                });

                // Allow some time for the bucket to initialize.
                await Task.Delay(1000);
            }

            // Configure the views in this bucket.
            await tentBuckets.InitializeAsync();
        }
    }
}
