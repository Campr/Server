using Campr.Server.Lib.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentVersionParent : ModelBase
    {
        public override JObject GetCanonicalJson(JsonSerializer serializer)
        {
            // Create the canonical Json object.
            var result = new JObject
            {
                { "version", this.VersionId }
            };

            if (!string.IsNullOrEmpty(this.OriginalEntity))
            {
                result.Add("entity", this.OriginalEntity);
            }
            else if (!string.IsNullOrEmpty(this.Entity))
            {
                result.Add("entity", this.Entity);
            }

            if (!string.IsNullOrEmpty(this.PostId))
            {
                result.Add("post", this.PostId);
            }

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
        
        public TentPost<object> Post { get; set; }
    }
}