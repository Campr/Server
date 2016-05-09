using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Net.Base.Exceptions;

namespace Campr.Server.Lib.Net.Base
{
    public class HttpClientWrapper : IHttpClient
    {
        public HttpClientWrapper(
            IJsonHelpers jsonHelpers, 
            IHttpHelpers httpHelpers,
            TimeSpan? timeout = null)
        {
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));
            Ensure.Argument.IsNotNull(httpHelpers, nameof(httpHelpers));

            this.jsonHelpers = jsonHelpers;
            this.httpHelpers = httpHelpers;

            this.baseClient = new HttpClient();
            this.timeout = timeout;
        }

        private readonly IJsonHelpers jsonHelpers;
        private readonly IHttpHelpers httpHelpers;

        private readonly HttpClient baseClient;
        private readonly TimeSpan? timeout;

        #region Public interface.

        public async Task<IHttpResponseMessage> SendAsync(IHttpRequestMessage request, CancellationToken cancellationToken = default(CancellationToken))
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
            var httpResponseMessage = await this.baseClient.SendAsync(
                request.ToSystemMessage(),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            // If the request succeeded, return.
            if (httpResponseMessage.IsSuccessStatusCode)
                return new HttpResponseMessageWrapper(this.httpHelpers, httpResponseMessage);

            if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
                return null;

            Debug.WriteLine(await httpResponseMessage.Content.ReadAsStringAsync());
            // TODO: Add time skew adjustment.

            throw new Exception("The HTTP request failed");
        }

        public async Task<T> SendAsync<T>(IHttpRequestMessage request, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            var httpResponse = await this.SendAsync(request, cancellationToken);
            
            // A null response means not found. Return null.
            if (httpResponse == null)
                return null;

            // If the target was found, but there was no content, throw. 
            if (httpResponse.Content == null)
                throw new HttpNoContentException();

            // Parse the content as the specified type.
            var contentJsonString = await httpResponse.Content.ReadAsStringAsync();
            return this.jsonHelpers.FromJsonString<T>(contentJsonString);
        }

        #endregion
    }
}
