using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    public interface IUserRepository
    {
        Task<string> GetUserIdFromHandleAsync(string handle);
        Task<string> GetUserIdFromEntityAsync(string entity);
        Task<string> GetUserIdFromEmail(string email);
        Task<User> GetUserAsync(string userId);
        Task<User> GetUserFromHandleAsync(string handle);
        Task<User> GetUserFromEntityAsync(string entity);
        Task UpdateUserAsync(User user);
    }
}