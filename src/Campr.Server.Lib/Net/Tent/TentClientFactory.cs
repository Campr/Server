using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;
using Campr.Server.Lib.Net.Base.Factories;

namespace Campr.Server.Lib.Net.Tent
{
    class TentClientFactory : ITentClientFactory
    {
        public TentClientFactory(
            IHttpRequestFactory httpRequestFactory,
            IHttpClientFactory httpClientFactory,
            IQueryStringHelpers queryStringHelpers,
            IUriHelpers uriHelpers,
            ITentConstants tentConstants)
        {
            Ensure.Argument.IsNotNull(httpRequestFactory, nameof(httpRequestFactory));
            Ensure.Argument.IsNotNull(httpClientFactory, nameof(httpClientFactory));
            Ensure.Argument.IsNotNull(queryStringHelpers, nameof(queryStringHelpers));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            Ensure.Argument.IsNotNull(tentConstants, nameof(tentConstants));

            this.httpRequestFactory = httpRequestFactory;
            this.httpClientFactory = httpClientFactory;
            this.queryStringHelpers = queryStringHelpers;
            this.uriHelpers = uriHelpers;
            this.tentConstants = tentConstants;
        }

        private readonly IHttpRequestFactory httpRequestFactory;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IQueryStringHelpers queryStringHelpers;
        private readonly IUriHelpers uriHelpers;
        private readonly ITentConstants tentConstants;

        public ITentClient Make(TentPost<TentContentMeta> target)
        {
            return new TentClient(
                this.httpRequestFactory,
                this.httpClientFactory,
                this.queryStringHelpers,
                this.uriHelpers,
                this.tentConstants,
                target);
        }

        public ITentClient MakeAuthenticated(TentPost<TentContentMeta> target, ITentHawkSignature credentials)
        {
            return new TentClient(
                this.httpRequestFactory,
                this.httpClientFactory,
                this.queryStringHelpers,
                this.uriHelpers,
                this.tentConstants,
                target,
                credentials);
        }
    }
}