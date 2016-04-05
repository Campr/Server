using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Json;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentPostRef : ModelBase
    {
        public override JObject GetCanonicalJson(IJsonHelpers jsonHelpers)
        {
            var result = new JObject
            {
                { "post", this.PostId }
            };
            
            if (!string.IsNullOrWhiteSpace(this.OriginalEntity))
                result.Add("entity", this.OriginalEntity);
            else if (!string.IsNullOrWhiteSpace(this.Entity))
                result.Add("entity", this.Entity);

            if (!string.IsNullOrWhiteSpace(this.VersionId))
                result.Add("version", this.VersionId);

            if (this.Type != null)
                result.Add("type", this.Type.ToString());

            return result;
        }
        
        [DbProperty]
        public string UserId { get; set; }
        
        [DbProperty]
        public bool FoundPost { get; set; }

        [DbProperty]
        [WebProperty]
        public string Entity { get; set; }

        [DbProperty]
        [WebProperty]
        public string OriginalEntity { get; set; }
        
        [DbProperty]
        [WebProperty]
        public string PostId { get; set; }
        
        [DbProperty]
        [WebProperty]
        public string VersionId { get; set; }
        
        [DbProperty]
        [WebProperty]
        public ITentPostType Type { get; set; }
        
        public User User { get; set; }
        public TentPost<object> Post { get; set; }
    }
}