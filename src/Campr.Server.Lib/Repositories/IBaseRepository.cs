using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Repositories
{
    public interface IBaseRepository<T> where T : DbModelBase
    {
        Task<T> GetAsync(object id, CancellationToken cancellationToken = default(CancellationToken));
        Task UpdateAsync(T newT, CancellationToken cancellationToken = default(CancellationToken));
    }
}