using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Campr.Server.Lib.Net
{
    /// <summary>
    ///     The response of an Http request.
    /// </summary>
    public interface IHttpResponseMessage
    {
        HttpResponseHeaders Headers { get; }
        HttpContent Content { get; }

        Uri FindLinkInHeader(string linkRel);
    }
}