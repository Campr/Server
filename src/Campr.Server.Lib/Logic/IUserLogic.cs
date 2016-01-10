using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Logic
{
    public interface IUserLogic
    {
        Task<string> GetUserIdAsync(string entityOrHandle);
        Task<User> GetUserAsync(string entityOrHandle);
        Task<User> CreateUserAsync(string name, string email, string password, string handle);
        //DbSession CreateSession(DbUser user);
    }
}