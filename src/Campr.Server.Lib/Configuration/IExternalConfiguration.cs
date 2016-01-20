using System;
using System.Collections.Generic;
using Campr.Server.Lib.Enums;

namespace Campr.Server.Lib.Configuration
{
    public interface IExternalConfiguration
    {
        string AzureQueuesConnectionString { get; }
        string AzureBlobsConnectionString { get; }
        string EncryptionKey { get; }
        EnvironmentEnum Environment { get; }
        IEnumerable<Uri> CouchBaseServers { get; }
        bool ConfigureBucket { get; }
        string BucketConfigurationPath { get; }
        string BucketAdministratorUsername { get; }
        string BucketAdministratorPassword { get; }
    }
}