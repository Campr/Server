using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Net.Base.Factories
{
    class HttpClientFactory : IHttpClientFactory
    {
        public HttpClientFactory(
            IJsonHelpers jsonHelpers, 
            IHttpHelpers httpHelpers)
        {
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));
            Ensure.Argument.IsNotNull(httpHelpers, nameof(httpHelpers));

            this.jsonHelpers = jsonHelpers;
            this.httpHelpers = httpHelpers;
        }

        private readonly IJsonHelpers jsonHelpers;
        private readonly IHttpHelpers httpHelpers;
        
        public IHttpClient Make()
        {
            return new HttpClientWrapper(this.jsonHelpers, this.httpHelpers);
        }
    }
}