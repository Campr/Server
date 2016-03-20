using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Queues;

namespace Campr.Server.Lib.Connectors.Queues
{
    public interface IQueue<T> where T : QueueMessageBase
    {
        Task<IQueueMessage<T>> GetMessageAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<IQueueMessage<T>> GetMessageAsync(TimeSpan? visibilityTimeout, CancellationToken cancellationToken = default(CancellationToken));
        Task AddMessageAsync(T message, CancellationToken cancellationToken = default(CancellationToken));
        Task AddMessageAsync(T message, TimeSpan? initialVisilityDelay, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteMessageAsync(IQueueMessage<T> message, CancellationToken cancellationToken = default(CancellationToken));
    }
}