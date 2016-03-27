using System;
using Campr.Server.Lib.Models.Other;

namespace Campr.Server.Lib.Configuration
{
    public interface ITentConstants
    {
        string NewPostEndpoint { get; }
        string OAuthEndpoint { get; }
        string OTokenEndpoint { get; }

        string PostContentType { get; }
        string PostFeedContentType { get; }
        string MentionsContentType { get; }
        string ReplyChainContentType { get; }
        string ServerInfoContentType { get; }
        string VersionsContentType { get; }
        string VersionChildrenContentType { get; }
        string ErrorContentType { get; }
        string JsonContentType { get; }
        string FormDataContentType { get; }
        string[] ApiContentTypes { get; }
        string[] WebContentTypes { get; }

        ITentPostType MetaPostType { get; }
        ITentPostType AppPostType { get; }
        ITentPostType AppAuthorizationPostType { get; }
        ITentPostType RelationshipPostType { get; }
        ITentPostType SubscriptionPostType { get; }
        ITentPostType CredentialsPostType { get; }
        ITentPostType DeletePostType { get; }
        ITentPostType DeliveryFailurePostType { get; }
        ITentPostType CamprProfilePostType { get; }

        string CredentialsRel { get; }
        string NotificationRel { get; }
        string ImportRel { get; }
        string MetaPostRel { get; }

        string HawkTokenType { get; }
        string CountHeaderName { get; }
        string HashPrefix { get; }
        string HawkAlgorithm { get; }
        string CreateDeletePostHeader { get; }

        string CacheNoProxyHeader { get; }
        string CacheProxyIfMissHeader { get; }
        string CacheProxyHeader { get; }

        string DeliveryFailureStatusTemporary { get; }
        string DeliveryFailureStatusPermanent { get; }

        string DeliveryFailureReasonUnreachable { get; }
        string DeliveryFailureReasonDiscovery { get; }
        string DeliveryFailureReasonRelationship { get; }
        string DeliveryFailureReasonDelivery { get; }

        TimeSpan HawkTimestampThreshold { get; }
    }
}