using System;
using Campr.Server.Lib.Models.Other;

namespace Campr.Server.Lib.Configuration
{
    internal class TentConstants : ITentConstants
    {
        private const string PostContentTypeConst = "application/vnd.tent.post.v0+json";
        private const string PostFeedContentTypeConst = "application/vnd.tent.posts-feed.v0+json";
        private const string MentionsContentTypeConst = "application/vnd.tent.post-mentions.v0+json";
        private const string ReplyChainContentTypeConst = "application/vnd.campr.post-replychain.v0+json";
        private const string ServerInfoContentTypeConst = "application/vnd.tent.server-info.v0+json";
        private const string VersionsContentTypeConst = "application/vnd.tent.post-versions.v0+json";
        private const string VersionChildrenContentTypeConst = "application/vnd.tent.post-children.v0+json";
        private const string ErrorContentTypeConst = "application/vnd.tent.error.v0+json";
        private const string JsonContentTypeConst = "application/json";
        private const string FormDataContentTypeConst = "multipart/form-data";

        public string NewPostEndpoint { get; } = "new_post";
        public string OAuthEndpoint { get; } = "oauth_auth";
        public string OTokenEndpoint { get; } = "oauth_token";
        public string PostContentType { get; } = PostContentTypeConst;
        public string PostFeedContentType { get; } = PostFeedContentTypeConst;
        public string MentionsContentType { get; } = MentionsContentTypeConst;
        public string ReplyChainContentType { get; } = ReplyChainContentTypeConst;
        public string ServerInfoContentType { get; } = ServerInfoContentTypeConst;
        public string VersionsContentType { get; } = VersionsContentTypeConst;
        public string VersionChildrenContentType { get; } = VersionChildrenContentTypeConst;
        public string ErrorContentType { get; } = ErrorContentTypeConst;
        public string JsonContentType { get; } = JsonContentTypeConst;
        public string FormDataContentType { get; } = FormDataContentTypeConst;
     
        public string[] ApiContentTypes { get; } = {
            PostContentTypeConst,
            PostFeedContentTypeConst,
            MentionsContentTypeConst,
            ReplyChainContentTypeConst,
            VersionsContentTypeConst,
            VersionChildrenContentTypeConst,
            ServerInfoContentTypeConst,
            ErrorContentTypeConst,
            JsonContentTypeConst,
            FormDataContentTypeConst
        };

        public string[] WebContentTypes { get; } =
        {
            "text/html",
            "application/x-www-form-urlencoded",
            "application/xhtml+xml",
            "application/xml"
        };

        public ITentPostType MetaPostType { get; } = new TentPostType("https://tent.io/types/meta/v0");
        public ITentPostType AppPostType { get; } = new TentPostType("https://tent.io/types/app/v0");
        public ITentPostType AppAuthorizationPostType { get; } = new TentPostType("https://tent.io/types/app-auth/v0#");
        public ITentPostType RelationshipPostType { get; } = new TentPostType("https://tent.io/types/relationship/v0#");
        public ITentPostType SubscriptionPostType { get; } = new TentPostType("https://tent.io/types/subscription/v0#");
        public ITentPostType CredentialsPostType { get; } = new TentPostType("https://tent.io/types/credentials/v0#");
        public ITentPostType DeletePostType { get; } = new TentPostType("https://tent.io/types/delete/v0#");
        public ITentPostType DeliveryFailurePostType { get; } = new TentPostType("https://tent.io/types/delivery-failure/v0#");
        public ITentPostType CamprProfilePostType { get; } = new TentPostType("https://campr.me/types/profile/v0#");
        public string CredentialsRel { get; } = "https://tent.io/rels/credentials";
        public string NotificationRel { get; } = "https://tent.io/rels/notification";
        public string ImportRel { get; } = "https://tent.io/rels/import";
        public string MetaPostRel { get; } = "https://tent.io/rels/meta-post";
        public string HawkTokenType { get; } = "https://tent.io/oauth/hawk-token";

        public string CountHeaderName { get; } = "count";
        public string HashPrefix { get; } = "sha512t256-";
        public string HawkAlgorithm { get; } = "sha256";

        public string CreateDeletePostHeader { get; } = "Create-Delete-Post";
        public string CacheNoProxyHeader { get; } = "no-proxy";
        public string CacheProxyIfMissHeader { get; } = "proxy-if-miss";
        public string CacheProxyHeader { get; } = "proxy";

        public string DeliveryFailureStatusTemporary { get; } = "temporary";
        public string DeliveryFailureStatusPermanent { get; } = "permanent";
        public string DeliveryFailureReasonUnreachable { get; } = "unreachable";
        public string DeliveryFailureReasonDiscovery { get; } = "discovery_failed";
        public string DeliveryFailureReasonRelationship { get; } = "relationship_failed";
        public string DeliveryFailureReasonDelivery { get; } = "delivery_failed";

        public TimeSpan HawkTimestampThreshold { get; } = TimeSpan.FromSeconds(60);
    }
}