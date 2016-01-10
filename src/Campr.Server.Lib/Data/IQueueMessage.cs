using Campr.Server.Lib.Models.Queues;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Campr.Server.Lib.Data
{
    public interface IQueueMessage<out T> where T : QueueMessageBase
    {
        T Content { get; }
        CloudQueueMessage BaseMessage { get; }
    }
}