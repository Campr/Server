using Campr.Server.Lib.Json;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db
{
    public class Attachment : DbModelBase
    {
        [DbProperty]
        public string Digest { get; set; }

        [DbProperty]
        public long Size { get; set; }

        [DbProperty]
        public string ContentType { get; set; }
    }
}