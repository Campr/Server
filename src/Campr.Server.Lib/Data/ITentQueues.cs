using System.Threading.Tasks;
using Campr.Server.Lib.Models.Queues;

namespace Campr.Server.Lib.Data
{
    public interface ITentQueues
    {
        Task Initialize();
        IQueue<QueueMentionMessage> Mentions { get; }
        IQueue<QueueSubscriptionMessage> Subscriptions { get; }
        IQueue<QueueAppNotificationMessage> AppNotifications { get; }
        IQueue<QueueMetaSubscriptionMessage> MetaSubscriptions { get; }
        IQueue<QueueRetryMessage> Retries { get; }
    }
}