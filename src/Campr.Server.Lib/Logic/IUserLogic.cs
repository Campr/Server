using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Logic
{
    public interface IUserLogic
    {
        Task<string> GetUserIdAsync(string entityOrHandle, CancellationToken cancellationToken = default(CancellationToken));
        Task<User> GetUserAsync(string entityOrHandle, CancellationToken cancellationToken = default(CancellationToken));
        Task<User> CreateUserAsync(string name, string email, string password, string handle);
        //DbSession CreateSession(DbUser user);
    }
}