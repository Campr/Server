using System.Threading.Tasks;
using Campr.Server.Lib.Data;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    class BewitRepository : IBewitRepository
    {
        public BewitRepository(IDbClient client)
        {
            Ensure.Argument.IsNotNull(client, nameof(client));
            this.client = client;
            this.prefix = "bewit_";
        }

        private readonly IDbClient client;
        private readonly string prefix;
        
        public async Task<Bewit> GetBewitAsync(string bewitId)
        {
            using (var bucket = this.client.GetBucket())
            {
                return bucket.Get<Bewit>(this.prefix + bewitId).Value;
            }
        }

        public async Task UpdateBewitAsync(Bewit newBewit)
        {
            using (var bucket = this.client.GetBucket())
            {
                bucket.Upsert(this.prefix + newBewit.Id, newBewit);
            }
        }
    }
}