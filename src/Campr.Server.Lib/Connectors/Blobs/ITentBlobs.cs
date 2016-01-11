using System.Threading;
using System.Threading.Tasks;

namespace Campr.Server.Lib.Connectors.Blobs
{
    public interface ITentBlobs
    {
        Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));
        IBlobContainer Attachments { get; }
    }
}