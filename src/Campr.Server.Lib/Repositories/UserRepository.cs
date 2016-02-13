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

        public Task<User> GetFromHandleAsync(string handle)
        {
            return this.Db.Run(async c =>
            {
                var users = await this.Table
                    .GetAll(handle)
                    .optArg("index", "handle")
                    .Limit(1)
                    .RunResultAsync<List<User>>(c);

                return users.FirstOrDefault();
            });
        }

        public Task<User> GetFromEntityAsync(string entity)
        {
            return this.Db.Run(async c =>
            {
                var users = await this.Table
                    .GetAll(entity)
                    .optArg("index", "entity")
                    .Limit(1)
                    .RunResultAsync<List<User>>(c);

                return users.FirstOrDefault();
            });
        }

        public Task<User> GetAsync(string userId, string versionId)
        {
            throw new NotImplementedException();
        }

        public override Task UpdateAsync(User user)
        {
            // Set the update date.
            user.UpdatedAt = DateTime.UtcNow;

            // If needed, set the creation date.
            if (!user.CreatedAt.HasValue)
                user.CreatedAt = user.UpdatedAt;
            
            // Update the version for this user before we save it.
            user.VersionId = this.textHelpers.GenerateUniqueId();

            return base.UpdateAsync(user);
        }
    }
}