using Campr.Server.Lib.Configuration;

namespace Campr.Server.TestClient
{
    public class TestSensitiveConfiguration : ISensitiveConfiguration
    {
        public string AzureQueuesConnectionString { get; } = "";
        public string AzureBlobsConnectionString { get; } = "";
        public string EncryptionKey { get; } = "";
    }
}