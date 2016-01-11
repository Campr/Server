using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Campr.Server.Lib.Connectors.Blobs.Azure
{
    class AzureTentBlobs : ITentBlobs
    {
        public AzureTentBlobs(IGeneralConfiguration configuration,
            IRetryHelpers retryHelpers,
            ILoggingService loggingService)
        {
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));
            Ensure.Argument.IsNotNull(retryHelpers, nameof(retryHelpers));
            Ensure.Argument.IsNotNull(loggingService, nameof(loggingService));

            this.retryHelpers = retryHelpers;
            this.loggingService = loggingService;

            // Create the storage account from the connection stirng, and the corresponding client.
            var blobsStorageAccount = CloudStorageAccount.Parse(configuration.AzureBlobsConnectionString);
            var blobsClient = blobsStorageAccount.CreateCloudBlobClient();

            // Create the blob container references.
            this.attachmentsContainer = blobsClient.GetContainerReference("attachments");

            // Create IBlobContainer objects.
            this.Attachments = new AzureBlobContainer(this.attachmentsContainer);
        }

        private bool initialized;
        private readonly AsyncLock initializeLock = new AsyncLock();
        private readonly CloudBlobContainer attachmentsContainer;

        private readonly IRetryHelpers retryHelpers;
        private readonly ILoggingService loggingService;
        
        public async Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Make sure this is not executed in parallel.
            using (await this.initializeLock.LockAsync(cancellationToken))
            {
                // If this instance of TentBlobs was already initialized, return.
                if (this.initialized)
                    return;
                
                // Try to create the containers.
                try
                {
                    await this.retryHelpers.RetryAsync(() => 
                        this.attachmentsContainer.CreateIfNotExistsAsync(cancellationToken), cancellationToken);

                    this.initialized = true;
                }
                catch (Exception ex)
                {
                    this.loggingService.Exception(ex, "Error during Azure blobb initialization. We won't retry.");
                }
            }
        }

        public IBlobContainer Attachments { get; }
    }
}