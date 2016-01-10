using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentMetaProfile : ModelBase
    {
        [DbProperty]
        [WebProperty]
        public string Name { get; set; }

        [DbProperty]
        [WebProperty]
        public string Website { get; set; }

        [DbProperty]
        [WebProperty]
        public string Location { get; set; }

        [DbProperty]
        [WebProperty]
        public string Biography { get; set; }

        [DbProperty]
        [WebProperty]
        public string AvatarDigest { get; set; }
    }
}