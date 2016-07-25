using System;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Json;
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
        public DateTime? CreatedAt { get; set; }
    }
}