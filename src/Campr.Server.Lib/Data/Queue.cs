using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Queues;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Campr.Server.Lib.Data
{
    class Queue<T> : IQueue<T> where T : QueueMessageBase
    {
        public Queue(CloudQueue baseQueue, 
            IJsonHelpers jsonHelpers)
        {
            Ensure.Argument.IsNotNull(baseQueue, nameof(baseQueue));
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));

            this.baseQueue = baseQueue;
            this.jsonHelpers = jsonHelpers;
        }

        private readonly CloudQueue baseQueue;
        private readonly IJsonHelpers jsonHelpers;

        public async Task<IQueueMessage<T>> GetMessageAsync(TimeSpan? visibilityTimeout)
        {
            // Try to retrieve a message from the underlying queue.
            var message = await this.baseQueue.GetMessageAsync(visibilityTimeout, null, null);
            if (message == null)
            {
                return null;
            }

            // If a message was found, deserialize it.
            var result = new QueueMessage<T>(message, this.jsonHelpers);
            await result.ReadContent();
            return result;
        }

        public async Task AddMessageAsync(T message, TimeSpan? initialVisilityDelay = null)
        {
            // Serialize the message.
            var stringMessage = this.jsonHelpers.ToJsonString(message);

            // Create the CloudQueueMessage.
            var queueMessage = new CloudQueueMessage(stringMessage);

            // Post it to the queue.
            await this.baseQueue.AddMessageAsync(queueMessage, null, initialVisilityDelay, null, null);
        }

        public Task DeleteMessageAsync(IQueueMessage<T> message)
        {
            return this.baseQueue.DeleteMessageAsync(message.BaseMessage);
        }
    }
}