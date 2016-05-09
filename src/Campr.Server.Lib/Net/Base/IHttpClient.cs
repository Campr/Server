using System.Threading;
using System.Threading.Tasks;

namespace Campr.Server.Lib.Net.Base
{
    /// <summary>
    /// Network abstraction for Http requests.
    /// </summary>
    public interface IHttpClient
    {
        Task<IHttpResponseMessage> SendAsync(IHttpRequestMessage request, CancellationToken cancellationToken = default(CancellationToken));

        Task<T> SendAsync<T>(IHttpRequestMessage request, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
    }
} 