﻿using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Infrastructure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Campr.Server.Lib.Connectors.Blobs.Azure
{
    class AzureBlob : IBlob
    {
        public AzureBlob(CloudBlockBlob baseBlockBlob)
        {
            Ensure.Argument.IsNotNull(baseBlockBlob, "baseBlockBlob");
            this.baseBlockBlob = baseBlockBlob;
        }

        private readonly CloudBlockBlob baseBlockBlob;

        public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.baseBlockBlob.OpenReadAsync(
                AccessCondition.GenerateIfExistsCondition(),
                null, null, cancellationToken);
        }

        public async Task<byte[]> DownloadByteArrayAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var mem = new MemoryStream())
            {
                await this.baseBlockBlob.DownloadToStreamAsync(
                    mem,
                    AccessCondition.GenerateIfExistsCondition(),
                    null, null, cancellationToken);
                return mem.ToArray();
            }
        }

        public Task UploadStreamAsync(Stream stream, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.baseBlockBlob.UploadFromStreamAsync(
                stream,
                AccessCondition.GenerateIfExistsCondition(),
                null, null, cancellationToken);
        }

        public Task UploadByteArrayAsync(byte[] data, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.baseBlockBlob.UploadFromByteArrayAsync(
                data, 0, data.Length,
                AccessCondition.GenerateIfExistsCondition(), 
                null, null, cancellationToken);
        }
    }
}