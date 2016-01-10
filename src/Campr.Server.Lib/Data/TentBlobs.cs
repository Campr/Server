using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Infrastructure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Campr.Server.Lib.Data
{
    class TentBlobs : ITentBlobs
    {
        public TentBlobs(ITentServConfiguration configuration)
        {
            Ensure.Argument.IsNotNull(configuration, "configuration");

            // Create the storage account from the connection stirng, and the corresponding client.
            var blobsStorageAccount = CloudStorageAccount.Parse(configuration.BlobsConnectionString());
            var blobsClient = blobsStorageAccount.CreateCloudBlobClient();

            // Create the blob container references.
            this.attachmentsContainer = blobsClient.GetContainerReference("attachments");

            // Create IBlobContainer objects.
            this.Attachments = new BlobContainer(this.attachmentsContainer);
        }

        private bool initialized;
        private readonly AsyncLock initializeLock = new AsyncLock();
        private readonly CloudBlobContainer attachmentsContainer;
        
        public async Task Initialize()
        {
            // Make sure this is not executed in parallel.
            using (await this.initializeLock.LockAsync())
            {
                // If this instance of TentQueues was already initialized, return.
                if (this.initialized)
                {
                    return;
                }

                // Try to create the Queues.
                try
                {
                    await this.attachmentsContainer.CreateIfNotExistsAsync();
                    this.initialized = true;
                }
                catch (Exception)
                {
                }
            }
        }

        public IBlobContainer Attachments { get; }
    }
}