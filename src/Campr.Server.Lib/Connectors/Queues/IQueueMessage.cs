using Campr.Server.Lib.Models.Queues;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Campr.Server.Lib.Connectors.Queues
{
    public interface IQueueMessage<out T> where T : QueueMessageBase
    {
        T Content { get; }
    }
}