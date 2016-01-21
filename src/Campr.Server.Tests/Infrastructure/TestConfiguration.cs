using System;
using System.Collections.Generic;
using System.IO;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;

namespace Campr.Server.Tests.Infrastructure
{
    public class TestConfiguration : IExternalConfiguration
    {
        public string AzureQueuesConnectionString { get; } = "";
        public string AzureBlobsConnectionString { get; } = "";
        public string EncryptionKey { get; } = "";
        public EnvironmentEnum Environment { get; } = EnvironmentEnum.Test;

        public IEnumerable<Uri> CouchBaseServers { get; } = new []
        {
            new Uri("http://localhost:8091/pools")
        };

        public bool ConfigureBucket { get; } = true;
        public string BucketConfigurationPath { get; } = Path.Combine("..", "..", "couchbase", "DesignDocuments");
        public string BucketAdministratorUsername { get; } = "Administrator";
        public string BucketAdministratorPassword { get; } = "CbPass";
    }
}