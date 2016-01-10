using System.IO;
using System.Threading.Tasks;

namespace Campr.Server.Lib.Data
{
    public interface IBlob
    {
        Task<Stream> OpenRead();
        Task<byte[]> DownloadByteArrayAsync();
        Task UploadFromByteArrayAsync(byte[] data);
    }
}