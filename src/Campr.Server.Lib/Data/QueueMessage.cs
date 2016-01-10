using System.Threading.Tasks;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Queues;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Campr.Server.Lib.Data
{
    class QueueMessage<T> : IQueueMessage<T> where T : QueueMessageBase
    {
        public QueueMessage(CloudQueueMessage baseMessage, 
            IJsonHelpers jsonHelpers)
        {
            Ensure.Argument.IsNotNull(baseMessage, nameof(baseMessage));
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));

            this.baseMessage = baseMessage;
            this.jsonHelpers = jsonHelpers;
        }

        private readonly CloudQueueMessage baseMessage;
        private readonly IJsonHelpers jsonHelpers;

        public async Task ReadContent()
        {
            this.Content = this.jsonHelpers.FromJsonString<T>(this.baseMessage.AsString);
        }

        public T Content { get; private set; }

        public CloudQueueMessage BaseMessage => this.baseMessage;
    }
}