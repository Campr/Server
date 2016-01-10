using System.Threading.Tasks;
using Campr.Server.Lib.Data;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    class AttachmentRepository : IAttachmentRepository
    {
        public AttachmentRepository(IDbClient client)
        {
            Ensure.Argument.IsNotNull(client, nameof(client));
            this.client = client;
            this.prefix = "attachment_";
        }

        private readonly IDbClient client;
        private readonly string prefix;

        public async Task<Attachment> GetAttachmentAsync(string digest)
        {
            using (var bucket = this.client.GetBucket())
            {
                var operation = bucket.Get<Attachment>(this.prefix + digest);
                return operation.Value;
            }
        }

        public async Task UpdateAttachmentAsync(Attachment attachment)
        {
            using (var bucket = this.client.GetBucket())
            {
                bucket.Upsert(this.prefix + attachment.Digest, attachment);
            }
        }
    }
}