using System.Collections.Generic;
using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent.PostContent
{
    public class TentContentApplication : ModelBase
    {
        [DbProperty]
        [WebProperty]
        public string Name { get; set; }

        [DbProperty]
        [WebProperty]
        public string Description { get; set; }

        [DbProperty]
        [WebProperty]
        public string Url { get; set; }

        [DbProperty]
        [WebProperty]
        public string IconUrl { get; set; }

        [DbProperty]
        [WebProperty]
        public string RedirectUri { get; set; }

        [DbProperty]
        [WebProperty]
        public string NotificationUrl { get; set; }

        [DbProperty]
        [WebProperty]
        public List<string> NotificationTypes { get; set; }
 
        [DbProperty]
        [WebProperty]
        public TentAppPostTypes Types { get; set; }

        [DbProperty]
        [WebProperty]
        public IList<string> Scopes { get; set; } 
    }
}