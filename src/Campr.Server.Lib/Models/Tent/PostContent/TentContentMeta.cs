using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Campr.Server.Lib.Enums;
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
            // No need to look further if we don't have a servers collection.
            if (this.Servers == null)
                return false;

            // Check if we have at least one match.
            return this.Servers.Any(s =>
            {
                // Find the corresponding endpoint.
                var postEndpoint = s.GetEndpoint(TentMetaEndpointEnum.Post);
                if (string.IsNullOrWhiteSpace(postEndpoint))
                    return false;

                // Create a Regex to test the provided Uri.
                var postEndpointRegex = new Regex('^' + postEndpoint.Replace("{entity}", "(.*)").Replace("{post}", "(.*)"));
                return postEndpointRegex.IsMatch(uri.AbsoluteUri);
            });
        }
    }
}