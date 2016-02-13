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
            ITaskHelpers taskHelpers,
            ILoggingService loggingService)
        {
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));
            Ensure.Argument.IsNotNull(taskHelpers, nameof(taskHelpers));
            Ensure.Argument.IsNotNull(loggingService, nameof(loggingService));

            this.taskHelpers = taskHelpers;
            this.loggingService = loggingService;

            // Create the storage account from the connection stirng, and the corresponding client.
            var blobsStorageAccount = CloudStorageAccount.Parse(configuration.AzureBlobsConnectionString);
            var blobsClient = blobsStorageAccount.CreateCloudBlobClient();

            // Create the blob container references.
            this.attachmentsContainer = blobsClient.GetContainerReference("attachments");

            // Create IBlobContainer objects.
            this.Attachments = new AzureBlobContainer(this.attachmentsContainer);

            // Create the initializer for this component.
            this.initializer = new TaskRunner(this.InitializeOnceAsync);
        }

        private readonly TaskRunner initializer;
        private readonly CloudBlobContainer attachmentsContainer;

        private readonly ITaskHelpers taskHelpers;
        private readonly ILoggingService loggingService;
        
        public Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.initializer.RunOnce(cancellationToken);
        }

        private async Task InitializeOnceAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Try to create the containers.
            try
            {
                await this.taskHelpers.RetryAsync(() =>
                    this.attachmentsContainer.CreateIfNotExistsAsync(
                        BlobContainerPublicAccessType.Off, 
                        null, null, cancellationToken),
                        cancellationToken);
            }
            catch (Exception ex)
            {
                this.loggingService.Exception(ex, "Error during Azure blob initialization. We won't retry.");
                throw;
            }
        }

        public IBlobContainer Attachments { get; }
    }
}