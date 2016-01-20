using System;
using System.Collections.Generic;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;

namespace Campr.Server.TestClient
{
    public class TestExternalConfiguration : IExternalConfiguration
    {
        public string AzureQueuesConnectionString { get; } = "";
        public string AzureBlobsConnectionString { get; } = "";
        public string EncryptionKey { get; } = "";
        public EnvironmentEnum Environment { get; }
        public IEnumerable<Uri> CouchBaseServers { get; } = new [] {
            new Uri("http://localhost:8091/pools")
        };
        public bool ConfigureBucket { get; }
        public string BucketConfigurationPath { get; }
        public string BucketAdministratorUsername { get; }
        public string BucketAdministratorPassword { get; }
    }
}