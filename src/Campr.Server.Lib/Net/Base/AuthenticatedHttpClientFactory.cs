using System;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Net.Base
{
    class AuthenticatedHttpClientFactory : IAuthenticatedHttpClientFactory
    {
        public AuthenticatedHttpClientFactory(IServiceProvider serviceProvider)
        {
            Ensure.Argument.IsNotNull(serviceProvider, nameof(serviceProvider));
            this.serviceProvider = serviceProvider;
        }

        private readonly IServiceProvider serviceProvider;

        public IAuthenticatedHttpClient MakeAuthenticatedHttpClient()
        {
            return this.serviceProvider.Resolve<IAuthenticatedHttpClient>();
        }

        public IAuthenticatedHttpClient MakeAuthenticatedHttpClientWithCustomCredentials(TentPost<object> credentialsPost)
        {
            var http = this.serviceProvider.Resolve<IAuthenticatedHttpClient>();
            http.SetCredentials(credentialsPost);

            return http;
        }
    }
}