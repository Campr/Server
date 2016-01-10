using System.IO;
using System.Threading.Tasks;
using Campr.Server.Lib.Infrastructure;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Campr.Server.Lib.Data
{
    class Blob : IBlob
    {
        public Blob(CloudBlockBlob baseBlockBlob)
        {
            Ensure.Argument.IsNotNull(baseBlockBlob, "baseBlockBlob");
            this.baseBlockBlob = baseBlockBlob;
        }

        private readonly CloudBlockBlob baseBlockBlob;

        public Task<Stream> OpenRead()
        {
            return this.baseBlockBlob.OpenReadAsync();
        }

        public async Task<byte[]> DownloadByteArrayAsync()
        {
            using (var mem = new MemoryStream())
            {
                await this.baseBlockBlob.DownloadToStreamAsync(mem);
                return mem.ToArray();
            }
        }

        public Task UploadFromByteArrayAsync(byte[] data)
        {
            return this.baseBlockBlob.UploadFromByteArrayAsync(data, 0, data.Length);
        }
    }
}