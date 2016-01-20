using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Services;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;

namespace Campr.Server.Lib.Connectors.Buckets.CouchBase
{
    class CouchbaseTentBuckets : ITentBuckets
    {
        public CouchbaseTentBuckets(IGeneralConfiguration configuration,
            ILoggingService loggingService,
            IDbJsonHelpers dbJsonHelpers, 
            ITextHelpers textHelpers, 
            IJsonHelpers jsonHelpers)
        {
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));
            Ensure.Argument.IsNotNull(loggingService, nameof(loggingService));
            Ensure.Argument.IsNotNull(dbJsonHelpers, nameof(dbJsonHelpers));
            Ensure.Argument.IsNotNull(textHelpers, nameof(textHelpers));
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));

            this.configuration = configuration;
            this.loggingService = loggingService;
            this.textHelpers = textHelpers;
            this.jsonHelpers = jsonHelpers;

            // Find the bucket to use.
            this.bucketName = this.GetBucketName(configuration.Environment);

            // Configure the CouchBase client.
            var config = new ClientConfiguration
            {
                Servers = configuration.CouchBaseServers.ToList(),
                BucketConfigs =
                {
                    { this.bucketName, new BucketConfiguration { BucketName = this.bucketName }}
                },
                Serializer = () => dbJsonHelpers
            };
            this.cluster = new Cluster(config);

            // Create the initializer for this component.
            this.initializer = new TaskRunner(this.InitializeOnceAsync);
        }

        private readonly IGeneralConfiguration configuration;
        private readonly ILoggingService loggingService;
        private readonly ITextHelpers textHelpers;
        private readonly IJsonHelpers jsonHelpers;

        private readonly TaskRunner initializer;

        private readonly string bucketName;
        private readonly ICluster cluster;
        private IBucket mainBucket;

        public Task InitializeAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return this.initializer.RunOnce(cancellationToken);
        }

        private async Task InitializeOnceAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                // Check if we need to do any configuration.
                if (!this.configuration.ConfigureBucket)
                    return;

                // Read the views and build the design documents.
                var designDocumentTasks = Directory
                    .GetDirectories(this.configuration.BucketConfigurationPath)
                    .Select(d => this.ReadDesignDocumentAsync(d, this.configuration.Environment == EnvironmentEnum.Production))
                    .ToList();
                await Task.WhenAll(designDocumentTasks);
                var designDocuments = designDocumentTasks.Select(t => t.Result).ToList();

                // Use a bucket manager to update the views in the bucket.
                var bucketManager = this.Main.CreateManager(this.configuration.BucketAdministratorUsername, this.configuration.BucketAdministratorPassword);

                // Create/Update the design documents.
                var upsertDesignDocumentTasks = designDocuments.Select(d => bucketManager.UpdateDesignDocumentAsync(d.Name, d.Json)).ToList();
                await Task.WhenAll(upsertDesignDocumentTasks);
            }
            catch (Exception ex)
            {
                this.loggingService.Exception(ex, "Error during Couchbase bucket initialization.");
                throw;
            }
        }

        // Initialize the bucket only once.
        public IBucket Main => this.mainBucket ?? (this.mainBucket = this.cluster.OpenBucket(this.bucketName));

        public void Dispose()
        {
            this.mainBucket?.Dispose();
            this.cluster?.Dispose();
        }

        private string GetBucketName(EnvironmentEnum environment)
        {
            switch (environment)
            {
                case EnvironmentEnum.Production:
                    return "camprdb-prod";
                case EnvironmentEnum.Test:
                    return "camprdb-test";
                default:
                    return "camprdb-dev";
            }
        }

        private async Task<DesignDocument> ReadDesignDocumentAsync(string path, bool development)
        {
            // Read the views for this design document.
            var viewTasks = Directory.GetDirectories(path).Select(this.ReadDesignDocumentViewAsync).ToList();
            await Task.WhenAll(viewTasks);
            var views = viewTasks.Select(t => t.Result).ToList();
            
            // Return a design document object with the JSON version already serialized.
            return new DesignDocument
            {
                Name = (development ? "dev_" : "") + this.textHelpers.ToJsonPropertyName(Path.GetFileName(path)),
                Json = this.jsonHelpers.ToJsonString(new
                {
                    views = views.ToDictionary(v => v.Name, v => new
                    {
                        map = v.Map,
                        reduce = v.Reduce
                    })
                })
            };
        }

        private async Task<DesignDocumentView> ReadDesignDocumentViewAsync(string path)
        {
            return new DesignDocumentView
            {
                Name = this.textHelpers.ToJsonPropertyName(Path.GetFileName(path)),
                Map = await this.ReadFileAsync(Path.Combine(path, "Map.js")),
                Reduce = await this.ReadFileAsync(Path.Combine(path, "Reduce.js"))
            };
        }

        private async Task<string> ReadFileAsync(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (var reader = new StreamReader(fileStream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private class DesignDocument
        {
            public string Name { get; set; }
            public string Json { get; set; }
        }

        private class DesignDocumentView
        {
            public string Name { get; set; }
            public string Map { get; set; }
            public string Reduce { get; set; }
        }
    }
}