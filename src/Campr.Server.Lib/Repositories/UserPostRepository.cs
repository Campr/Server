using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using RethinkDb.Driver.Ast;

namespace Campr.Server.Lib.Repositories
{
    class UserPostRepository : IUserPostRepository
    {
        public UserPostRepository(
            IRethinkConnection db, 
            IModelHelpers modelHelpers)
        {
            Ensure.Argument.IsNotNull(db, nameof(db));
            Ensure.Argument.IsNotNull(modelHelpers, nameof(modelHelpers));

            this.db = db;
            this.modelHelpers = modelHelpers;

            this.table = db.UserPosts;
            this.tableVersions = db.UserPostVersions;
        }

        private readonly IRethinkConnection db;
        private readonly IModelHelpers modelHelpers;

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
    }
}