﻿using Campr.Server.Lib.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentPostRef : ModelBase
    {
        public override JObject GetCanonicalJson(JsonSerializer serializer)
        {
            var result = new JObject
            {
                { "post", this.PostId }
            };
            
            if (!string.IsNullOrWhiteSpace(this.OriginalEntity))
            {
                result.Add("entity", this.OriginalEntity);
            }
            else if (!string.IsNullOrWhiteSpace(this.Entity))
            {
                result.Add("entity", this.Entity);
            }

            if (!string.IsNullOrWhiteSpace(this.VersionId))
            {
                result.Add("version", this.VersionId);
            }

            if (!string.IsNullOrWhiteSpace(this.Type))
            {
                result.Add("type", this.Type);
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
        
        [DbProperty]
        [WebProperty]
        public string Type { get; set; }
        
        public TentPost<object> Post { get; set; }
    }
}