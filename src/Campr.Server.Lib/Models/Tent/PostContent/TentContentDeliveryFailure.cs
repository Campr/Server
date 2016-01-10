using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent.PostContent
{
    public class TentContentDeliveryFailure : ModelBase
    {
        [DbProperty]
        [WebProperty]
        public string Entity { get; set; }

        [DbProperty]
        [WebProperty]
        public string Status { get; set; }

        [DbProperty]
        [WebProperty]
        public string Reason { get; set; }
    }
}