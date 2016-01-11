using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<string> GetIdFromHandleAsync(string handle);
        Task<string> GetIdFromEntityAsync(string entity);
        Task<string> GetIdFromEmailAsync(string email);
        Task<User> GetFromHandleAsync(string handle);
        Task<User> GetFromEntityAsync(string entity);
    }
}