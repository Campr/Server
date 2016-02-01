using Campr.Server.Lib.Infrastructure;
using Microsoft.AspNet.Builder;

namespace Campr.Server.Middleware
{
    public static class HawkAppBuilderExtensions
    {
        public static IApplicationBuilder UseHawkAuthentication(this IApplicationBuilder app, HawkOptions options)
        {
            Ensure.Argument.IsNotNull(app, nameof(app));
            Ensure.Argument.IsNotNull(options, nameof(options));

            return app.UseMiddleware<HawkMiddleware>(options);
        }
    }
}