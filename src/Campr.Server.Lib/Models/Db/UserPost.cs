using System;
using System.Collections.Generic;
using Campr.Server.Lib.Json;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db
{
    public class UserPost : ModelBase
    {
        [DbProperty]
        public string[] KeyOwnerUserPost { get; set; }

        [DbProperty]
        public string[] KeyOwnerUserPostVersion { get; set; }

        [DbProperty]
        public string OwnerId { get; set; }

        [DbProperty]
        public string UserId { get; set; }

        [DbProperty]
        public string PostId { get; set; }

        [DbProperty]
        public string VersionId { get; set; }

        [DbProperty]
        public DateTime VersionReceivedAt { get; set; }

        [DbProperty]
        public DateTime VersionPublishedAt { get; set; }

        [DbProperty]
        public DateTime ReceivedAt { get; set; }

        [DbProperty]
        public DateTime PublishedAt { get; set; }

        [DbProperty]
        public DateTime? DeletedAt { get; set; }

        [DbProperty]
        public string Type { get; set; }

        [DbProperty]
        public IList<UserPostMention> Mentions { get; set; }
    }
}
