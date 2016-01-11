using System.Threading.Tasks;
using System.Linq;
using Campr.Server.Lib.Connectors.Buckets;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    class UserRepository : IUserRepository
    {
        public UserRepository(ITentBuckets buckets)
        {
            Ensure.Argument.IsNotNull(buckets, nameof(buckets));
            this.buckets = buckets;
            this.prefix = "user_";
        }

        private readonly ITentBuckets buckets;
        private readonly string prefix;

        public async Task<string> GetUserIdFromHandleAsync(string handle)
        {
            // Create the view query to retrieve our post last version.
            var query = this.buckets.Main.CreateQuery("users", "users_handle")
                .Key(handle, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.buckets.Main.QueryAsync<string>(query);
            return results.Rows.FirstOrDefault()?.Value;
        }

        public async Task<string> GetUserIdFromEntityAsync(string entity)
        {
            // Create the view query to retrieve our post last version.
            var query = this.buckets.Main.CreateQuery("users", "users_entity")
                .Key(entity, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.buckets.Main.QueryAsync<string>(query);
            return results.Rows.FirstOrDefault()?.Value;
        }

        public async Task<string> GetUserIdFromEmail(string email)
        {
            // Create the view query to retrieve our post last version.
            var query = this.buckets.Main.CreateQuery("users", "users_email")
                .Key(email, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.buckets.Main.QueryAsync<string>(query);
            return results.Rows.FirstOrDefault()?.Value;
        }

        public async Task<User> GetUserAsync(string userId)
        {
            var operation = await this.buckets.Main.GetAsync<User>(this.prefix + userId);
            return operation.Value;
        }

        public async Task<User> GetUserFromHandleAsync(string handle)
        {
            // Create the view query to retrieve our post last version.
            var query = this.buckets.Main.CreateQuery("users", "users_handle")
                .Key(handle, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.buckets.Main.QueryAsync<string>(query);

            // Retrieve and return the first result.
            var documentId = results.Rows.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(documentId))
                return null;

            var operation = await this.buckets.Main.GetAsync<User>(documentId);
            return operation.Value;
        }

        public async Task<User> GetUserFromEntityAsync(string entity)
        {
            // Create the view query to retrieve our post last version.
            var query = this.buckets.Main.CreateQuery("users", "users_entity")
                .Key(entity, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.buckets.Main.QueryAsync<string>(query);

            // Retrieve and return the first result.
            var documentId = results.Rows.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(documentId))
                return null;

            var operation = await this.buckets.Main.GetAsync<User>(documentId);
            return operation.Value;
        }

        public async Task UpdateUserAsync(User user)
        {
            await this.buckets.Main.UpsertAsync(this.prefix + user.Id, user);
        }
    }
}