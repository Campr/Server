using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Models.Db.Factories
{
    class AttachmentFactory : IAttachmentFactory
    {
        public Attachment CreateAttachment(string digest, long size, string contentType = null)
        {
            Ensure.Argument.IsNotNull(digest, "digest");

            return new Attachment
            {
                Digest = digest,
                Size = size,
                ContentType = contentType
            };
        }
    }
}