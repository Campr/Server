using System;
using System.Collections.Generic;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Configuration
{
    class GeneralConfiguration : IGeneralConfiguration
    {
        public GeneralConfiguration(IExternalConfiguration externalConfiguration)
        {
            Ensure.Argument.IsNotNull(externalConfiguration, nameof(externalConfiguration));
            this.externalConfiguration = externalConfiguration;
        }

        private readonly IExternalConfiguration externalConfiguration;
        
        public string AzureQueuesConnectionString => this.externalConfiguration.AzureQueuesConnectionString;
        public string AzureBlobsConnectionString => this.externalConfiguration.AzureBlobsConnectionString;
        public string EncryptionKey => this.externalConfiguration.EncryptionKey;
        public AppEnvironment AppEnvironment => this.externalConfiguration.AppEnvironment;
        public IEnumerable<Uri> CouchBaseServers => this.externalConfiguration.CouchBaseServers;
        public bool ConfigureBucket => this.externalConfiguration.ConfigureBucket;
        public string BucketConfigurationPath => this.externalConfiguration.BucketConfigurationPath;
        public string BucketAdministratorUsername => this.externalConfiguration.BucketAdministratorUsername;
        public string BucketAdministratorPassword => this.externalConfiguration.BucketAdministratorPassword;
        
        public string AuthCookieName { get; } = "campr_auth";
        public string LangCookieName { get; } = "campr_lang";
        public string CacheControlHeaderName { get; } = "X-Cache-Control";
        public string CamprEntityBaseUrl { get; } = "https://{0}.campr.me";
        public string CamprBaseDomain { get; } = "campr.me";
        public string CamprBaseUrl { get; } = "https://campr.me";
        public string CdnBaseUrl { get; } = "https://az557631.vo.msecnd.net";

        public uint SubscriptionsBatchSize { get; } = 1000;
        public uint MaxPostLimit { get; } = 200;
        public uint DefaultPostLimit { get; } = 25;
        public uint MaxRefs { get; } = 200;
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

        //public IDictionary<string, PostKnownTypeEnum> KnownPostTypes { get; } = new Dictionary<string, PostKnownTypeEnum>
        //{
        //    { "https://tent.io/types/app/v0" , PostKnownTypeEnum.Application },
        //    { "https://tent.io/types/credentials/v0", PostKnownTypeEnum.Credentials },
        //    { "https://tent.io/types/cursor/v0", PostKnownTypeEnum.Cursor },
        //    { "https://tent.io/types/delete/v0", PostKnownTypeEnum.Delete },
        //    { "https://tent.io/types/essay/v0", PostKnownTypeEnum.Essay },
        //    { "https://tent.io/types/meta/v0", PostKnownTypeEnum.Meta },
        //    { "https://tent.io/types/relationship/v0", PostKnownTypeEnum.Relationship },
        //    { "https://tent.io/types/repost/v0", PostKnownTypeEnum.Repost },
        //    { "https://tent.io/types/status/v0", PostKnownTypeEnum.Status },
        //    { "https://tent.io/types/subscription/v0", PostKnownTypeEnum.Subscription },
        //    { "https://tent.io/types/tag/v0", PostKnownTypeEnum.Tag }
        //};
    }
}