using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Queues;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Campr.Server.Lib.Connectors.Queues.Azure
{
    class AzureQueue<T> : IQueue<T> where T : QueueMessageBase
    {
        public AzureQueue(CloudQueue baseQueue, 
            IJsonHelpers jsonHelpers)
        {
            Ensure.Argument.IsNotNull(baseQueue, nameof(baseQueue));
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));

            this.baseQueue = baseQueue;
            this.jsonHelpers = jsonHelpers;
        }

        private readonly CloudQueue baseQueue;
        private readonly IJsonHelpers jsonHelpers;

        public async Task<IQueueMessage<T>> GetMessageAsync(TimeSpan? visibilityTimeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Try to retrieve a message from the underlying queue.
            var message = await this.baseQueue.GetMessageAsync(visibilityTimeout, null, null, cancellationToken);
            if (message == null)
                return null;

            // If a message was found, deserialize it.
            return new AzureQueueMessage<T>(message, this.jsonHelpers);
        }

        public async Task AddMessageAsync(T message, TimeSpan? initialVisilityDelay = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Serialize the message.
            var stringMessage = this.jsonHelpers.ToJsonString(message);

            // Create the CloudQueueMessage.
            var queueMessage = new CloudQueueMessage(stringMessage);

            // Post it to the queue.
            await this.baseQueue.AddMessageAsync(queueMessage, null, initialVisilityDelay, null, null, cancellationToken);
        }

        public async Task DeleteMessageAsync(IQueueMessage<T> message, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Only do something if this is an Azure message.
            var azureQueueMessage = message as AzureQueueMessage<T>;
            if (azureQueueMessage == null)
                return;

            await this.baseQueue.DeleteMessageAsync(
                azureQueueMessage.BaseMessage.Id,
                azureQueueMessage.BaseMessage.PopReceipt,
                null, null, cancellationToken);
        }
    }
}