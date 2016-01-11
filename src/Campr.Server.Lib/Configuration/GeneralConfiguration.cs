using System;
using System.Collections.Generic;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Configuration
{
    class GeneralConfiguration : IGeneralConfiguration
    {
        public GeneralConfiguration(ISensitiveConfiguration sensitiveConfiguration)
        {
            Ensure.Argument.IsNotNull(sensitiveConfiguration, nameof(sensitiveConfiguration));
            this.sensitiveConfiguration = sensitiveConfiguration;
        }

        private readonly ISensitiveConfiguration sensitiveConfiguration;
        
        public string AzureQueuesConnectionString => this.sensitiveConfiguration.AzureQueuesConnectionString;
        public string AzureBlobsConnectionString => this.sensitiveConfiguration.AzureBlobsConnectionString;
        public string EncryptionKey => this.sensitiveConfiguration.EncryptionKey;

        public Uri[] CouchBaseServers { get; } = {
            new Uri("http://localhost:8091/pools"), 
        };

        public string MainBucketName { get; } = "camprdb-dev";
        public string AuthCookieName { get; } = "campr_auth";
        public string LangCookieName { get; } = "campr_lang";
        public string CacheControlHeaderName { get; } = "X-Cache-Control";
        public string CamprEntityBaseUrl { get; } = "https://{0}.campr.me";
        public string CamprBaseDomain { get; } = "campr.me";
        public string CamprBaseUrl { get; } = "https://campr.me";
        public string CdnBaseUrl { get; } = "https://az557631.vo.msecnd.net";

        public int SubscriptionsBatchSize { get; } = 1000;
        public int MaxPostLimit { get; } = 200;
        public int DefaultPostLimit { get; } = 25;
        public int MaxBatchRequests { get; } = 50;
        public int MaxAttachmentImageSize { get; } = 2000;
        public int MinAttachmentImageSize { get; } = 10;
        public int DefaultAvatarSize { get; } = 100;
        public int MaxAttachmentResizableSize { get; } = 20971520;  // 20MB.
        public int MaxAttachmentCacheSize { get; } = 20971520;      // 20MB.
        public int MaxNotificationRetryAttempts { get; } = 3;
        public string[] AttachmentResizableTypes { get; } = {
            "image/jpeg",
            "image/jpg",
            "image/png"
        };

        public TimeSpan AuthCookieExpiration { get; } = TimeSpan.FromDays(30);
        public TimeSpan DefaultBewitExpiration { get; } = TimeSpan.FromMinutes(30);
        public TimeSpan SubscriptionsQueueVisibilityTimeout { get; } = TimeSpan.FromMinutes(20);
        public TimeSpan DiscoveryAttemptTimeout { get; } = TimeSpan.FromMinutes(10);
        public TimeSpan OutgoingRequestTimeout { get; } = TimeSpan.FromSeconds(5);
        public TimeSpan[] NotificationRetryDelays { get; } = {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(5)
        };

        public IDictionary<string, PostKnownTypeEnum> KnownPostTypes { get; } = new Dictionary<string, PostKnownTypeEnum>
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
    }
}