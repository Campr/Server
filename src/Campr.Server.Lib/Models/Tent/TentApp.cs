using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Json;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentApp : ModelBase
    {
        public override JObject GetCanonicalJson(IJsonHelpers jsonHelpers)
        {
            var result = new JObject();

            if (!string.IsNullOrWhiteSpace(this.Name))
            {
                result.Add("name", this.Name);
            }

            if (!string.IsNullOrWhiteSpace(this.Url))
            {
                result.Add("url", this.Url);
            }

            return result;
        }

        [DbProperty]
        [WebProperty]
        public string Id { get; set; }

        [DbProperty]
        [WebProperty]
        public string Url { get; set; }

        [DbProperty]
        [WebProperty]
        public string Name { get; set; }
    }
}