using Campr.Server.Lib.Models.Queues;

namespace Campr.Server.Lib.Connectors.Queues
{
    public interface IQueueMessage<out T> where T : QueueMessageBase
    {
        T Content { get; }
    }
}