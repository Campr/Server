using System;
using System.Collections.Generic;
using System.Linq;
using Campr.Server.Lib.Extensions;
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
        private List<User> permissionsUsers;

        public ITentPostFactoryBuilder<T> WithMentions(params TentMention[] mentions)
        {
            Ensure.Argument.IsNotNull(mentions, nameof(mentions));
            this.post.Mentions = mentions.ToList();

            // Add the mentioned users to the permissions.
            (this.permissionsUsers ?? (this.permissionsUsers = new List<User>())).AddRange(mentions.Select(m => m.User));

            return this;
        }

        public ITentPostFactoryBuilder<T> WithPostRefs(params TentPostRef[] postRefs)
        {
            Ensure.Argument.IsNotNull(postRefs, nameof(postRefs));
            this.post.Refs = postRefs.ToList();
            return this;
        }

        public ITentPostFactoryBuilder<T> WithAttachments(params TentPostAttachment[] attachments)
        {
            Ensure.Argument.IsNotNull(attachments, nameof(attachments));
            this.post.Attachments = attachments.ToList();
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

            // Normalize dates to milliseconds.
            this.post.Version.PublishedAt = this.post.Version.PublishedAt.Value.TruncateToMilliseconds();
            this.post.Version.ReceivedAt = this.post.Version.ReceivedAt.Value.TruncateToMilliseconds();
            this.post.PublishedAt = this.post.PublishedAt.Value.TruncateToMilliseconds();
            this.post.ReceivedAt = this.post.ReceivedAt.Value.TruncateToMilliseconds();

            // If the post is private, add the cached User Ids to the permissions.
            if (!this.post.Permissions.Public.GetValueOrDefault(true))
            {
                var users = this.permissionsUsers?.Where(u => u != null).Distinct().ToList();
                this.post.Permissions.UserIds = users?.Select(u => u.Id).ToList();
                this.post.Permissions.Entities = users?.Select(u => u.Entity).ToList();
            }

            // Compute the Version Id and set the dates.
            this.post.Version.Id = this.modelHelpers.GetVersionIdFromPost(this.post);

            // Return the post created through this builder.
            return this.post;
        }
    }
}