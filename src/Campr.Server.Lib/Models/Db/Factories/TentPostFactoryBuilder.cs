using System.Collections.Generic;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db.Factories
{
    class TentPostFactoryBuilder<T> : ITentPostFactoryBuilder<T> where T : ModelBase
    {
        public TentPostFactoryBuilder(TentPost<T> post)
        {
            Ensure.Argument.IsNotNull(post, nameof(post));
            this.post = post;
        }
        
        private readonly TentPost<T> post; 

        public ITentPostFactoryBuilder<T> WithMentions(IList<TentMention> mentions)
        {
            this.post.Mentions = mentions;
            return this;
        }

        public ITentPostFactoryBuilder<T> WithPostRefs(IList<TentPostRef> postRefs)
        {
            this.post.Refs = postRefs;
            return this;
        }

        public ITentPostFactoryBuilder<T> WithAttachments(IList<TentPostAttachment> attachments)
        {
            this.post.Attachments = attachments;
            return this;
        }

        public ITentPostFactoryBuilder<T> WithPublic(bool isPublic)
        {
            this.post.Permissions.Public = isPublic;
            return this;
        }

        public TentPost<T> Post()
        {
            return this.post;
        }
    }
}