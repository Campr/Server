using System;
using System.Collections.Generic;
using Campr.Server.Lib.Enums;

namespace Campr.Server.Lib.Configuration
{
    public interface IGeneralConfiguration : IExternalConfiguration
    {
        string AuthCookieName { get; }
        string LangCookieName { get; }
        string CacheControlHeaderName { get; }

        TimeSpan AuthCookieExpiration { get; }
        TimeSpan DefaultBewitExpiration { get; }
        TimeSpan SubscriptionsQueueVisibilityTimeout { get; }
        TimeSpan DiscoveryAttemptTimeout { get; }
        TimeSpan OutgoingRequestTimeout { get; }
        TimeSpan[] NotificationRetryDelays { get; }

        uint SubscriptionsBatchSize { get; }
        uint MaxPostLimit { get; }
        uint DefaultPostLimit { get; }
        uint MaxRefs { get; }
        int MaxBatchRequests { get; }
        int MaxAttachmentImageSize { get; }
        int MinAttachmentImageSize { get; }
        int DefaultAvatarSize { get; }
        int MaxAttachmentResizableSize { get; }
        int MaxAttachmentCacheSize { get; }
        int MaxNotificationRetryAttempts { get; }

        string[] AttachmentResizableTypes { get; }
        string CamprEntityBaseUrl { get; }
        string CamprBaseDomain { get; }
        string CamprBaseUrl { get; }
        string CdnBaseUrl { get; }

        IDictionary<string, PostKnownTypeEnum> KnownPostTypes { get; }
    }
}