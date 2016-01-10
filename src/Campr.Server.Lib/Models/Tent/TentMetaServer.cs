using System.Collections.Generic;
using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentMetaServer : ModelBase
    {
        [DbProperty]
        [WebProperty]
        public string Version { get; set; }
        
        [DbProperty]
        [WebProperty]
        public int Preference { get; set; }
        
        [DbProperty]
        [WebProperty]
        public IDictionary<string, string> Urls { get; set; }
    }
}