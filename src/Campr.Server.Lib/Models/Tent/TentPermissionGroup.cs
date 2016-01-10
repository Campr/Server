using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentPermissionGroup : ModelBase
    {
        [DbProperty]
        [WebProperty]
        public string PostId { get; set; }
    }
}