namespace Campr.Server.Lib.Models.Queues
{
    public class QueueAppNotificationMessage : QueueMessageBase
    {
        public string OwnerId { get; set; }
        public string UserId { get; set; }
        public string PostId { get; set; }
        public string VersionId { get; set; }
    }
}