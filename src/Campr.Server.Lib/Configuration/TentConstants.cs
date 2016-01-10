using System;

namespace Campr.Server.Lib.Configuration
{
    internal class TentConstants : ITentConstants
    {
        private const string NewPostEndpointConst = "new_post";
        private const string OAuthEndpointConst = "oauth_auth";
        private const string OTokenEndpointConst = "oauth_token";
        private const string PostContentTypeConst = "application/vnd.tent.post.v0+json";
        private const string PostFeedContentTypeConst = "application/vnd.tent.posts-feed.v0+json";
        private const string MentionsContentTypeConst = "application/vnd.tent.post-mentions.v0+json";
        private const string ReplyChainContentTypeConst = "application/vnd.campr.post-replychain.v0+json";
        private const string VersionsContentTypeConst = "application/vnd.tent.post-versions.v0+json";
        private const string VersionChildrenContentTypeConst = "application/vnd.tent.post-children.v0+json";
        private const string ServerInfoContentTypeConst = "application/vnd.tent.server-info.v0+json";
        private const string ErrorContentTypeConst = "application/vnd.tent.error.v0+json";
        private const string FormDataContentType = "multipart/form-data";
        private const string JsonContentTypeConst = "application/json";
        private const string HawkTokenTypeConst = "https://tent.io/oauth/hawk-token";

        private const string MetaPostTypeConst = "https://tent.io/types/meta/v0#";
        private const string AppPostTypeConst = "https://tent.io/types/app/v0#";
        private const string AppAuthorizationTypeConst = "https://tent.io/types/app-auth/v0#";
        private const string RelationshipPostTypeConst = "https://tent.io/types/relationship/v0#";
        private const string SubscriptionPostTypeConst = "https://tent.io/types/subscription/v0#";
        private const string CredentialsPostTypeConst = "https://tent.io/types/credentials/v0#";
        private const string DeletePostTypeConst = "https://tent.io/types/delete/v0#";
        private const string DeliveryFailurePostTypeConst = "https://tent.io/types/delivery-failure/v0#";
        private const string CamprProfilePostTypeConst = "https://campr.me/types/profile/v0#";

        private const string CredentialsRelConst = "https://tent.io/rels/credentials";
        private const string NotificationRelConst = "https://tent.io/rels/notification";
        private const string ImportRelConst = "https://tent.io/rels/import";
        private const string MetaPostRelConst = "https://tent.io/rels/meta-post";

        private const string CountHeaderNameConst = "count";
        private const string HashPrefixConst = "sha512t256-";
        private const string HawkAlgorithmConst = "sha256";
        private const string CreateDeletePostHeaderConst = "Create-Delete-Post";

        private const string CacheNoProxyHeaderConst = "no-proxy";
        private const string CacheProxyIfMissHeaderConst = "proxy-if-miss";
        private const string CacheProxyHeaderConst = "proxy";

        private const string DeliveryFailureStatusTemporaryConst = "temporary";
        private const string DeliveryFailureStatusPermanentConst = "permanent";

        private const string DeliveryFailureReasonUnreachableConst = "unreachable";
        private const string DeliveryFailureReasonDiscoveryConst = "discovery_failed";
        private const string DeliveryFailureReasonRelationshipConst = "relationship_failed";
        private const string DeliveryFailureReasonDeliveryConst = "delivery_failed";

        private readonly TimeSpan hawkTimestampThreshold = TimeSpan.FromSeconds(60);

        private readonly string[] apiContentTypes =
        {
            PostContentTypeConst,
            PostFeedContentTypeConst,
            MentionsContentTypeConst,
            ReplyChainContentTypeConst,
            VersionsContentTypeConst,
            VersionChildrenContentTypeConst,
            ServerInfoContentTypeConst,
            ErrorContentTypeConst,
            FormDataContentType,
            JsonContentTypeConst
        };

