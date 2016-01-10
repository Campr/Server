using System.Threading.Tasks;
using System.Linq;
using Campr.Server.Lib.Data;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    class UserRepository : IUserRepository
    {
        public UserRepository(IDbClient client)
        {
            Ensure.Argument.IsNotNull(client, nameof(client));
            this.client = client;
            this.prefix = "user_";
        }

        private readonly IDbClient client;
        private readonly string prefix;

        public async Task<string> GetUserIdFromHandleAsync(string handle)
        {
            using (var bucket = this.client.GetBucket())
            {
                // Create the view query to retrieve our post last version.
                var query = bucket.CreateQuery("users", "users_handle")
                    .Key(handle, true)
                    .Limit(1);

                // Run this query on our bucket.
                var results = await bucket.QueryAsync<string>(query);
                return results.Rows.FirstOrDefault()?.Value;
            }
        }

        public async Task<string> GetUserIdFromEntityAsync(string entity)
        {
            using (var bucket = this.client.GetBucket())
            {
                // Create the view query to retrieve our post last version.
                var query = bucket.CreateQuery("users", "users_entity")
                    .Key(entity, true)
                    .Limit(1);

                // Run this query on our bucket.
                var results = await bucket.QueryAsync<string>(query);
                return results.Rows.FirstOrDefault()?.Value;
            }
        }

        public async Task<string> GetUserIdFromEmail(string email)
        {
            using (var bucket = this.client.GetBucket())
            {
                // Create the view query to retrieve our post last version.
                var query = bucket.CreateQuery("users", "users_email")
                    .Key(email, true)
                    .Limit(1);

                // Run this query on our bucket.
                var results = await bucket.QueryAsync<string>(query);
                return results.Rows.FirstOrDefault()?.Value;
            }
        }

        public Task<User> GetUserAsync(string userId)
        {
            using (var bucket = this.client.GetBucket())
            {
                return Task.FromResult(bucket.Get<User>(this.prefix + userId).Value);
            }
        }

        public async Task<User> GetUserFromHandleAsync(string handle)
        {
            using (var bucket = this.client.GetBucket())
            {
                // Create the view query to retrieve our post last version.
                var query = bucket.CreateQuery("users", "users_handle")
                    .Key(handle, true)
                    .Limit(1);

                // Run this query on our bucket.
                var results = await bucket.QueryAsync<string>(query);

                // Retrieve and return the first result.
                var documentId = results.Rows.FirstOrDefault()?.Id;
                if (string.IsNullOrEmpty(documentId))
                {
                    return null;
                }

                var operation = bucket.Get<User>(documentId);
                return operation.Value;
            }
        }

        public async Task<User> GetUserFromEntityAsync(string entity)
        {
            using (var bucket = this.client.GetBucket())
            {
                // Create the view query to retrieve our post last version.
                var query = bucket.CreateQuery("users", "users_entity")
                    .Key(entity, true)
                    .Limit(1);

                // Run this query on our bucket.
                var results = await bucket.QueryAsync<string>(query);

                // Retrieve and return the first result.
                var documentId = results.Rows.FirstOrDefault()?.Id;
                if (string.IsNullOrEmpty(documentId))
                {
                    return null;
                }

                var operation = bucket.Get<User>(documentId);
                return operation.Value;
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            using (var bucket = this.client.GetBucket())
            {
                bucket.Upsert(this.prefix + user.Id, user);
            }
        }
    }
}