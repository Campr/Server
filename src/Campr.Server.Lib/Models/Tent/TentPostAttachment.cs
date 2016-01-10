using Campr.Server.Lib.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentPostAttachment : ModelBase
    {
        public override JObject GetCanonicalJson(JsonSerializer serializer)
        {
            return new JObject
            {
                { "category", this.Category },
                { "content_type", this.ContentType },
                { "name", this.Name },
                { "digest", this.Digest },
                { "size", this.Size }
            };
        }

        [DbProperty]
        [WebProperty]
        public string Category { get; set; }
        
        [DbProperty]
        [WebProperty]
        public string ContentType { get; set; }
        
        [DbProperty]
        [WebProperty]
        public string Name { get; set; }
        
        [DbProperty]
        [WebProperty]
        public string Digest { get; set; }
        
        [DbProperty]
        [WebProperty]
        public long Size { get; set; }
        
        public byte[] Data { get; set; }
    }
}