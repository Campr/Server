using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Queues;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Campr.Server.Lib.Connectors.Queues.Azure
{
    class AzureQueueMessage<T> : IQueueMessage<T> where T : QueueMessageBase
    {
        public AzureQueueMessage(CloudQueueMessage baseMessage, 
            IJsonHelpers jsonHelpers)
        {
            Ensure.Argument.IsNotNull(baseMessage, nameof(baseMessage));
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));

            this.BaseMessage = baseMessage;
            this.Content = jsonHelpers.FromJsonString<T>(this.BaseMessage.AsString);
        }

        public CloudQueueMessage BaseMessage { get; }
        public T Content { get; }
    }
}