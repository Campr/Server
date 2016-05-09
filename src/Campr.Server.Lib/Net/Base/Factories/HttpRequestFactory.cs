using System;
using System.Net.Http;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Net.Base.Factories
{
    class HttpRequestFactory : IHttpRequestFactory
    {
        public HttpRequestFactory(IJsonHelpers jsonHelpers,
            ITentConstants tentConstants)
        {
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));
            Ensure.Argument.IsNotNull(tentConstants, nameof(tentConstants));

            this.jsonHelpers = jsonHelpers;
            this.tentConstants = tentConstants;
        }

        private readonly IJsonHelpers jsonHelpers;
        private readonly ITentConstants tentConstants;
        
        public IHttpRequestMessage Head(Uri uri)
        {
            return this.CreateRequestMessageBase(HttpMethod.Head, uri);
        }

        public IHttpRequestMessage Get(Uri uri)
        {
            return this.CreateRequestMessageBase(HttpMethod.Get, uri);
        }

        public IHttpRequestMessage Post(Uri uri)
        {
            return this.CreateRequestMessageBase(HttpMethod.Post, uri);
        }

        public IHttpRequestMessage Put(Uri uri)
        {
            return this.CreateRequestMessageBase(HttpMethod.Put, uri);
        }

        public IHttpRequestMessage Delete(Uri uri)
        {
            return this.CreateRequestMessageBase(HttpMethod.Delete, uri);
        }

        private IHttpRequestMessage CreateRequestMessageBase(HttpMethod method, Uri uri)
        {
            return new HttpRequestMessageWrapper(this.jsonHelpers, this.tentConstants, new HttpRequestMessage
            {
                Method = method,
                RequestUri = uri
            });
        }
    }
}