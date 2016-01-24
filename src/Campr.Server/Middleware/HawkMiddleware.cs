using Campr.Server.Lib.Infrastructure;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebEncoders;

namespace Campr.Server.Middleware
{
    public class HawkMiddleware : AuthenticationMiddleware<HawkOptions>
    {
        public HawkMiddleware(
            RequestDelegate next, 
            HawkOptions options, 
            ILoggerFactory loggerFactory, 
            IUrlEncoder encoder
            ) : base(next, options, loggerFactory, encoder)
        {
            Ensure.Argument.IsNotNull(next, nameof(next));
            Ensure.Argument.IsNotNull(options, nameof(options));
        }

        protected override AuthenticationHandler<HawkOptions> CreateHandler()
        {
            return new HawkHandler();
        }
    }
}