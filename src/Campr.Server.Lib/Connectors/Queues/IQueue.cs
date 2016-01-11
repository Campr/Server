using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Queues;

namespace Campr.Server.Lib.Connectors.Queues
{
    public interface IQueue<T> where T : QueueMessageBase
    {
        Task<IQueueMessage<T>> GetMessageAsync(TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default(CancellationToken));
        Task AddMessageAsync(T message, TimeSpan? initialVisilityDelay = null, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteMessageAsync(IQueueMessage<T> message, CancellationToken cancellationToken = default(CancellationToken));
    }
}