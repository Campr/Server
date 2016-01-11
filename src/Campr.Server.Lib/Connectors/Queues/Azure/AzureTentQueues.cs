using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Queues;
using Campr.Server.Lib.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Campr.Server.Lib.Connectors.Queues.Azure
{
    class AzureTentQueues : ITentQueues
    {
        public AzureTentQueues(IGeneralConfiguration configuration, 
            IJsonHelpers jsonHelpers,
            IRetryHelpers retryHelpers,
            ILoggingService loggingService)
        {
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));
            Ensure.Argument.IsNotNull(retryHelpers, nameof(retryHelpers));
            Ensure.Argument.IsNotNull(loggingService, nameof(loggingService));

            this.retryHelpers = retryHelpers;
            this.loggingService = loggingService;

            // Create the storage account from the connection string, and the corresponding client.
            var queuesStorageAccount = CloudStorageAccount.Parse(configuration.AzureQueuesConnectionString);
            var queuesClient = queuesStorageAccount.CreateCloudQueueClient();

            // Create the queues references.
            this.mentionsQueue = queuesClient.GetQueueReference("mentions");
            this.subscriptionsQueue = queuesClient.GetQueueReference("subscriptions");
            this.appNotificationQueue = queuesClient.GetQueueReference("appnotifications");
            this.metaSubscriptionQueue = queuesClient.GetQueueReference("metasubscriptions");
            this.retryQueue = queuesClient.GetQueueReference("retries");

            // Create the IQueue objects.
            this.Mentions = new AzureQueue<QueueMentionMessage>(this.mentionsQueue, jsonHelpers);
            this.Subscriptions = new AzureQueue<QueueSubscriptionMessage>(this.subscriptionsQueue, jsonHelpers);
            this.AppNotifications = new AzureQueue<QueueAppNotificationMessage>(this.appNotificationQueue, jsonHelpers);
            this.MetaSubscriptions = new AzureQueue<QueueMetaSubscriptionMessage>(this.metaSubscriptionQueue, jsonHelpers);
            this.Retries = new AzureQueue<QueueRetryMessage>(this.retryQueue, jsonHelpers);
        }
        
        private bool initialized;
        private readonly AsyncLock initializeLock = new AsyncLock();

        private readonly CloudQueue mentionsQueue;
        private readonly CloudQueue subscriptionsQueue;
        private readonly CloudQueue appNotificationQueue;
        private readonly CloudQueue metaSubscriptionQueue;
        private readonly CloudQueue retryQueue;

        private readonly IRetryHelpers retryHelpers;
        private readonly ILoggingService loggingService;

        public async Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Make sure this is not executed in parallel.
            using (await this.initializeLock.LockAsync(cancellationToken))
            {
                // If this instance of TentQueues was already initialized, return.
                if (this.initialized)
                    return;

                // Try to create the Queues.
                try
                {
                    await this.retryHelpers.RetryAsync(async () =>
                    {
                        await this.mentionsQueue.CreateIfNotExistsAsync(cancellationToken);
                        await this.subscriptionsQueue.CreateIfNotExistsAsync(cancellationToken);
                        await this.appNotificationQueue.CreateIfNotExistsAsync(cancellationToken);
                        await this.metaSubscriptionQueue.CreateIfNotExistsAsync(cancellationToken);
                        await this.retryQueue.CreateIfNotExistsAsync(cancellationToken);
                    }, cancellationToken);

                    this.initialized = true;
                }
                catch (Exception ex)
                {
                    this.loggingService.Exception(ex, "Error during Azure queues initialization. We won't retry.");
                }
            }
        }

        public IQueue<QueueMentionMessage> Mentions { get; }
        public IQueue<QueueSubscriptionMessage> Subscriptions { get; }
        public IQueue<QueueAppNotificationMessage> AppNotifications { get; }
        public IQueue<QueueMetaSubscriptionMessage> MetaSubscriptions { get; }
        public IQueue<QueueRetryMessage> Retries { get; }
    }
}