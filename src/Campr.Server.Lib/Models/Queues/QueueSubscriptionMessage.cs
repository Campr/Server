namespace Campr.Server.Lib.Models.Queues
{
    public class QueueSubscriptionMessage : QueueMessageBase
    {
        public string UserId { get; set; }
        public string PostId { get; set; }
        public string VersionId { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}