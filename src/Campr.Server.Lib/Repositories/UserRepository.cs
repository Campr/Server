using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace Campr.Server.Lib.Repositories
{
    class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(
            IRethinkConnection db, 
            ITextHelpers textHelpers) : base(db, db.Users)
        {
            Ensure.Argument.IsNotNull(textHelpers, nameof(textHelpers));
            this.textHelpers = textHelpers;
            this.tableVersions = db.UserVersions;
        }

        private readonly ITextHelpers textHelpers;
        private readonly Table tableVersions;

        public Task<string> GetIdFromHandleAsync(string handle, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.Db.Run(c => this.Table
                .GetAll(handle)[new { index = "handle" }]
                .Nth(0)
                .GetField("id")
                .RunResultAsync<string>(c, null, cancellationToken), cancellationToken);
        }

        public Task<string> GetIdFromEntityAsync(string entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.Db.Run(c => this.Table
                .GetAll(entity)[new { index = "entity" }]
                .Nth(0)
                .GetField("id")
                .RunResultAsync<string>(c, null, cancellationToken), cancellationToken);
        }

        public Task<string> GetIdFromEmailAsync(string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.Db.Run(c => this.Table
                .GetAll(email)[new { index = "email" }]
                .Nth(0)
                .GetField("id")
                .RunResultAsync<string>(c, null, cancellationToken), cancellationToken);
        }

        public Task<User> GetFromHandleAsync(string handle, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.Db.Run(c => this.Table
                .GetAll(handle)[new { index = "handle" }]
                .Nth(0)
                .RunResultAsync<User>(c, null, cancellationToken), cancellationToken);
        }

        public Task<User> GetFromEntityAsync(string entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.Db.Run(c => this.Table
                .GetAll(entity)[new { index = "entity" }]
                .Nth(0)
                .RunResultAsync<User>(c, null, cancellationToken), cancellationToken);
        }

        public Task<User> GetAsync(string userId, string versionId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.Db.Run(c => this.tableVersions.Get(new [] { userId, versionId }).RunResultAsync<User>(c, null, cancellationToken), cancellationToken);
        }

        public override Task UpdateAsync(User user, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Set the update date.
            user.UpdatedAt = DateTime.UtcNow;

            // If needed, set the creation date.
            if (!user.CreatedAt.HasValue)
                user.CreatedAt = user.UpdatedAt;
            
            // Update the version for this user before we save it.
            user.VersionId = this.textHelpers.GenerateUniqueId();

            return this.Db.Run(async c =>
            {
                // Start by saving this specific version.
                var versionInsertResult = await this.tableVersions
                    .Insert(user)
                    .RunResultAsync(c, null, cancellationToken);

                versionInsertResult.AssertInserted(1);

                // Then, conditionally update the last version.
                var upsertResult = await this.Table
                    .Get(user.Id)
                    .Replace(r => this.Db.R.Branch(r.Eq(null)
                        .Or(r.G("updated_at").Lt(user.UpdatedAt)
                            .Or(r.G("updated_at").Eq(user.UpdatedAt).And(r.G("version").Lt(user.VersionId))))
                        , user, r))
                    .RunResultAsync(c, null, cancellationToken);

                upsertResult.AssertNoErrors();
            }, cancellationToken);
        }
    }
}