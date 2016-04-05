using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Repositories
{
    public interface IUserPostRepository
    {
        Task<UserPost> GetAsync(string ownerId, string userId, string postId, CancellationToken cancellationToken = default(CancellationToken));
        Task<UserPost> GetAsync(string ownerId, string userId, string postId, string versionId, CancellationToken cancellationToken = default(CancellationToken));
        Task<IList<UserPost>> GetAsync(string requesterId, string feedOwnerId, ITentFeedRequest feedRequest, CancellationToken cancellationToken = new CancellationToken());
        Task<long> CountAsync(string requesterId, string feedOwnerId, ITentFeedRequest feedRequest, CancellationToken cancellationToken = new CancellationToken());
        Task UpdateAsync(string ownerId, TentPost post, bool isFromFollowing, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteAsync(string ownerId, TentPost post, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteAsync(string ownerId, TentPost post, bool specificVersion, CancellationToken cancellationToken = default(CancellationToken));
    }
}