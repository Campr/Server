using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent.PostContent
{
    public class TentContentMeta : ModelBase
    {
        //public override bool Validate(TentPost post)
        //{
        //    // TODO: Validate mentions & post refs (there shouldn't be any).

        //    return !(string.IsNullOrEmpty(this.Entity)
        //        || this.Servers == null
        //        || this.Servers.Count == 0
        //        || this.Servers.Any(s => !s.Validate()));
        //}

        [DbProperty]
        [WebProperty]
        public string Entity { get; set; }

        [DbProperty]
        [WebProperty]
        public IList<string> PreviousEntities { get; set; }

        [DbProperty]
        [WebProperty]
        public TentMetaProfile Profile { get; set; }

        [DbProperty]
        [WebProperty]
        public IList<TentMetaServer> Servers { get; set; }

        public bool IsUrlServerMatch(Uri uri)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var server in this.Servers)
            {
                // Find the corresponding endpoint 
                var postEndpoint = server.Urls.TryGetValue("post");
                if (postEndpoint == null)
                {
                    continue;
                }

                // Create a regex out of the endpoint and test the provided url.
                var postEndpointRegex = new Regex('^' + postEndpoint.Replace("{entity}", "(.*)").Replace("{post}", "(.*)"));
                if (postEndpointRegex.IsMatch(uri.AbsoluteUri))
                {
                    return true;
                }
            }

            return false;
        }
    }
}