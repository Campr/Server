using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    class AttachmentRepository : BaseRepository<Attachment>, IAttachmentRepository
    {
        public AttachmentRepository(IRethinkConnection db) : base(db, db.Attachments)
        {
        }
    }
}