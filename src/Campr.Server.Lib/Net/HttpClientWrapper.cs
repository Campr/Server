using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Net.Exceptions;

namespace Campr.Server.Lib.Net
{
    public class HttpClientWrapper : IHttpClient
    {
        public HttpClientWrapper(IJsonHelpers jsonHelpers, 
            IHttpHelpers httpHelpers)
        {
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));
            Ensure.Argument.IsNotNull(httpHelpers, nameof(httpHelpers));

            this.jsonHelpers = jsonHelpers;
            this.httpHelpers = httpHelpers;
            this.baseClient = new HttpClient();
        }

        private readonly HttpClient baseClient;
        private readonly IJsonHelpers jsonHelpers;
        private readonly IHttpHelpers httpHelpers;
        
        public async Task<T> SendAsync<T>(IHttpRequestMessage request) where T : class
        {
            var httpResponse = await this.SendAsync(request);
            if (httpResponse?.Content == null)
            {
                throw new HttpNoContentException();
            }

            var contentJsonString = await httpResponse.Content.ReadAsStringAsync();
            return this.jsonHelpers.FromJsonString<T>(contentJsonString);
        }

        public TimeSpan Timeout
        {
            get { return this.baseClient.Timeout; }
            set { this.baseClient.Timeout = value; }
        }

        public Task<IHttpResponseMessage> SendAsync(IHttpRequestMessage request)
        {
            return this.SendAsync(request, true);
        }

        private async Task<IHttpResponseMessage> SendAsync(IHttpRequestMessage request, bool canAdjustTimeOffset)
        {
            var httpRequest = request.ToSystemMessage();

            // Temporary code for validator.
            if (httpRequest.RequestUri.Host == "localhost")
            {
                var uriBuilder = new UriBuilder(httpRequest.RequestUri)
                {
                    Host = "tenthost.com"
                };
                httpRequest.RequestUri = uriBuilder.Uri;
            }

            // Perform the request.
            var httpResponseMessage = await this.baseClient.SendAsync(request.ToSystemMessage(), HttpCompletionOption.ResponseHeadersRead);

            // If the request succeeded, return.
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return new HttpResponseMessageWrapper(this.httpHelpers, httpResponseMessage);
            }

            if (httpResponseMessage.Content == null)
            {
                return null;
            }

            Debug.WriteLine(await httpResponseMessage.Content.ReadAsStringAsync());

            // TODO: Add time skew adjustment.

            return null;
        }
    }
}
