using System.Threading.Tasks;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Repositories
{
    public interface IBaseRepository<T> where T : DbModelBase
    {
        Task<T> GetAsync(string id);
        Task UpdateAsync(T newT);
    }
}