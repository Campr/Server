using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    public interface IUserPostRepository
    {
        Task<UserPost> GetAsync(string ownerId, string userId, string postId, CancellationToken cancellationToken = default(CancellationToken));
        Task<UserPost> GetAsync(string ownerId, string userId, string postId, string versionId, CancellationToken cancellationToken = default(CancellationToken));
    }
}