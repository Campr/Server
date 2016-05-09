using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Net.Base
{
    public class HttpRequestMessageWrapper : IHttpRequestMessage
    {
        public HttpRequestMessageWrapper(IJsonHelpers jsonHelpers,
            ITentConstants tentConstants, 
            HttpRequestMessage request)
        {
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));
            Ensure.Argument.IsNotNull(tentConstants, nameof(tentConstants));
            Ensure.Argument.IsNotNull(request, nameof(request));

            this.jsonHelpers = jsonHelpers;
            this.tentConstants = tentConstants;
            this.request = request;
        }

        private readonly IJsonHelpers jsonHelpers;
        private readonly ITentConstants tentConstants;
        private readonly HttpRequestMessage request;

        public IHttpRequestMessage AddAccept(string mediaType)
        {
            Ensure.Argument.IsNotNullOrWhiteSpace(mediaType, "mediaType");

            // Add a new accept header to the underlying request.
            this.request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            return this;
        }

        public IHttpRequestMessage AddLink(Uri linkValue, string rel)
        {
            Ensure.Argument.IsNotNull(linkValue, "linkValue");
            Ensure.Argument.IsNotNullOrWhiteSpace(rel, "rel");

            // Add a new link header to the underlying request.
            this.request.Headers.Add("Link", string.Format(
                CultureInfo.InvariantCulture,
                "<{0}>; rel=\"{1}\"",
                linkValue.AbsoluteUri,
                rel));

            return this;
        }

        public IHttpRequestMessage AddCredentials(ITentHawkSignature credentials)
        {
            Ensure.Argument.IsNotNull(credentials, "credentials");

            // Use the provided credentials to create the "Authorization" header for this request.
            var authorizationHeaderValue = credentials.ToHeader(this.request.Method.ToString().ToUpper(), this.request.RequestUri);
            this.request.Headers.Authorization = new AuthenticationHeaderValue("Hawk", authorizationHeaderValue);

            return this;
        }

        public IHttpRequestMessage AddContent(object content, string contentType = null)
        {
            return this.AddContent(content, string.IsNullOrWhiteSpace(contentType)
                ? null
                : new MediaTypeHeaderValue(contentType));
        }

        public IHttpRequestMessage AddContent<T>(TentPost<T> post) where T : class
        {
            return this.AddContent(post, new MediaTypeHeaderValue(this.tentConstants.PostContentType)
            {
                Parameters =
                {
                    new NameValueHeaderValue("type", string.Format(CultureInfo.InvariantCulture, "\"{0}\"", post.Type)),
                    new NameValueHeaderValue("rel", string.Format(CultureInfo.InvariantCulture, "\"{0}\"", this.tentConstants.NotificationRel))
                }
            });
        }

        private IHttpRequestMessage AddContent(object content, MediaTypeHeaderValue contentType = null)
        {
            // Serialize the content and add it to the request.
            var jsonContent = this.jsonHelpers.ToJsonString(content);
            this.request.Content = new StringContent(jsonContent);

            // Specify the ContentType.
            this.request.Content.Headers.ContentType = 
                contentType 
                ?? new MediaTypeHeaderValue(this.tentConstants.JsonContentType); 

            return this;
        }

        public HttpRequestMessage ToSystemMessage()
        {
            return this.request;
        }
    }
}