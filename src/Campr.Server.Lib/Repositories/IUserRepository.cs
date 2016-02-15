using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<string> GetIdFromHandleAsync(string handle, CancellationToken cancellationToken = default(CancellationToken));
        Task<string> GetIdFromEntityAsync(string entity, CancellationToken cancellationToken = default(CancellationToken));
        Task<string> GetIdFromEmailAsync(string email, CancellationToken cancellationToken = default(CancellationToken));
        Task<User> GetFromHandleAsync(string handle, CancellationToken cancellationToken = default(CancellationToken));
        Task<User> GetFromEntityAsync(string entity, CancellationToken cancellationToken = default(CancellationToken));
        Task<User> GetAsync(string userId, string versionId, CancellationToken cancellationToken = default(CancellationToken));
    }
}