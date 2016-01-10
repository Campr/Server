using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent.PostContent
{
    public class TentContentCredentials : ModelBase
    {
        [DbProperty]
        [WebProperty]
        public string HawkKey { get; set; }

        [DbProperty]
        [WebProperty]
        public string HawkAlgorithm { get; set; }
    }
}