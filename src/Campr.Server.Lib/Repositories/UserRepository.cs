using System.Threading.Tasks;
using System.Linq;
using Campr.Server.Lib.Connectors.Buckets;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(ITentBuckets buckets) : base(buckets, "user")
        {
        }

        public async Task<string> GetIdFromHandleAsync(string handle)
        {
            // Create the view query to retrieve our post last version.
            var query = this.Buckets.Main.CreateQuery("users", "users_handle")
                .Key(handle, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.Buckets.Main.QueryAsync<string>(query);
            return results.Rows.FirstOrDefault()?.Value;
        }

        public async Task<string> GetIdFromEntityAsync(string entity)
        {
            // Create the view query to retrieve our post last version.
            var query = this.Buckets.Main.CreateQuery("users", "users_entity")
                .Key(entity, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.Buckets.Main.QueryAsync<string>(query);
            return results.Rows.FirstOrDefault()?.Value;
        }

        public async Task<string> GetIdFromEmailAsync(string email)
        {
            // Create the view query to retrieve our post last version.
            var query = this.Buckets.Main.CreateQuery("users", "users_email")
                .Key(email, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.Buckets.Main.QueryAsync<string>(query);
            return results.Rows.FirstOrDefault()?.Value;
        }

        public async Task<User> GetFromHandleAsync(string handle)
        {
            // Create the view query to retrieve our post last version.
            var query = this.Buckets.Main.CreateQuery("users", "users_handle")
                .Key(handle, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.Buckets.Main.QueryAsync<string>(query);

            // Retrieve and return the first result.
            var documentId = results.Rows.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(documentId))
                return null;

            var operation = await this.Buckets.Main.GetAsync<User>(documentId);
            return operation.Value;
        }

        public async Task<User> GetFromEntityAsync(string entity)
        {
            // Create the view query to retrieve our post last version.
            var query = this.Buckets.Main.CreateQuery("users", "users_entity")
                .Key(entity, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.Buckets.Main.QueryAsync<string>(query);

            // Retrieve and return the first result.
            var documentId = results.Rows.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(documentId))
                return null;

            var operation = await this.Buckets.Main.GetAsync<User>(documentId);
            return operation.Value;
        }
    }
}