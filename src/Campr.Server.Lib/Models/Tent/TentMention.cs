﻿using System.Collections.Generic;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Json;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentMention : ModelBase
    {
        public override JObject GetCanonicalJson(IJsonHelpers jsonHelpers)
        {
            var result = new JObject();

            if (!string.IsNullOrWhiteSpace(this.OriginalEntity))
                result.Add("entity", this.OriginalEntity);
            else if (!string.IsNullOrWhiteSpace(this.Entity))
                result.Add("entity", this.Entity);

            if (!string.IsNullOrWhiteSpace(this.PostId))
                result.Add("post", this.PostId);

            if (this.Type != null)
                result.Add("type", this.Type.ToString());

            if (!string.IsNullOrWhiteSpace(this.VersionId))
                result.Add("version", this.VersionId);

            if (this.Public.HasValue && !this.Public.Value)
                result.Add("public", false);

            return result;
        }

        public bool Validate()
        {
            return true;
        }  
        
        [DbProperty]
        public string UserId { get; set; }
        
        [DbProperty]
        public bool FoundPost { get; set; }

        [DbProperty]
        public List<TentPostIdentifier> ReplyChain { get; set; }
        
        [WebProperty]
        public string Entity { get; set; }
        
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
        
        [DbProperty]
        [WebProperty]
        public bool? Public { get; set; }
        
        public User User { get; set; }
        public TentPost<object> Post { get; set; }
    }
}