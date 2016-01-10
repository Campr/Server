namespace Campr.Server.Lib.Models.Queues
{
    public class QueueMentionMessage : QueueMessageBase
    {
        public string UserId { get; set; }
        public string PostId { get; set; }
        public string VersionId { get; set; }
    }
}