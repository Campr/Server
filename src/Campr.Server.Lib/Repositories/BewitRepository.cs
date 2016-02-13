using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    class BewitRepository : BaseRepository<Bewit>, IBewitRepository
    {
        public BewitRepository(IRethinkConnection db) : base(db, db.Bewits)
        {
        }
    }
}