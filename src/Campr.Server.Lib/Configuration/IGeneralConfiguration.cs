using System;
using System.Collections.Generic;
using Campr.Server.Lib.Enums;

namespace Campr.Server.Lib.Configuration
{
    public interface IGeneralConfiguration : ISensitiveConfiguration
    {
        Uri[] CouchBaseServers { get; }
        string MainBucketName { get; }

        string AuthCookieName { get; }
        string LangCookieName { get; }
        string CacheControlHeaderName { get; }

        TimeSpan AuthCookieExpiration { get; }
        TimeSpan DefaultBewitExpiration { get; }
        TimeSpan SubscriptionsQueueVisibilityTimeout { get; }
        TimeSpan DiscoveryAttemptTimeout { get; }
        TimeSpan OutgoingRequestTimeout { get; }
        TimeSpan[] NotificationRetryDelays { get; }

        int SubscriptionsBatchSize { get; }
        int MaxPostLimit { get; }
        int DefaultPostLimit { get; }
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