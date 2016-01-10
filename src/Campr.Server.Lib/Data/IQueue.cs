using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Queues;

namespace Campr.Server.Lib.Data
{
    public interface IQueue<T> where T : QueueMessageBase
    {
        Task<IQueueMessage<T>> GetMessageAsync(TimeSpan? visibilityTimeout = null);
        Task AddMessageAsync(T message, TimeSpan? initialVisilityDelay = null);
        Task DeleteMessageAsync(IQueueMessage<T> message);
    }
}