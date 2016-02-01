using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other.Factories;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using System.Linq;

namespace Campr.Server.Middleware
{
    public class TentParametersMiddleware
    {
        public TentParametersMiddleware(RequestDelegate next)
        {
            Ensure.Argument.IsNotNull(next, nameof(next));
            this.next = next;
        }

        private readonly RequestDelegate next;

        public Task Invoke(
            IQueryStringHelpers queryStringHelpers,
            ITentRequestParametersFactory requestParametersFactory,
            IGeneralConfiguration configuration,
            HttpContext context)
        {
            // Read the request parameters from the request.
            var query = queryStringHelpers.ParseQueryString(context.Request.QueryString.Value);
            context.Items[RequestItemEnum.TentParameters] = requestParametersFactory.FromQueryString(
                query, 
                this.ReadCacheControl(configuration, context.Request));

            // Continue on to the next middleware.
            return this.next(context);
        }

        private CacheControlValue ReadCacheControl(IGeneralConfiguration configuration, HttpRequest request)
        {
            // First, check we have the custom Cache-Control header.
            var cacheControlStr = request.Headers[configuration.CacheControlHeaderName].FirstOrDefault() ??
                                  request.Headers["Cache-Control"].FirstOrDefault();

            // Convert it to an enum value.
            switch (cacheControlStr)
            {
                case "proxy":
                    return CacheControlValue.Proxy;
                case "no-proxy":
                    return CacheControlValue.NoProxy;
                default:
                    return default(CacheControlValue);
            }
        }
    }
}