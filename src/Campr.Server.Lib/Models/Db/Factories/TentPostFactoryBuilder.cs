using System;
using System.Collections.Generic;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db.Factories
{
    class TentPostFactoryBuilder<T> : ITentPostFactoryBuilder<T> where T : ModelBase
    {
        public TentPostFactoryBuilder(
            IModelHelpers modelHelpers,
            TentPost<T> post)
        {
            Ensure.Argument.IsNotNull(modelHelpers, nameof(modelHelpers));
            Ensure.Argument.IsNotNull(post, nameof(post));

            this.modelHelpers = modelHelpers;
            this.post = post;
        }

        private readonly IModelHelpers modelHelpers;
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
            // Fill-in the missing dates for this post.
            var date = DateTime.UtcNow;
            if (!this.post.Version.PublishedAt.HasValue)
                this.post.Version.PublishedAt = date;

            if (!this.post.Version.ReceivedAt.HasValue)
                this.post.Version.ReceivedAt = date;

            if (!this.post.PublishedAt.HasValue)
                this.post.PublishedAt = date;

            if (!this.post.ReceivedAt.HasValue)
                this.post.ReceivedAt = date;

            // Compute the Version Id and set the dates.
            this.post.Version.Id = this.modelHelpers.GetVersionIdFromPost(this.post);

            // Return the post created through this builder.
            return this.post;
        }
    }
}