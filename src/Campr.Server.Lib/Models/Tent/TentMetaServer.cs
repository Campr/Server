using System;
using System.Collections.Generic;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Extensions;
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

        public string GetEndpoint(TentMetaEndpointEnum endpoint)
        {
            string endpointName;
            switch (endpoint)
            {
                case TentMetaEndpointEnum.OAuthAuth:
                    endpointName = "oauth_auth";
                    break;
                case TentMetaEndpointEnum.OAuthToken:
                    endpointName = "oauth_token";
                    break;
                case TentMetaEndpointEnum.PostsFeed:
                    endpointName = "posts_feed";
                    break;
                case TentMetaEndpointEnum.NewPost:
                    endpointName = "new_post";
                    break;
                case TentMetaEndpointEnum.Post:
                    endpointName = "post";
                    break;
                case TentMetaEndpointEnum.PostAttachment:
                    endpointName = "post_attachment";
                    break;
                case TentMetaEndpointEnum.Attachment:
                    endpointName = "attachment";
                    break;
                case TentMetaEndpointEnum.Batch:
                    endpointName = "batch";
                    break;
                case TentMetaEndpointEnum.ServerInfo:
                    endpointName = "server_info";
                    break;
                case TentMetaEndpointEnum.Discover:
                    endpointName = "discover";
                    break;
                default:
                    throw new Exception("The specified endpoint type couldn't be found.");
            }

            return this.Urls.TryGetValue(endpointName);
        }
    }
}