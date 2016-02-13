using System;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Models.Tent
{
    public abstract class ModelBase
    {
        public virtual JObject GetCanonicalJson(IJsonHelpers jsonHelpers)
        {
            throw new NotImplementedException("This method shouldn't be called on this object.");
        }
    }

    public abstract class DbModelBase : ModelBase
    {
        [DbProperty]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? CreatedAt { get; set; }

        public abstract string GetId();
    }

    public abstract class DbVersionModelBase : DbModelBase
    {
        [DbProperty]
        public string Id { get; set; }

        [DbProperty]
        public string VersionId { get; set; }

        [DbProperty]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? UpdatedAt { get; set; }

        public override string GetId()
        {
            return $"{this.Id}_{this.VersionId}";
        }
    }
}