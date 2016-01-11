namespace Campr.Server.Lib.Configuration
{
    public interface ISensitiveConfiguration
    {
        string AzureQueuesConnectionString { get; }
        string AzureBlobsConnectionString { get; }
        string EncryptionKey { get; }
    }
}