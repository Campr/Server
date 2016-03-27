using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Connectors.Blobs;
using Campr.Server.Lib.Connectors.Blobs.Azure;
using Campr.Server.Lib.Connectors.Queues;
using Campr.Server.Lib.Connectors.Queues.Azure;
using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Json;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Models.Other.Factories;
using Campr.Server.Lib.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Campr.Server.Lib
{
    public static class CamprCommonInitializer
    {
        public static void Register(IServiceCollection services)
        {
            // Base classes.
            services.AddSingleton<IGeneralConfiguration, GeneralConfiguration>();
            services.AddSingleton<ITentConstants, TentConstants>();
            services.AddSingleton<IUriHelpers, UriHelpers>();
            services.AddSingleton<IStringHelpers, StringHelpers>();
            services.AddSingleton<ICryptoHelpers, CryptoHelpers>();
            services.AddSingleton<ITextHelpers, TextHelpers>();
            services.AddSingleton<IQueryStringHelpers, QueryStringHelpers>();
            //services.AddSingleton<IAuthenticationHelpers, AuthenticationHelpers>();
            services.AddSingleton<IModelHelpers, ModelHelpers>();
            //services.AddSingleton<IHttpHelpers, HttpHelpers>();
            //services.AddSingleton<IImageService, ImageService>();

            services.AddSingleton<IWebContractResolver, WebContractResolver>();
            services.AddSingleton<IDbContractResolver, DbContractResolver>();
            services.AddSingleton<IWebJsonHelpers>(s => new JsonHelpers(s.GetService<IWebContractResolver>()));
            services.AddSingleton<IDbJsonHelpers>(s => new JsonHelpers(s.GetService<IDbContractResolver>()));
            services.AddSingleton<TentPostTypeConverter, TentPostTypeConverter>();
            services.AddSingleton<IJsonHelpers>(s => s.GetService<IWebJsonHelpers>());

            // Network.
            //services.AddTransient<IHttpClient, HttpClientWrapper>();
            //services.AddSingleton<IHttpRequestFactory, HttpRequestFactory>();
            //services.AddSingleton<ITentClient, TentClient>();
            //services.AddSingleton<IDiscoveryService, DiscoveryService>();

            // Model factories.
            services.AddSingleton<IUserFactory, UserFactory>();
            services.AddSingleton<ITentPostFactory, TentPostFactory>();
            services.AddSingleton<IUserPostFactory, UserPostFactory>();
            //services.AddSingleton<IDbSessionFactory, DbSessionFactory>();
            //services.AddSingleton<IDbPostFactory, DbPostFactory>();
            //services.AddSingleton<IDbAttachmentFactory, DbAttachmentFactory>();
            //services.AddSingleton<IDbBewitFactory, DbBewitFactory>();
            //services.AddSingleton<IDbOauthCodeFactory, DbOauthCodeFactory>();

            //services.AddSingleton<ITentMetaServerFactory, TentMetaServerFactory>();
            //services.AddSingleton<ITentAttachmentFactory, TentAttachmentFactory>();

            services.AddSingleton<ITentPostTypeFactory, TentPostTypeFactory>();
            //services.AddSingleton<ITentRequestDateFactory, TentRequestDateFactory>();
            //services.AddSingleton<ITentRequestPostFactory, ITentRequestPostFactory>();
            //services.AddSingleton<ITentRequestParametersFactory, TentRequestParametersFactory>();
            //services.AddSingleton<ITentHawkSignatureFactory, TentHawkSignatureFactory>();

            // Data Access.
            services.AddSingleton<ITentQueues, AzureTentQueues>();
            services.AddSingleton<ITentBlobs, AzureTentBlobs>();
            services.AddSingleton<IRethinkConnection, RethinkConnection>();
            
            services.AddSingleton<IUserRepository, UserRepository>();
            //services.AddSingleton<IUserMapRepository, UserMapRepository>();
            //services.AddSingleton<ISessionRepository, SessionRepository>();
            services.AddSingleton<IPostRepository, PostRepository>();
            services.AddSingleton<IUserPostRepository, UserPostRepository>();
            //services.AddSingleton<IAttachmentRepository, AttachmentRepository>();
            //services.AddSingleton<IBewitRepository, BewitRepository>();
            //services.AddSingleton<IOauthCodeRepository, OauthCodeRepository>();

            //// App logic.
            //services.AddSingleton<IBewitLogic, BewitLogic>();
            //services.AddSingleton<IUserLogic, UserLogic>();
            //services.AddSingleton<IFollowLogic, FollowLogic>();
            //services.AddSingleton<ITypeSpecificLogic, TypeSpecificLogic>();
            //services.AddSingleton<IPostLogic, PostLogic>();
            //services.AddSingleton<IAppPostLogic, AppPostLogic>();
            //services.AddSingleton<IAttachmentLogic, AttachmentLogic>();
            //services.AddSingleton<IAuthenticationService, AuthenticationService>();
        }
    }
}