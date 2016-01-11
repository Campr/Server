using System;
using System.Linq;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Infrastructure;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;

namespace Campr.Server.Lib.Connectors.Buckets.CouchBase
{
    class TentBuckets : ITentBuckets
    {
        public TentBuckets(IGeneralConfiguration configuration)
        {
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));
            this.configuration = configuration;

            // Configure the CouchBase client.
            var config = new ClientConfiguration
            {
                Servers = configuration.CouchBaseServers.ToList(),
                BucketConfigs =
                {
                    { configuration.MainBucketName, new BucketConfiguration { BucketName = configuration.MainBucketName }}
                }
            };
            this.cluster = new Cluster(config);
        }

        private readonly IGeneralConfiguration configuration;
        private readonly ICluster cluster;
        private IBucket mainBucket;

        // Initialize the bucket only once.
        public IBucket Main => this.mainBucket ?? (this.mainBucket = this.cluster.OpenBucket(this.configuration.MainBucketName));

        public void Dispose()
        {
            this.mainBucket?.Dispose();
            this.cluster?.Dispose();
        }
    }
}