        private readonly string[] webContentTypes =
        {
            "text/html",
            "application/x-www-form-urlencoded",
            "application/xhtml+xml",
            "application/xml"
        };

        public string NewPostEndpoint()
        {
            return NewPostEndpointConst;
        }

        public string OAuthEndpoint()
        {
            return OAuthEndpointConst;
        }

        public string OTokenEndpoint()
        {
            return OTokenEndpointConst;
        }

        public string PostContentType()
        {
            return PostContentTypeConst;
        }

        public string PostFeedContentType()
        {
            return PostFeedContentTypeConst;
        }

        public string MentionsContentType()
        {
            return MentionsContentTypeConst;
        }

        public string VersionsContentType()
        {
            return VersionsContentTypeConst;
        }

        public string VersionChildrenContentType()
        {
            return VersionChildrenContentTypeConst;
        }

        public string ReplyChainContentType()
        {
            return ReplyChainContentTypeConst;
        }

        public string ErrorContentType()
        {
            return ErrorContentTypeConst;
        }

        public string[] ApiContentTypes()
        {
            return this.apiContentTypes;
        }

        public string[] WebContentTypes()
        {
            return this.webContentTypes;
        }

        public string JsonContentType()
        {
            return JsonContentTypeConst;
        }

        public string HawkTokenType()
        {
            return HawkTokenTypeConst;
        }

        public string MetaPostType()
        {
            return MetaPostTypeConst;
        }

        public string AppPostType()
        {
            return AppPostTypeConst;
        }

        public string AppAuthorizationPostType()
        {
            return AppAuthorizationTypeConst;
        }

        public string RelationshipPostType()
        {
            return RelationshipPostTypeConst;
        }

        public string SubscriptionPostType()
        {
            return SubscriptionPostTypeConst;
        }

        public string CredentialsPostType()
        {
            return CredentialsPostTypeConst;
        }

        public string DeletePostType()
        {
            return DeletePostTypeConst;
        }

        public string DeliveryFailurePostType()
        {
            return DeliveryFailurePostTypeConst;
        }

        public string CamprProfilePostType()
        {
            return CamprProfilePostTypeConst;
        }

        public string CredentialsRel()
        {
            return CredentialsRelConst;
        }

        public string NotificationRel()
        {
            return NotificationRelConst;
        }

        public string ImportRel()
        {
            return ImportRelConst;
        }

        public string MetaPostRel()
        {
            return MetaPostRelConst;
        }

        public string CountHeaderName()
        {
            return CountHeaderNameConst;
        }

        public string HashPrefix()
        {
            return HashPrefixConst;
        }

        public string HawkAlgorithm()
        {
            return HawkAlgorithmConst;
        }

        public string CreateDeletePostHeader()
        {
            return CreateDeletePostHeaderConst;
        }

        public string CacheNoProxyHeader()
        {
            return CacheNoProxyHeaderConst;
        }

        public string CacheProxyIfMissHeader()
        {
            return CacheProxyIfMissHeaderConst;
        }

        public string CacheProxyHeader()
        {
            return CacheProxyHeaderConst;
        }

        public string DeliveryFailureStatusTemporary()
        {
            return DeliveryFailureStatusTemporaryConst;
        }

        public string DeliveryFailureStatusPermanent()
        {
            return DeliveryFailureStatusPermanentConst;
        }

        public string DeliveryFailureReasonUnreachable()
        {
            return DeliveryFailureReasonUnreachableConst;
        }

        public string DeliveryFailureReasonDiscovery()
        {
            return DeliveryFailureReasonDiscoveryConst;
        }

        public string DeliveryFailureReasonRelationship()
        {
            return DeliveryFailureReasonRelationshipConst;
        }

        public string DeliveryFailureReasonDelivery()
        {
            return DeliveryFailureReasonDeliveryConst;
        }

        public TimeSpan HawkTimestampThreshold()
        {
            return this.hawkTimestampThreshold;
        }
    }
}