using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Net.Base.Factories;

namespace Campr.Server.Lib.Net.Tent
{
    public class SimpleTentClient : ISimpleTentClient
    {
        public SimpleTentClient(
            IHttpRequestFactory httpRequestFactory,
            IHttpClientFactory httpClientFactory,
            ITentConstants tentConstants, 
            ITentHawkSignature credentials = null)
        {
            Ensure.Argument.IsNotNull(httpRequestFactory, nameof(httpRequestFactory));
            Ensure.Argument.IsNotNull(httpClientFactory, nameof(httpClientFactory));
            Ensure.Argument.IsNotNull(tentConstants, nameof(tentConstants));

            this.httpRequestFactory = httpRequestFactory;
            this.httpClientFactory = httpClientFactory;
            this.tentConstants = tentConstants;
            this.Credentials = credentials;
        }

        private readonly IHttpRequestFactory httpRequestFactory;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ITentConstants tentConstants;

        protected ITentHawkSignature Credentials { get; }

        public async Task<TentPost<T>> GetAsync<T>(Uri postUri, CancellationToken cancellationToken = new CancellationToken()) where T : class
        {
            // Build the request.
            var request = this.httpRequestFactory
                .Get(postUri)
                .AddAccept(this.tentConstants.PostContentType);

            // If needed, and an Authorization header to this request.
            if (this.Credentials != null)
                request.AddCredentials(this.Credentials);

            // Perform the request using an HTTP client.
            var client = this.httpClientFactory.Make();
            var postResult = await client.SendAsync<TentPostResult<T>>(request, cancellationToken);

            // TODO: Perform validation.

            // Return the post.
            return postResult?.Post;
        }
    }
}