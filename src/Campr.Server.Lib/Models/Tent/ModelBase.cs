using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Models.Tent
{
    public abstract class ModelBase
    {
        public virtual JObject GetCanonicalJson(JsonSerializer serializer)
        {
            throw new NotImplementedException("This method shouldn't be called on this object.");
        }
    }
}