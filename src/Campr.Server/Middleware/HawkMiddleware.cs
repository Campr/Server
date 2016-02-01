using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other.Factories;
using Campr.Server.Lib.Repositories;
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
            IUrlEncoder encoder, 

            IUserRepository userRepository, 
            IPostRepository postRepository, 
            IBewitRepository bewitRepository, 
            IUriHelpers uriHelpers, 
            ITentHawkSignatureFactory hawkSignatureFactory, 
            ITentPostTypeFactory postTypeFactory, 
            IGeneralConfiguration configuration, 
            ITentConstants tentConstants) : base(next, options, loggerFactory, encoder)
        {
            Ensure.Argument.IsNotNull(next, nameof(next));
            Ensure.Argument.IsNotNull(options, nameof(options));

            Ensure.Argument.IsNotNull(userRepository, nameof(userRepository));
            Ensure.Argument.IsNotNull(postRepository, nameof(postRepository));
            Ensure.Argument.IsNotNull(bewitRepository, nameof(bewitRepository));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            Ensure.Argument.IsNotNull(hawkSignatureFactory, nameof(hawkSignatureFactory));
            Ensure.Argument.IsNotNull(postTypeFactory, nameof(postTypeFactory));
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));
            Ensure.Argument.IsNotNull(tentConstants, nameof(tentConstants));

            this.userRepository = userRepository;
            this.postRepository = postRepository;
            this.bewitRepository = bewitRepository;
            this.uriHelpers = uriHelpers;
            this.hawkSignatureFactory = hawkSignatureFactory;
            this.postTypeFactory = postTypeFactory;
            this.configuration = configuration;
            this.tentConstants = tentConstants;
        }

        private readonly IUserRepository userRepository;
        private readonly IPostRepository postRepository;
        private readonly IBewitRepository bewitRepository;
        private readonly IUriHelpers uriHelpers;
        private readonly ITentHawkSignatureFactory hawkSignatureFactory;
        private readonly ITentPostTypeFactory postTypeFactory;
        private readonly IGeneralConfiguration configuration;
        private readonly ITentConstants tentConstants;

        protected override AuthenticationHandler<HawkOptions> CreateHandler()
        {
            return new HawkHandler(
                this.userRepository,
                this.postRepository,
                this.bewitRepository,
                this.uriHelpers,
                this.hawkSignatureFactory,
                this.postTypeFactory,
                this.configuration,
                this.tentConstants);
        }
    }
}