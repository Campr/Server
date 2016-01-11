using System;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Json;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Newtonsoft.Json;

namespace Campr.Server.Lib.Data
{
    class DbClient : IDbClient, IDisposable
    {
        public DbClient(IDbContractResolver dbContractResolver, 
            IGeneralConfiguration configuration)
        {
            // Check arguments and assign local variables.
            Ensure.Argument.IsNotNull(dbContractResolver, nameof(dbContractResolver));
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));

            // Configure the CouchBase client.
            var config = new ClientConfiguration
            { 
                Servers =
                {
                    new Uri("http://localhost:8091/pools")
                },
                BucketConfigs = 
                {
                    { "camprdb_dev", new BucketConfiguration { BucketName = "camprdb_dev" }}
                },
                SerializationSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = dbContractResolver
                },
                DeserializationSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = dbContractResolver
                }
            };

            // Create the CouchBase cluster that we'll connect to.
            this.cluster = new Cluster(config);
        }

        private readonly Cluster cluster;

        public IBucket GetBucket()
        {
            // Return the default bucket.
            return this.cluster.OpenBucket("camprdb_dev", "CamprDbPass");
        }

        public void Dispose()
        {
            // Dispose the underlying cluster.
            this.cluster.Dispose();
        }
    }
}