using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Net.Base
{
    public class HttpResponseMessageWrapper : IHttpResponseMessage
    {
        public HttpResponseMessageWrapper(IHttpHelpers httpHelpers, 
            HttpResponseMessage response)
        {
            Ensure.Argument.IsNotNull(httpHelpers, nameof(httpHelpers));
            Ensure.Argument.IsNotNull(response, nameof(response));

            this.httpHelpers = httpHelpers;
            this.response = response;
        }

        private readonly IHttpHelpers httpHelpers;
        private readonly HttpResponseMessage response;

        public HttpResponseHeaders Headers => this.response.Headers;
        public HttpContent Content => this.response.Content;

        public Uri FindLinkInHeader(string linkRel)
        {
            var links = this.httpHelpers.ReadLinksInHeaders(this.response.Headers, linkRel);
            return links.FirstOrDefault();
        }
    }
}