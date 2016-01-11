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
            this.prefix = prefix + '_';
        }

        protected ITentBuckets Buckets { get; }
        private readonly string prefix;

        public async Task<T> GetAsync(string id)
        {
            var operation = await this.Buckets.Main.GetAsync<T>(this.prefix + id);
            return operation.Value;
        }

        public async Task UpdateAsync(T newT)
        {
            await this.Buckets.Main.UpsertAsync(this.prefix + newT.GetId(), newT);
        }
    }
}