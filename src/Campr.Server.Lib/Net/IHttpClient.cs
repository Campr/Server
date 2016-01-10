using System;
using System.Threading.Tasks;

namespace Campr.Server.Lib.Net
{
    /// <summary>
    /// Network abstraction for Http requests.
    /// </summary>
    public interface IHttpClient
    {
        Task<IHttpResponseMessage> SendAsync(IHttpRequestMessage request);

        Task<T> SendAsync<T>(IHttpRequestMessage request) where T : class;

        TimeSpan Timeout { get; set; }
    }
}