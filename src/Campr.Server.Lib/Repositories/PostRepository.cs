using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace Campr.Server.Lib.Repositories
{
    class PostRepository : IPostRepository
    { 
        public PostRepository(
            IRethinkConnection db,
            IModelHelpers modelHelpers)
        {
            Ensure.Argument.IsNotNull(db, nameof(db));
            Ensure.Argument.IsNotNull(modelHelpers, nameof(modelHelpers));

            this.db = db;
            this.modelHelpers = modelHelpers;

            this.table = db.Posts;
            this.tableVersions = db.PostVersions;
        }

        private readonly IRethinkConnection db;
        private readonly IModelHelpers modelHelpers;

        private readonly Table table;
        private readonly Table tableVersions;

        public Task<TentPost<T>> GetAsync<T>(string userId, string postId, string versionId, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            return this.db.Run(c => this.tableVersions
                .Get(new [] { userId, postId, this.GetShortVersionId(versionId) })
                .Do_(r => this.db.R.Branch(r.HasFields("deleted_at"), null, r))
                .RunResultAsync<TentPost<T>>(c, null, cancellationToken), cancellationToken);
        }

        public Task<TentPost<T>> GetLastVersionAsync<T>(string userId, string postId, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            return this.db.Run(c => this.table
                .Get(new [] { userId, postId })
                .Do_(r => this.db.R.Branch(r.HasFields("deleted_at"), null, r))
                .RunResultAsync<TentPost<T>>(c, null, cancellationToken), cancellationToken);
        }

        public Task<TentPost<T>> GetLastVersionOfTypeAsync<T>(string userId, ITentPostType type, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            return this.db.Run(c =>
            {
                // Depending on whether this is a wildcard query or not, don't use the same index.
                var index = type.WildCard 
                    ? "user_stype_versionreceivedat"
                    : "user_ftype_versionreceivedat";

                // Perform the query.
                return this.table.Between(
                        new object[] { userId, type.ToString(), this.db.R.Minval() },
                        new object[] { userId, type.ToString(), this.db.R.Maxval() })[new { index }]
                    .OrderBy()[new { index = this.db.R.Desc(index) }]
                    .Nth(0)
                    .Default_((object)null)
                    .RunResultAsync<TentPost<T>>(c, null, cancellationToken);
            }, cancellationToken);
        }

        public Task<IList<TentPost<T>>> GetBulkAsync<T>(IList<TentPostReference> references, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            var postIds = references.Select(r => new [] { r.UserId, r.PostId, this.GetShortVersionId(r.VersionId) });
            return this.db.Run(c => this.tableVersions
                .GetAll(postIds.Cast<object>().ToArray())
                .Filter(r => this.db.R.Not(r.HasFields("deleted_at")))
                .RunResultAsync<IList<TentPost<T>>>(c, null, cancellationToken), cancellationToken);
        }

        public Task UpdateAsync<T>(TentPost<T> post, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            // Set the version receive date.
            post.Version.ReceivedAt = DateTime.UtcNow;
            
            // If needed, set the version publish date.
            if (!post.Version.PublishedAt.HasValue)
                post.Version.PublishedAt = post.Version.ReceivedAt;

            // If needed, set the post receive date.
            if (!post.ReceivedAt.HasValue)
                post.ReceivedAt = post.Version.ReceivedAt;

            // If needed, set the post publish date.
            if (!post.PublishedAt.HasValue)
                post.PublishedAt = post.Version.PublishedAt;

            // Compute the VersionId for this post.
            post.Version.Id = this.modelHelpers.GetVersionIdFromPost(post);

            return this.db.Run(async c =>
            {
                // Set the key values.
                post.KeyUserPost = null;
                post.KeyUserPostVersion = new [] { post.UserId, post.Id, this.GetShortVersionId(post.Version.Id) };

                // Start by saving this specific version.
                var versionInsertResult = await this.tableVersions
                    .Insert(post)
                    .RunResultAsync(c, null, cancellationToken);

                versionInsertResult.AssertInserted(1);

                // Set the key values.
                post.KeyUserPost = new [] { post.UserId, post.Id };
                post.KeyUserPostVersion = null;

                // Then, conditionally update the last version.
                var upsertResult = await this.table
                    .Get(new [] { post.UserId, post.Id })
                    .Replace(r => this.db.R.Branch(r.Eq(null)
                        .Or(r.HasFields("deleted_at"))
                        .Or(r.G("version").G("received_at").Lt(post.Version.ReceivedAt)
                            .Or(r.G("version").G("received_at").Eq(post.Version.ReceivedAt).And(r.G("version").G("id").Lt(post.Version.Id)))),
                        post, r))
                    .RunResultAsync(c, null, cancellationToken);

                upsertResult.AssertNoErrors();
            }, cancellationToken);
        }

        public Task DeleteAsync<T>(TentPost<T> post, CancellationToken cancellationToken = new CancellationToken()) where T : class
        {
            return this.DeleteAsync(post.UserId, post.Id, null, cancellationToken);
        }

        public Task DeleteAsync<T>(TentPost<T> post, bool specificVersion = false, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            return this.DeleteAsync(post.UserId, post.Id, post.Version.Id, cancellationToken);
        }

        public Task DeleteAsync(string userId, string postId, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.DeleteAsync(userId, postId, null, cancellationToken);
        }

        public Task DeleteAsync(string userId, string postId, string versionId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Make sure we use the date for all.
            var deletedAt = DateTime.UtcNow;

            return this.db.Run(async c =>
            {
                // If a specific version was specified, retrieve the penultimate version.
                var postVersionReceivedAtIndex = "user_post_versionreceivedat";
                var penultimatePostVersion = string.IsNullOrWhiteSpace(versionId)
                    ? null
                    : await this.tableVersions
                        .Between(
                            new object[] { userId, postId, this.db.R.Minval() },
                            new object[] { userId, postId, this.db.R.Maxval() })[new { index = postVersionReceivedAtIndex }]
                        .OrderBy()[new { index = this.db.R.Desc(postVersionReceivedAtIndex) }]
                        .Nth(1)
                        .Default_((object)null)
                        .RunResultAsync<TentPost<object>>(c, null, cancellationToken);

                // If a penultimate version was found, prepare for insertion in different table.
                if (penultimatePostVersion != null)
                {
                    penultimatePostVersion.KeyUserPost = new [] { userId, postId };
                    penultimatePostVersion.KeyUserPostVersion = null;
                }

                // Depending on the result, either update the last version, or replace it with the penultimate.
                var lastVersionUpdateResult = penultimatePostVersion == null
                    ? await this.table
                        .Get(new [] { userId, postId })
                        .Update(r => this.db.R.Branch(string.IsNullOrWhiteSpace(versionId) 
                                ? (object)r.Ne(null) 
                                : (object)r.Ne(null).And(r.G("version").G("id").Eq(versionId))
                            , new { deletedAt }, null))
                        .RunResultAsync(c, null, cancellationToken)
                    : await this.table
                        .Get(new [] { userId, postId })
                        .Replace(r => this.db.R.Branch(r.Ne(null).And(r.G("version").G("id").Eq(versionId)), penultimatePostVersion, r))
                        .RunResultAsync(c, null, cancellationToken);

                lastVersionUpdateResult.AssertNoErrors();

                // Depending of whether a version id was specified, update one or more versions.
                var versionUpdateResult = await (string.IsNullOrWhiteSpace(versionId)
                    ? this.tableVersions.Between(
                            new object[] { userId, postId, this.db.R.Minval() },
                            new object[] { userId, postId, this.db.R.Maxval() })
                        .Update(new { deletedAt })
                    : this.tableVersions.Get(new [] { userId, postId, this.GetShortVersionId(versionId) })
                        .Update(new { deletedAt }))
                    .RunResultAsync(c, null, cancellationToken);

                versionUpdateResult.AssertNoErrors();
            }, cancellationToken);
        }

        // Don't use the full version for a post's primary key.
        private string GetShortVersionId(string versionId)
        {
            return versionId.Substring(versionId.Length - 32, 32);
        }
    }
}