using System;

namespace Campr.Server.Lib.Configuration
{
    public interface ITentConstants
    {
        string NewPostEndpoint();
        string OAuthEndpoint();
        string OTokenEndpoint();

        string PostContentType();
        string PostFeedContentType();
        string MentionsContentType();
        string VersionsContentType();
        string VersionChildrenContentType();
        string ReplyChainContentType();
        string ErrorContentType();
        string JsonContentType();
        string[] ApiContentTypes();
        string[] WebContentTypes();

        string MetaPostType();
        string AppPostType();
        string AppAuthorizationPostType();
        string RelationshipPostType();
        string SubscriptionPostType();
        string CredentialsPostType();
        string DeletePostType();
        string DeliveryFailurePostType();
        string CamprProfilePostType();

        string CredentialsRel();
        string NotificationRel();
        string ImportRel();
        string MetaPostRel();

        string HawkTokenType();
        string CountHeaderName();
        string HashPrefix();
        string HawkAlgorithm();
        string CreateDeletePostHeader();

        string CacheNoProxyHeader();
        string CacheProxyIfMissHeader();
        string CacheProxyHeader();

        string DeliveryFailureStatusTemporary();
        string DeliveryFailureStatusPermanent();

        string DeliveryFailureReasonUnreachable();
        string DeliveryFailureReasonDiscovery();
        string DeliveryFailureReasonRelationship();
        string DeliveryFailureReasonDelivery();

        TimeSpan HawkTimestampThreshold();
    }
}