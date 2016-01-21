using System.Linq;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Connectors.Buckets;
using Campr.Server.Lib.Infrastructure;
using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using Couchbase.Core.Buckets;
using Couchbase.Management;

namespace Campr.Server.Tests.Infrastructure
{
    public class BucketConfigurator
    {
        public BucketConfigurator(
            IExternalConfiguration externalConfiguration, 
            ITentBuckets tentBuckets)
        {
            Ensure.Argument.IsNotNull(externalConfiguration, nameof(externalConfiguration));
            Ensure.Argument.IsNotNull(tentBuckets, nameof(tentBuckets));

            this.externalConfiguration = externalConfiguration;
            this.tentBuckets = tentBuckets;
        }

        private readonly IExternalConfiguration externalConfiguration;
        private readonly ITentBuckets tentBuckets;
        private const string BucketName = "camprdb-test";

        public async Task Reset()
        {
            // Configure the connection to the cluster.
            var cluster = new Cluster(new ClientConfiguration
            {
                Servers = this.externalConfiguration.CouchBaseServers.ToList()
            });

            var clusterManager = cluster.CreateManager(
                this.externalConfiguration.BucketAdministratorUsername, 
                this.externalConfiguration.BucketAdministratorPassword);

            // Recreate the bucket.
            await clusterManager.RemoveBucketAsync(BucketName);
            await clusterManager.CreateBucketAsync(new BucketSettings
            {
                Name = BucketName,
                BucketType = BucketTypeEnum.Couchbase,
                AuthType = AuthType.Sasl,
                ReplicaNumber = ReplicaNumber.Zero
            });

            // Wait on the bucket to be ready.
            await Task.Delay(1000);

            // Configure views in the bucket.
            await this.tentBuckets.InitializeAsync();
        } 
    }
}