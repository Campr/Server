using Campr.Server.Lib.Connectors.Buckets;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    class AttachmentRepository : BaseRepository<Attachment>, IAttachmentRepository
    {
        public AttachmentRepository(ITentBuckets buckets) : base(buckets, "attachments")
        {
        }
    }
}