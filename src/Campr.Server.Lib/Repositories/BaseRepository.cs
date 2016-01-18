using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Connectors.Buckets;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Repositories
{
    abstract class BaseRepository<T> : IBaseRepository<T> where T : DbModelBase
    {
        protected BaseRepository(ITentBuckets buckets, string prefix)
        {
            Ensure.Argument.IsNotNull(buckets, nameof(buckets));
            this.Buckets = buckets;
            this.Prefix = prefix + '_';
        }

        protected ITentBuckets Buckets { get; }
        protected string Prefix { get; }

        public virtual async Task<T> GetAsync(string id)
        {
            var operation = await this.Buckets.Main.GetAsync<T>(this.Prefix + id);
            return operation.Value;
        }

        public virtual async Task UpdateAsync(T newT)
        {
            // If needed, set the creation date.
            if (!newT.CreatedAt.HasValue)
                newT.CreatedAt = DateTime.UtcNow;

            await this.Buckets.Main.UpsertAsync(this.Prefix + newT.GetId(), newT);
        }
    }
}