using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Db
{
    public class UserPostMention
    {
        [DbProperty]
        public string UserId { get; set; }

        [DbProperty]
        public string PostId { get; set; }

        [DbProperty]
        public string VersionId { get; set; }
    }
}