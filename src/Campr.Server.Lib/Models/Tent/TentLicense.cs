using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentLicense : ModelBase
    {
        [DbProperty]
        [WebProperty]
        public string Url { get; set; }
    }
}