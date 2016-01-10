using System;
using System.Net.Http;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Net
{
    public interface IHttpRequestMessage
    {
        IHttpRequestMessage AddAccept(string mediaType);
        IHttpRequestMessage AddLink(Uri linkValue, string rel);
        IHttpRequestMessage AddCredentials(ITentHawkSignature credentials);
        IHttpRequestMessage AddContent<T>(TentPost<T> post) where T : class;
        HttpRequestMessage ToSystemMessage();
    }
}