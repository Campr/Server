using System;
using System.Collections.Generic;
using Campr.Server.Lib.Enums;

namespace Campr.Server.Lib.Configuration
{
    class TentServConfiguration : ITentServConfiguration
    {
        private const string EncryptionKeyConst = "fBW9zrP/tj/uiahbu3XeEMCmUtXBWlvnKNGnS3KTlvg=;1KBkVNgI1hMg/7Da64XIig==";
        private const string AuthCookieNameConst = "campr_auth";
        private const string LangCookieNameConst = "campr_lang";
        private const string CacheControlHeaderNameConst = "X-Cache-Control";

        private const string CamprEntityBaseUrlConst = "https://{0}.campr.me";
        private const string CamprBaseDomainConst = "campr.me";
        private const string CamprBaseUrlConst = "https://campr.me";
        private const string CdnBaseUrlConst = "https://az557631.vo.msecnd.net";

        private const int SubscriptionsBatchSizeConst = 1000;
        private const int MaxPostLimitConst = 200;
        private const int DefaultPostLimitConst = 25;
        private const int MaxBatchRequestsConst = 50;
        private const int MaxAttachmentImageSizeConst = 2000;
        private const int MinAttachmentImageSizeConst = 10;
        private const int DefaultAvatarSizeConst = 100;
        private const int MaxAttachmentResizableSizeConst = 20971520;   // 20MB.
        private const int MaxAttachmentCacheSizeConst = 20971520;       // 20MB.
        private const int MaxNotificationRetryAttemptsConst = 3;

        private readonly string[] attachmentResizableTypes = {
            "image/jpeg",
            "image/jpg",
            "image/png" };
        
        private readonly IDictionary<string, PostKnownTypeEnum> knownPostTypes = new Dictionary<string, PostKnownTypeEnum>
        {
            { "https://tent.io/types/app/v0" , PostKnownTypeEnum.Application },
            { "https://tent.io/types/credentials/v0", PostKnownTypeEnum.Credentials },
            { "https://tent.io/types/cursor/v0", PostKnownTypeEnum.Cursor },
            { "https://tent.io/types/delete/v0", PostKnownTypeEnum.Delete },
            { "https://tent.io/types/essay/v0", PostKnownTypeEnum.Essay },
            { "https://tent.io/types/meta/v0", PostKnownTypeEnum.Meta },
            { "https://tent.io/types/relationship/v0", PostKnownTypeEnum.Relationship },
            { "https://tent.io/types/repost/v0", PostKnownTypeEnum.Repost },
            { "https://tent.io/types/status/v0", PostKnownTypeEnum.Status },
            { "https://tent.io/types/subscription/v0", PostKnownTypeEnum.Subscription },
            { "https://tent.io/types/tag/v0", PostKnownTypeEnum.Tag }
        };

        private readonly TimeSpan authCookieExpiration = TimeSpan.FromDays(30);
        private readonly TimeSpan defaultBewitExpiration = TimeSpan.FromMinutes(30);
        private readonly TimeSpan subscriptionQueueVisibilityTimeout = TimeSpan.FromMinutes(20);
        private readonly TimeSpan discoveryAttemptTimeout = TimeSpan.FromMinutes(10);
        private readonly TimeSpan outgoingRequestTimeout = TimeSpan.FromSeconds(5);

        private readonly TimeSpan[] notificationRetryDelays =
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(5)
        };

        public string QueuesConnectionString()
        {
            return "DefaultEndpointsProtocol=https;AccountName=camprqueues00001;AccountKey=c3d0ygXgr4FHfFQomeyvX2Uhx0Fb+9jVrSlXnjQGKY74Rl2P/kUJsIfJ/xZG3xmSa54wDYRicWHtAmPlO8JKBQ==";
            //return RoleEnvironment.GetConfigurationSettingValue("CamprQueues");
        }

        public string BlobsConnectionString()
        {
            return "DefaultEndpointsProtocol=https;AccountName=camprblobs00001;AccountKey=372gncHyQxbP5KcsoLgDred0L8+t764vwGtN+9eHbtSFCcJxc4/Tln3Qfc+mMr1cCift8DhQUyW+Zpu9dPWBdw==";
            //return RoleEnvironment.GetConfigurationSettingValue("CamprBlobs");
        }

        public string EncryptionKey()
        {
            return EncryptionKeyConst;
        }

        public string AuthCookieName()
        {
            return AuthCookieNameConst;
        }

        public string LangCookieName()
        {
            return LangCookieNameConst;
        }

        public string CacheControlHeaderName()
        {
            return CacheControlHeaderNameConst;
        }

        public TimeSpan AuthCookieExpiration()
        {
            return this.authCookieExpiration;
        }

        public TimeSpan DefaultBewitExpiration()
        {
            return this.defaultBewitExpiration;
        }

        public TimeSpan SubscriptionsQueueVisibilityTimeout()
        {
            return this.subscriptionQueueVisibilityTimeout;
        }

        public TimeSpan DiscoveryAttemptTimeout()
        {
            return this.discoveryAttemptTimeout;
        }

        public TimeSpan OutgoingRequestTimeout()
        {
            return this.outgoingRequestTimeout;
        }

        public TimeSpan[] NotificationRetryDelays()
        {
            return this.notificationRetryDelays;
        }

        public int SubscriptionsBatchSize()
        {
            return SubscriptionsBatchSizeConst;
        }

        public int MaxPostLimit()
        {
            return MaxPostLimitConst;
        }

        public int DefaultPostLimit()
        {
            return DefaultPostLimitConst;
        }

        public int MaxBatchRequests()
        {
            return MaxBatchRequestsConst;
        }

        public int MaxAttachmentImageSize()
        {
            return MaxAttachmentImageSizeConst;
        }

        public int MinAttachmentImageSize()
        {
            return MinAttachmentImageSizeConst;
        }

        public int DefaultAvatarSize()
        {
            return DefaultAvatarSizeConst;
        }

        public int MaxAttachmentResizableSize()
        {
            return MaxAttachmentResizableSizeConst;
        }

        public int MaxAttachmentCacheSize()
        {
            return MaxAttachmentCacheSizeConst;
        }

        public int MaxNotificationRetryAttempts()
        {
            return MaxNotificationRetryAttemptsConst;
        }

        public string[] AttachmentResizableTypes()
        {
            return this.attachmentResizableTypes;
        }

        public string CamprEntityBaseUrl()
        {
            return CamprEntityBaseUrlConst;
        }

        public string CamprBaseDomain()
        {
            return CamprBaseDomainConst;
        }

        public string CamprBaseUrl()
        {
            return CamprBaseUrlConst;
        }

        public string CdnBaseUrl()
        {
            return CdnBaseUrlConst;
        }

        public IDictionary<string, PostKnownTypeEnum> KnownPostTypes()
        {
            return this.knownPostTypes;
        }
    }
}