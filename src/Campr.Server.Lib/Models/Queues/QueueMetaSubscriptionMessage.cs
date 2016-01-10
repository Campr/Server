namespace Campr.Server.Lib.Models.Queues
{
    public class QueueMetaSubscriptionMessage : QueueMessageBase
    {
        public string UserId { get; set; }
        public string TargetUserId { get; set; }
    }
}