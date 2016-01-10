using System;

namespace Campr.Server.Lib.Net.Factories
{
    public interface IHttpRequestFactory
    {
        IHttpRequestMessage Head(Uri uri);
        IHttpRequestMessage Get(Uri uri);
        IHttpRequestMessage Post(Uri uri);
        IHttpRequestMessage Put(Uri uri);
        IHttpRequestMessage Delete(Uri uri);
    }
}