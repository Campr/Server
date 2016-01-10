using System.Collections.Generic;
using System.Linq;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentSinglePostResult<T> : TentPostResult<T> where T : class 
    {

    }
    
    public class TentPostResult<T> : ModelBase where T : class 
    {
        public void Combine(TentPostResult<T> postResult)
        {
            if (postResult == null)
            {
                return;
            }

            // Count.
            if (postResult.Count.HasValue)
            {
                this.Count = this.Count.GetValueOrDefault() + postResult.Count.Value;
            }

            // Posts.
            if (postResult.Posts != null)
            {
                this.Posts = (this.Posts ?? new List<TentPost<T>>()).Concat(postResult.Posts).ToList();
            }
            // Mentions.
            else if (postResult.Mentions != null)
            {
                this.Mentions = (this.Mentions ?? new List<TentMention>()).Concat(postResult.Mentions).ToList();
            }
            // Versions.
            else if (postResult.Versions != null)
            {
                this.Versions = (this.Versions ?? new List<TentVersion>()).Concat(postResult.Versions).ToList();
            }
        }

        [WebProperty]
        public TentPostPages Pages { get; set; }

        [WebProperty]
        public IList<TentMention> Mentions { get; set; }

        [WebProperty]
        public IList<TentVersion> Versions { get; set; }

        [WebProperty]
        public IDictionary<string, TentMetaProfile> Profiles { get; set; }

        [WebProperty]
        public TentPost<T> Post { get; set; }

        [WebProperty]
        public IList<TentPost<T>> Posts { get; set; }

        [WebProperty]
        public IList<TentPost<object>> PostRefs { get; set; }

        public ApiContentTypeEnum? ContentType { get; set; }

        public int? Count { get; set; }
    }
}