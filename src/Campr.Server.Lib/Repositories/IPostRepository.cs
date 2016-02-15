using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Repositories
{
    public interface IPostRepository
    {
        Task<TentPost<T>> GetLastVersionAsync<T>(string userId, string postId, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task<TentPost<T>> GetLastVersionByTypeAsync<T>(string userId, ITentPostType type, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task<TentPost<T>> GetAsync<T>(string userId, string postId, string versionId, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task<IList<TentPost<T>>> GetBulkAsync<T>(IList<TentPostReference> references, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task UpdateAsync<T>(TentPost<T> post, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task DeleteAsync<T>(TentPost<T> post, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task DeleteAsync<T>(TentPost<T> post, bool specificVersion, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task DeleteAsync(string userId, string postId, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteAsync(string userId, string postId, string versionId, CancellationToken cancellationToken = default(CancellationToken));
    }
}