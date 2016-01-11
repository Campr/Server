using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Queues;

namespace Campr.Server.Lib.Connectors.Queues
{
    public interface ITentQueues
    {
        Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));
        IQueue<QueueMentionMessage> Mentions { get; }
        IQueue<QueueSubscriptionMessage> Subscriptions { get; }
        IQueue<QueueAppNotificationMessage> AppNotifications { get; }
        IQueue<QueueMetaSubscriptionMessage> MetaSubscriptions { get; }
        IQueue<QueueRetryMessage> Retries { get; }
    }
}