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
            ITaskHelpers taskHelpers,
            ILoggingService loggingService)
        {
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));
            Ensure.Argument.IsNotNull(taskHelpers, nameof(taskHelpers));
            Ensure.Argument.IsNotNull(loggingService, nameof(loggingService));

            this.taskHelpers = taskHelpers;
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

            // Create the initializer for this component.
            this.initializer = new TaskRunner(this.InitializeOnceAsync);
        }
        
        private readonly TaskRunner initializer;

        private readonly CloudQueue mentionsQueue;
        private readonly CloudQueue subscriptionsQueue;
        private readonly CloudQueue appNotificationQueue;
        private readonly CloudQueue metaSubscriptionQueue;
        private readonly CloudQueue retryQueue;

        private readonly ITaskHelpers taskHelpers;
        private readonly ILoggingService loggingService;

        public Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.initializer.RunOnce(cancellationToken);
        }

        private async Task InitializeOnceAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Try to create the queues.
            try
            {
                await this.taskHelpers.RetryAsync(async () =>
                {
                    await this.mentionsQueue.CreateIfNotExistsAsync(null, null, cancellationToken);
                    await this.subscriptionsQueue.CreateIfNotExistsAsync(null, null, cancellationToken);
                    await this.appNotificationQueue.CreateIfNotExistsAsync(null, null, cancellationToken);
                    await this.metaSubscriptionQueue.CreateIfNotExistsAsync(null, null, cancellationToken);
                    await this.retryQueue.CreateIfNotExistsAsync(null, null, cancellationToken);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                this.loggingService.Exception(ex, "Error during Azure queues initialization. We won't retry.");
                throw;
            }
        }

        public IQueue<QueueMentionMessage> Mentions { get; }
        public IQueue<QueueSubscriptionMessage> Subscriptions { get; }
        public IQueue<QueueAppNotificationMessage> AppNotifications { get; }
        public IQueue<QueueMetaSubscriptionMessage> MetaSubscriptions { get; }
        public IQueue<QueueRetryMessage> Retries { get; }
    }
}