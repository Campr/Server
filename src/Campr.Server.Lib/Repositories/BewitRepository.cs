using Campr.Server.Lib.Connectors.Buckets;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    class BewitRepository : BaseRepository<Bewit>, IBewitRepository
    {
        public BewitRepository(ITentBuckets buckets) : base(buckets, "bewit")
        {
        }
    }
}