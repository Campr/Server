using System.Collections.Generic;
using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent.PostContent
{
    public class TentContentAppAuthorization : ModelBase
    {
        [DbProperty]
        [WebProperty]
        public TentAppPostTypes Types { get; set; }

        [DbProperty]
        [WebProperty]
        public List<string> Scopes { get; set; }
    }
}