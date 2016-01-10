using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    public interface IBewitRepository
    {
        Task<Bewit> GetBewitAsync(string bewitId);
        Task UpdateBewitAsync(Bewit newBewit);
    }
}