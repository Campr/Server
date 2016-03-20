using Campr.Server.Lib.Enums;

namespace Campr.Server.Lib.Models.Queues
{ 
    public class QueueRetryMessage : QueueMessageBase
    {
        public string OwnerId { get; set; }
        public string UserId { get; set; }
        public string PostId { get; set; }
        public string VersionId { get; set; }
        public RetryNotificationType NotificationType { get; set; }
        public string TargetId { get; set; }
        public string DeliveryFailureId { get; set; }
        public int Retries { get; set; }
    }
}