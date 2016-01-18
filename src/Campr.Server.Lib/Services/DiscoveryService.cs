using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Net;
using Campr.Server.Lib.Net.Factories;
using Campr.Server.Lib.Net.Tent;

namespace Campr.Server.Lib.Services
{
    class DiscoveryService : IDiscoveryService
    {
        public DiscoveryService(ITentClient tentClient,
            IHttpRequestFactory requestFactory,
            ITentConstants tentConstants,
            IServiceProvider serviceProvider)
        {
            Ensure.Argument.IsNotNull(tentClient, nameof(tentClient));
            Ensure.Argument.IsNotNull(requestFactory, nameof(requestFactory));
            Ensure.Argument.IsNotNull(tentConstants, nameof(tentConstants));
            Ensure.Argument.IsNotNull(serviceProvider, nameof(serviceProvider));

            this.tentClient = tentClient;
            this.requestFactory = requestFactory;
            this.tentConstants = tentConstants;
            this.serviceProvider = serviceProvider;
        }

        private readonly ITentClient tentClient;
        private readonly IHttpRequestFactory requestFactory;
        private readonly ITentConstants tentConstants;
        private readonly IServiceProvider serviceProvider;

        public async Task<TentPost<T>> DiscoverUriAsync<T>(Uri targetUri) where T: class 
        {
            // Perform a GET request on the specified Uri.
            var httpClient = this.serviceProvider.Resolve<IHttpClient>();
            var request = this.requestFactory.Head(targetUri);
            var targetUriResponse = await httpClient.SendAsync(request);
            if (targetUriResponse == null)
            {
                return null;
            }

            // Extract the Uri of the Meta post from this response.
            var metaPostUri = targetUriResponse.FindLinkInHeader(this.tentConstants.MetaPostRel);

            // If needed, combine this Uri to get the absolute Meta post Uri.
            var absoluteMetaPostUri = metaPostUri.IsAbsoluteUri
                ? metaPostUri
                : new Uri(targetUri, metaPostUri);

            // Use the TentClient to retrieve the meta post.
            return await this.tentClient.RetrievePostAtUriAsync<T>(absoluteMetaPostUri);
        }
    }
}