using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other.Factories;
using Campr.Server.Lib.Models.Tent.PostContent;
using Campr.Server.Lib.Repositories;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Http.Authentication;

namespace Campr.Server.Middleware
{
    public class HawkHandler : AuthenticationHandler<HawkOptions>
    {
        public HawkHandler(
            IUserRepository userRepository, 
            IPostRepository postRepository, 
            IBewitRepository bewitRepository, 
            IUriHelpers uriHelpers, 
            ITentHawkSignatureFactory hawkSignatureFactory,
            ITentPostTypeFactory postTypeFactory,
            IGeneralConfiguration configuration, 
            ITentConstants tentConstants)
        {
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
    
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Extract the user handle from the current request.
            var userHandle = this.uriHelpers.ExtractUsernameFromPath(this.Request.Path);
            if (string.IsNullOrWhiteSpace(userHandle))
                return AuthenticateResult.Failed("This is not an authenticable request.");

            // Parse the request Uri from the current request.
            var requestUri = this.Request.ToUri();
            
            // Extract the hawk signature from the Authorization header.
            var authorizationHeader = this.Request.Headers["Authorization"];
            if (authorizationHeader.Any())
            {
                // Parse the authorization header.
                var authorizationHawkSignature = this.hawkSignatureFactory.FromAuthorizationHeader(authorizationHeader.First());
                if (authorizationHawkSignature == null)
                    return AuthenticateResult.Failed("Unable to parse the provided Authorization hedaer.");

                // Check if the timespan for this request is an acceptable range.
                var timeDiff = DateTime.UtcNow - authorizationHawkSignature.Timestamp;
                if (timeDiff.Duration() > this.tentConstants.HawkTimestampThreshold)
                    return AuthenticateResult.Failed("Stale timestamp.");

                // Retrieve the User Id for the specified handle.
                var userId = await this.userRepository.GetIdFromHandleAsync(userHandle);
                if (string.IsNullOrWhiteSpace(userId))
                    return AuthenticateResult.Failed("Couldn't find the corresponding user.");

                // Retrieve the specified credentials post.
                var credentialsPost = await this.postRepository.GetAsync<TentContentCredentials>(userId, authorizationHawkSignature.Id);
                if (credentialsPost == null)
                    return AuthenticateResult.Failed("Session not found.");

                // Read the key from the credentials post, and validate the Hawk header.
                var credentialsKey = Encoding.UTF8.GetBytes(credentialsPost.Content.HawkKey);
                if (!authorizationHawkSignature.Validate(this.Request.Method, requestUri, credentialsKey))
                    return AuthenticateResult.Failed("Session not valid.");

                // Retrieve the post mentioned by the credentials.
                var credentialsTargetMention = credentialsPost.Mentions.First(m => m.FoundPost);
                var credentialsTargetPost = await this.postRepository.GetAsync<object>(credentialsTargetMention.UserId, credentialsTargetMention.PostId);
                if (credentialsTargetPost == null || credentialsTargetPost.UserId != userId)
                    return AuthenticateResult.Failed("Session not valid.");


                // If this is an app authorization post, authenticate as the user.
                if (credentialsTargetPost.Type.Type == this.tentConstants.AppAuthorizationPostType.Type
                  && credentialsTargetPost.Mentions != null)
                {
                    // Extract the first mention of the App Authorization to get the App Id.
                    var appAuthorizationFirstMention = credentialsTargetPost.Mentions.FirstOrDefault(m => m.FoundPost);
                    if (appAuthorizationFirstMention == null)
                        return AuthenticateResult.Failed("Couldn't find the corresponding app authorization.");

                    // Read the content of the App Authorization post.
                    var appAuthorizationContent = credentialsTargetPost.ReadContentAs<TentContentAppAuthorization>();
                    if (appAuthorizationContent == null)
                        return AuthenticateResult.Failed("Couldn't find a valid app authorization.");

                    // Set authentication data.
                    var identity = new ClaimsIdentity(new []
                    {
                        new Claim("userId", credentialsTargetPost.UserId),
                        new Claim("appId", appAuthorizationFirstMention.PostId),
                        new Claim("authType", "user") 
                    });

                    // Add the authorized post types to the request context.
                    this.Context.Items["AppPostTypes"] = appAuthorizationContent.Types;

                    // Update the parameters with the types allowed for this app.
                    if (!appAuthorizationContent.Types.GetAllRead().Contains("all"))
                        this.Context.Items[RequestItemEnum.AllowedPostTypes] = appAuthorizationContent.Types
                            .GetAllRead()
                            .Select(t => this.postTypeFactory.FromString(t));

                    return this.ResultFromIdentity(identity);
                }
                
                // If this is an App post, authenticate as an app.
                if (credentialsTargetPost.Type.Type == this.tentConstants.AppPostType.Type)
                {
                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim("appId", credentialsTargetPost.Id),
                        new Claim("authType", "app")
                    });

                    return this.ResultFromIdentity(identity);
                }
                
                // If this is a relationship post, authenticate as a remote server.
                if (credentialsTargetPost.Type.Type == this.tentConstants.RelationshipPostType.Type
                    && credentialsTargetPost.Mentions != null)
                {
                    // Extract the first mention of the Relationship to get our user's Id.
                    var relationshipFirstMention = credentialsTargetPost.Mentions.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.UserId));
                    if (relationshipFirstMention == null)
                        return AuthenticateResult.Failed("Couldn't find the corresponding user.");
                    
                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim("userId", relationshipFirstMention.UserId),
                        new Claim("relationshipUserId", credentialsTargetPost.UserId), 
                        new Claim("authType", "server")
                    });

                    return this.ResultFromIdentity(identity);
                }

                // Otherwise, fail.
                return AuthenticateResult.Failed("Couldn't find the corresponding authentication post.");
            }

            // If no authorization header was found, check for a bewit query parameter.
            var bewitQueryParameter = this.Request.Query["bewit"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(bewitQueryParameter))
            {
                // Read the bewit string to extract the credentials Id.
                var hawk = this.hawkSignatureFactory.FromBewit(bewitQueryParameter);
                if (hawk == null)
                    return AuthenticateResult.Failed("Couldn't read the provided bewit parameter.");

                // Retrieve the corresponding bewit object.
                var bewitCredentials = await this.bewitRepository.GetAsync(hawk.Id);
                if (bewitCredentials == null)
                    return AuthenticateResult.Failed("Couldn't find a corresponding authorization.");

                // Validate the bewit auth.
                if (!hawk.Validate(this.Request.Method, this.uriHelpers.RemoveUriQuery(requestUri), bewitCredentials.Key))
                    return AuthenticateResult.Failed("Authorization not valid.");

                var identity = new ClaimsIdentity(new[]
                {
                    new Claim("authType", "bewit")
                });

                return this.ResultFromIdentity(identity);
            }

            return AuthenticateResult.Failed("No valid Authentication header nor Bewit parameter found.");
        }

        private AuthenticateResult ResultFromIdentity(IIdentity identity)
        {
            var principal = new GenericPrincipal(identity, new[] { "user" });
            var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), "hawk");

            return AuthenticateResult.Success(ticket);
        }

        private CacheControlValue ReadCacheControl()
        {
            // First, check we have the custom Cache-Control header.
            var cacheControlStr = this.Request.Headers[this.configuration.CacheControlHeaderName].FirstOrDefault() ??
                                  this.Request.Headers["Cache-Control"].FirstOrDefault();

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