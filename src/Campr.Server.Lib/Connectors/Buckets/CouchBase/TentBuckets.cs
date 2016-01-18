using System.Linq;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;

namespace Campr.Server.Lib.Connectors.Buckets.CouchBase
{
    class TentBuckets : ITentBuckets
    {
        #region Constructor & Private fields.

        public TentBuckets(IGeneralConfiguration configuration,
            IDbJsonHelpers dbJsonHelpers)
        {
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));
            Ensure.Argument.IsNotNull(dbJsonHelpers, nameof(dbJsonHelpers));

            this.configuration = configuration;

            // Configure the CouchBase client.
            var config = new ClientConfiguration
            {
                Servers = configuration.CouchBaseServers.ToList(),
                BucketConfigs =
                {
                    { configuration.MainBucketName, new BucketConfiguration { BucketName = configuration.MainBucketName }}
                },
                Serializer = () => dbJsonHelpers
            };
            this.cluster = new Cluster(config);
        }

        private readonly IGeneralConfiguration configuration;
        private readonly ICluster cluster;
        private IBucket mainBucket;

        #endregion

        #region ITentBuckets implementation.

        // Initialize the bucket only once.
        public IBucket Main => this.mainBucket ?? (this.mainBucket = this.cluster.OpenBucket(this.configuration.MainBucketName));

        #endregion

        #region IDisposable implementation.

        public void Dispose()
        {
            this.mainBucket?.Dispose();
            this.cluster?.Dispose();
        }

        #endregion
    }
}