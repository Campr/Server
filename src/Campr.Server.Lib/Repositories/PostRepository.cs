using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace Campr.Server.Lib.Repositories
{
    class PostRepository : IPostRepository
    { 
        public PostRepository(IRethinkConnection db)
        {
            Ensure.Argument.IsNotNull(db, nameof(db));
            this.db = db;

            this.table = db.Posts;
            this.tableVersions = db.PostVersions;
        }

        private readonly IRethinkConnection db;
        private readonly Table table;
        private readonly Table tableVersions;
        
        public Task<TentPost<T>> GetLastVersionAsync<T>(string userId, string postId, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            return this.db.Run(c => this.tableVersions.Get(new[] { userId, postId }).RunResultAsync<TentPost<T>>(c, null, cancellationToken), cancellationToken);
        }

        public Task<TentPost<T>> GetLastVersionByTypeAsync<T>(string userId, ITentPostType type, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            return this.db.Run(c =>
            {
                // Depending on whether this is a wildcard query or not, don't use the same index.
                var index = type.WildCard 
                    ? "user_stype_updatedat" 
                    : "user_ftype_updatedat";

                // Perform the query.
                return this.table.Between(
                        new object[] { userId, type.ToString(), this.db.R.Minval() },
                        new object[] { userId, type.ToString(), this.db.R.Maxval() })[new { index }]
                    .OrderBy()[new { index }]
                    .Nth(0)
                    .RunResultAsync<TentPost<T>>(c, null, cancellationToken);
            }, cancellationToken);
        }

        public Task<TentPost<T>> GetAsync<T>(string userId, string postId, string versionId, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            return this.db.Run(c => this.tableVersions.Get(new[] { userId, postId, versionId }).RunResultAsync<TentPost<T>>(c, null, cancellationToken), cancellationToken);
        }

        public Task<IList<TentPost<T>>> GetBulkAsync<T>(IList<TentPostReference> references, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            var postIds = references.Select(r => new [] { r.UserId, r.PostId, r.VersionId });
            return this.db.Run(c => this.table.GetAll(postIds).RunResultAsync<IList<TentPost<T>>>(c, null, cancellationToken), cancellationToken);
        }

        public Task UpdateAsync<T>(TentPost<T> post, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            return this.db.Run(async c =>
            {
                // Start by saving this specific version.
                var versionInsertResult = await this.tableVersions
                    .Insert(post)
                    .RunResultAsync(c, null, cancellationToken);

                versionInsertResult.AssertInserted(1);

                // Then, conditionally update the last version.
                var upsertResult = await this.table
                    .Get(new [] { post.UserId, post.Id, post.Version.Id })
                    .Replace(r => this.db.R.Branch(r.Eq(null)
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
                // Update the last version.
                var lastVersionUpdateResult = await this.table
                    .Get(new[] { userId, postId })
                    .Update(r => this.db.R.Branch(r.Ne(null).And(r.G("version").Eq(versionId)), new { deletedAt }, null))
                    .RunResultAsync(c, null, cancellationToken);

                lastVersionUpdateResult.AssertNoErrors();

                // Depending of whether a version id was specified, update one or more versions.
                var versionUpdateResult = await (string.IsNullOrWhiteSpace(versionId)
                    ? this.tableVersions.Between(
                            new object[] { userId, postId, this.db.R.Minval() },
                            new object[] { userId, postId, this.db.R.Maxval() })
                        .Update(new { deletedAt })
                    : this.tableVersions.Get(new { userId, postId, versionId })
                        .Update(new { deletedAt }))
                    .RunResultAsync(c, null, cancellationToken);

                versionUpdateResult.AssertNoErrors();
            }, cancellationToken);
        }
    }
}