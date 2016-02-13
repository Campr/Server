using System;
using System.Collections.Generic;
using System.Linq;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentVersion : ModelBase
    {
        public override JObject GetCanonicalJson(IJsonHelpers jsonHelpers)
        {
            var result = new JObject
            {
                { "published_at", this.PublishedAt.GetValueOrDefault().ToUnixTime() },
            };

            if (this.Parents != null && this.Parents.Any())
            {
                result.Add("parents", jsonHelpers.FromObject(this.Parents.Select(p => p.GetCanonicalJson(jsonHelpers))));
            }

            return result;
        }

        public bool Validate()
        {
            return !string.IsNullOrEmpty(this.Id);
        }

        public void ResponseClean()
        {
            this.ReceivedAt = null;
        }

        [DbProperty]
        public string UserId { get; set; }

        [DbProperty]
        [WebProperty]
        public string Id { get; set; }
        
        [DbProperty]
        [WebProperty]
        public IList<TentVersionParent> Parents { get; set; }
        
        [DbProperty]
        [WebProperty]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? PublishedAt { get; set; }
        
        [DbProperty]
        [WebProperty]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? ReceivedAt { get; set; }
        
        [DbProperty]
        [WebProperty]
        public string Message { get; set; }

        [DbProperty]
        [WebProperty]
        public string Type { get; set; }

        [DbProperty]
        [WebProperty]
        public string Entity { get; set; }
        
        [DbProperty]
        [WebProperty]
        public string PostId { get; set; }
    }
}