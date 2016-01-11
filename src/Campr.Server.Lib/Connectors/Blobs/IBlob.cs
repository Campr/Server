using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Campr.Server.Lib.Connectors.Blobs
{
    public interface IBlob
    {
        Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<byte[]> DownloadByteArrayAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task UploadStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken));
        Task UploadByteArrayAsync(byte[] data, CancellationToken cancellationToken = default(CancellationToken));
    }
}