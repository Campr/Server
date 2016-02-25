using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Models.Tent;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace Campr.Server.Lib.Repositories
{
    class UserPostRepository : IUserPostRepository
    {
        public UserPostRepository(
            IRethinkConnection db, 
            IModelHelpers modelHelpers, 
            IUserPostFactory userPostFactory)
        {
            Ensure.Argument.IsNotNull(db, nameof(db));
            Ensure.Argument.IsNotNull(modelHelpers, nameof(modelHelpers));
            Ensure.Argument.IsNotNull(userPostFactory, nameof(userPostFactory));

            this.db = db;
            this.modelHelpers = modelHelpers;
            this.userPostFactory = userPostFactory;

            this.table = db.UserPosts;
            this.tableVersions = db.UserPostVersions;
        }

        private readonly IRethinkConnection db;
        private readonly IModelHelpers modelHelpers;
        private readonly IUserPostFactory userPostFactory;

        private readonly Table table;
        private readonly Table tableVersions;
        
        public Task<UserPost> GetAsync(string ownerId, string userId, string postId, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.db.Run(c => this.table
                .Get(new[] { ownerId, userId, postId })
                .Do_(r => this.db.R.Branch(r.HasFields("deleted_at"), null, r))
                .Default_((object)null)
                .RunResultAsync<UserPost>(c, null, cancellationToken), cancellationToken);
        }

        public Task<UserPost> GetAsync(string ownerId, string userId, string postId, string versionId, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.db.Run(c => this.tableVersions
                .Get(new[] { ownerId, userId, postId, this.modelHelpers.GetShortVersionId(versionId) })
                .Do_(r => this.db.R.Branch(r.HasFields("deleted_at"), null, r))
                .Default_((object)null)
                .RunResultAsync<UserPost>(c, null, cancellationToken), cancellationToken);
        }

        public Task UpdateAsync(string ownerId, TentPost post, CancellationToken cancellationToken = new CancellationToken())
        {
            // Create a new UserPost from the provided post.
            var userPost = this.userPostFactory.FromPost(ownerId, post);

            return this.db.Run(async c =>
            {
                // Set the key values.
                userPost.KeyOwnerUserPost = null;
                userPost.KeyOwnerUserPostVersion = new [] { ownerId, userPost.UserId, userPost.PostId, this.modelHelpers.GetShortVersionId(userPost.VersionId) };

                // Start by saving this specific version.
                var versionInsertResult = await this.tableVersions
                    .Insert(userPost)
                    .RunResultAsync(c, null, cancellationToken);

                versionInsertResult.AssertInserted(1);

                // Set the key values.
                userPost.KeyOwnerUserPost = new [] { ownerId, post.UserId, post.Id };
                userPost.KeyOwnerUserPostVersion = null;

                // Then, conditionally update the last version.
                var upsertResult = await this.table
                    .Get(userPost.KeyOwnerUserPost)
                    .Replace(r => this.db.R.Branch(r.Eq(null)
                        .Or(r.HasFields("deleted_at"))
                        .Or(r.G("version_received_at").Lt(userPost.VersionReceivedAt)
                            .Or(r.G("version_received_at").Eq(userPost.VersionReceivedAt).And(r.G("version").Lt(userPost.VersionId)))),
                        userPost, r))
                    .RunResultAsync(c, null, cancellationToken);

                upsertResult.AssertNoErrors();
            }, cancellationToken);
        }

        public Task DeleteAsync(string ownerId, TentPost post, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.DeleteAsync(ownerId, post, false, cancellationToken);
        }

        public Task DeleteAsync(string ownerId, TentPost post, bool specificVersion, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.db.Run(async c =>
            {
                var deletedAt = new { deletedAt = post.DeletedAt.GetValueOrDefault() };

                // If a specific version was specified, retrieve the penultimate version.
                var postVersionReceivedAtIndex = "owner_user_post_versionreceivedat";
                var penultimatePostVersion = !specificVersion
                    ? null
                    : await this.tableVersions
                        .Between(
                            new object[] { ownerId, post.UserId, post.Id, this.db.R.Minval() },
                            new object[] { ownerId, post.UserId, post.Id, this.db.R.Maxval() })[new { index = postVersionReceivedAtIndex }]
                        .OrderBy()[new { index = this.db.R.Desc(postVersionReceivedAtIndex) }]
                        .Nth(1)
                        .Default_((object)null)
                        .RunResultAsync<UserPost>(c, null, cancellationToken);

                // If a penultimate version was found, prepare for insertion in different table.
                if (penultimatePostVersion != null)
                {
                    penultimatePostVersion.KeyOwnerUserPost = new [] { ownerId, post.UserId, post.Id };
                    penultimatePostVersion.KeyOwnerUserPostVersion = null;
                }

                // Depending on the result, either update the last version, or replace it with the penultimate.
                var lastVersionUpdateResult = penultimatePostVersion == null
                    ? await this.table
                        .Get(new[] { ownerId, post.UserId, post.Id })
                        .Update(r => this.db.R.Branch(!specificVersion
                                ? (object)r.Ne(null) 
                                : (object)r.Ne(null).And(r.G("version").Eq(post.Version.Id))
                            , deletedAt, null))
                        .RunResultAsync(c, null, cancellationToken)
                    : await this.table
                        .Get(new[] { ownerId, post.UserId, post.Id })
                        .Replace(r => this.db.R.Branch(r.Ne(null).And(r.G("version").Eq(post.Version.Id)), penultimatePostVersion, r))
                        .RunResultAsync(c, null, cancellationToken);

                lastVersionUpdateResult.AssertNoErrors();

                // Depending of whether a version id was specified, update one or more versions.
                var versionUpdateResult = await (!specificVersion
                    ? this.tableVersions.Between(
                            new object[] { ownerId, post.UserId, post.Id, this.db.R.Minval() },
                            new object[] { ownerId, post.UserId, post.Id, this.db.R.Maxval() })
                        .Update(deletedAt)
                    : this.tableVersions.Get(new[] { ownerId, post.UserId, post.Id, this.modelHelpers.GetShortVersionId(post.Version.Id) })
                        .Update(deletedAt))
                    .RunResultAsync(c, null, cancellationToken);

                versionUpdateResult.AssertNoErrors();
            }, cancellationToken);
        }
    }
}