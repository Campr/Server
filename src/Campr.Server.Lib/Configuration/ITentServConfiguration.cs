using System;
using System.Collections.Generic;
using Campr.Server.Lib.Enums;

namespace Campr.Server.Lib.Configuration
{
    public interface ITentServConfiguration
    {
        string QueuesConnectionString();
        string BlobsConnectionString();
        string EncryptionKey();
        string AuthCookieName();
        string LangCookieName();
        string CacheControlHeaderName();
        TimeSpan AuthCookieExpiration();
        TimeSpan DefaultBewitExpiration();
        TimeSpan SubscriptionsQueueVisibilityTimeout();
        TimeSpan DiscoveryAttemptTimeout();
        TimeSpan OutgoingRequestTimeout();
        TimeSpan[] NotificationRetryDelays();
        int SubscriptionsBatchSize();
        int MaxPostLimit();
        int DefaultPostLimit();
        int MaxBatchRequests();
        int MaxAttachmentImageSize();
        int MinAttachmentImageSize();
        int DefaultAvatarSize();
        int MaxAttachmentResizableSize();
        int MaxAttachmentCacheSize();
        int MaxNotificationRetryAttempts();
        string[] AttachmentResizableTypes();
        string CamprEntityBaseUrl();
        string CamprBaseDomain();
        string CamprBaseUrl();
        string CdnBaseUrl();

        IDictionary<string, PostKnownTypeEnum> KnownPostTypes();
    }
}