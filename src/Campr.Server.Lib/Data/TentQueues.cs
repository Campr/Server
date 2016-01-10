using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Queues;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Campr.Server.Lib.Data
{
    class TentQueues : ITentQueues
    {
        public TentQueues(ITentServConfiguration configuration, IJsonHelpers jsonHelpers)
        {
            Ensure.Argument.IsNotNull(configuration, "configuration");
            Ensure.Argument.IsNotNull(jsonHelpers, "jsonHelpers");
            
            // Create the storage account from the connection string, and the corresponding client.
            var queuesStorageAccount = CloudStorageAccount.Parse(configuration.QueuesConnectionString());
            var queuesClient = queuesStorageAccount.CreateCloudQueueClient();

            // Create the queues references.
            this.mentionsQueue = queuesClient.GetQueueReference("mentions");
            this.subscriptionsQueue = queuesClient.GetQueueReference("subscriptions");
            this.appNotificationQueue = queuesClient.GetQueueReference("appnotifications");
            this.metaSubscriptionQueue = queuesClient.GetQueueReference("metasubscriptions");
            this.retryQueue = queuesClient.GetQueueReference("retries");

            // Create the IQueue objects.
            this.Mentions = new Queue<QueueMentionMessage>(this.mentionsQueue, jsonHelpers);
            this.Subscriptions = new Queue<QueueSubscriptionMessage>(this.subscriptionsQueue, jsonHelpers);
            this.AppNotifications = new Queue<QueueAppNotificationMessage>(this.appNotificationQueue, jsonHelpers);
            this.MetaSubscriptions = new Queue<QueueMetaSubscriptionMessage>(this.metaSubscriptionQueue, jsonHelpers);
            this.Retries = new Queue<QueueRetryMessage>(this.retryQueue, jsonHelpers);
        }
        
        private bool initialized;
        private readonly AsyncLock initializeLock = new AsyncLock();

        private readonly CloudQueue mentionsQueue;
        private readonly CloudQueue subscriptionsQueue;
        private readonly CloudQueue appNotificationQueue;
        private readonly CloudQueue metaSubscriptionQueue;
        private readonly CloudQueue retryQueue;

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
                    await this.mentionsQueue.CreateIfNotExistsAsync();
                    await this.subscriptionsQueue.CreateIfNotExistsAsync();
                    await this.appNotificationQueue.CreateIfNotExistsAsync();
                    await this.metaSubscriptionQueue.CreateIfNotExistsAsync();
                    await this.retryQueue.CreateIfNotExistsAsync();
                    this.initialized = true;
                }
                catch (Exception)
                {
                    // TODO: Log this.
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