using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent.PostContent
{
    public class TentContentSubscription : ModelBase
    {
        //public override bool Validate(TentPost post)
        //{
        //    return !string.IsNullOrWhiteSpace(this.Type);
        //}

        [DbProperty]
        [WebProperty]
        public string Type { get; set; }
    }
}