using System.Threading.Tasks;
using Campr.Server.Lib.Data;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Repositories;

namespace Campr.Server.Lib.Logic
{
    class AttachmentLogic : IAttachmentLogic
    {
        private readonly ITentBlobs tentBlobs;
        private readonly IAttachmentRepository attachmentRepository;
        private readonly IAttachmentFactory attachmentFactory;
        private readonly ICryptoHelpers cryptoHelpers;

        public AttachmentLogic(ITentBlobs tentBlobs, 
            IAttachmentRepository attachmentRepository, 
            IAttachmentFactory attachmentFactory, 
            ICryptoHelpers cryptoHelpers)
        {
            Ensure.Argument.IsNotNull(tentBlobs, nameof(tentBlobs));
            Ensure.Argument.IsNotNull(attachmentRepository, nameof(attachmentRepository));
            Ensure.Argument.IsNotNull(attachmentFactory, nameof(attachmentFactory));
            Ensure.Argument.IsNotNull(cryptoHelpers, nameof(cryptoHelpers));

            this.tentBlobs = tentBlobs;
            this.attachmentRepository = attachmentRepository;
            this.attachmentFactory = attachmentFactory;
            this.cryptoHelpers = cryptoHelpers;
        }

        public async Task<string> SaveAttachment(byte[] data, string digest = null, string contentType = null)
        {
            // Compute the digest for this file, if needed.
            if (string.IsNullOrWhiteSpace(digest))
                digest = this.cryptoHelpers.ConvertToSha512Truncated(data, 128);

            // Try to find an existing attachment with this digest.
            if (await this.attachmentRepository.GetAttachmentAsync(digest) != null)
                return digest;

            // Save the file to the storage. 
            var blob = this.tentBlobs.Attachments.GetBlob(digest);
            await blob.UploadFromByteArrayAsync(data);
            
            // Create a new attachment entry.
            var attachment = this.attachmentFactory.CreateAttachment(digest, data.Length, contentType);
            await this.attachmentRepository.UpdateAttachmentAsync(attachment);

            return digest;
        }
    }
}