using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;

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
        }

        private readonly ITextHelpers textHelpers;

        public Task<string> GetIdFromHandleAsync(string handle)
        {
            return this.Db.Run(async c =>
            {
                var userIds = await this.Table
                    .GetAll(handle)
                    .optArg("index", "handle")
                    .Limit(1)
                    .GetField("id")
                    .RunResultAsync<List<string>>(c);

                return userIds.FirstOrDefault();
            });
        }

        public Task<string> GetIdFromEntityAsync(string entity)
        {
            return this.Db.Run(async c =>
            {
                var userIds = await this.Table
                    .GetAll(entity)
                    .optArg("index", "entity")
                    .Limit(1)
                    .GetField("id")
                    .RunResultAsync<List<string>>(c);

                return userIds.FirstOrDefault();
            });
        }

        public Task<string> GetIdFromEmailAsync(string email)
        {
            return this.Db.Run(async c =>
            {
                var userIds = await this.Table
                    .GetAll(email)
                    .optArg("index", "email")
                    .Limit(1)
                    .GetField("id")
                    .RunResultAsync<List<string>>(c);

                return userIds.FirstOrDefault();
            });
        }

        public async Task<User> GetFromHandleAsync(string handle)
        {
            // Create the view query to retrieve a user id by handle.
            var query = this.Buckets.Main.CreateQuery("users", "users_handle")
                .Stale(StaleState.False)
                .Key(handle, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.Buckets.Main.QueryAsync<ViewVersionResult>(query);

            // Retrieve and return the first result.
            var documentId = results.Rows.FirstOrDefault()?.Value?.DocId;
            if (string.IsNullOrEmpty(documentId))
                return null;

            var operation = await this.Buckets.Main.GetAsync<User>(documentId);
            return operation.Value;
        }

        public async Task<User> GetFromEntityAsync(string entity)
        {
            // Create the view query to retrieve a user id by entity.
            var query = this.Buckets.Main.CreateQuery("users", "users_entity")
                .Stale(StaleState.False)
                .Key(entity, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.Buckets.Main.QueryAsync<ViewVersionResult>(query);

            // Retrieve and return the first result.
            var documentId = results.Rows.FirstOrDefault()?.Value?.DocId;
            if (string.IsNullOrEmpty(documentId))
                return null;

            var operation = await this.Buckets.Main.GetAsync<User>(documentId);
            return operation.Value;
        }

        public override async Task<User> GetAsync(string userId)
        {
            // Create the viewer query to retrieve the last version of this user.
            var query = this.Buckets.Main.CreateQuery("users", "users_versions")
                .Stale(StaleState.False)
                .Key(userId, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.Buckets.Main.QueryAsync<ViewVersionResult>(query);

            // Retrieve and return the first result.
            var documentId = results.Rows.FirstOrDefault()?.Value?.DocId;
            if (string.IsNullOrEmpty(documentId))
                return null;

            var operation = await this.Buckets.Main.GetAsync<User>(documentId);
            return operation.Value;
        }

        public Task<User> GetAsync(string userId, string versionId)
        {
            return base.GetAsync($"{userId}_{versionId}");
        }

        public override async Task UpdateAsync(User user)
        {
            // Set the update date.
            user.UpdatedAt = DateTime.UtcNow;

            // If needed, set the creation date.
            if (!user.CreatedAt.HasValue)
                user.CreatedAt = user.UpdatedAt;
            
            // Update the version for this user before we save it.
            user.VersionId = this.textHelpers.GenerateUniqueId();

            await this.Buckets.Main.UpsertAsync(this.Prefix + user.GetId(), user);
        }
    }
}