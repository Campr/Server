using Campr.Server.Lib.Infrastructure;
using Microsoft.AspNet.Builder;

namespace Campr.Server.Middleware
{
    public static class TentParametersAppBuilderExtensions
    {
        public static IApplicationBuilder UseTentParameters(this IApplicationBuilder app)
        {
            Ensure.Argument.IsNotNull(app, nameof(app));
            return app.UseMiddleware<TentParametersMiddleware>();
        }
    }
}