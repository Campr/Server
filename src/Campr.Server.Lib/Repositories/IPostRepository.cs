using System.Collections.Generic;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Repositories
{
    public interface IPostRepository
    {
        Task<TentPost<T>> GetPostLastVersionAsync<T>(string userId, string postId) where T : class;
        Task<TentPost<T>> GetPostLastVersionByTypeAsync<T>(string userId, ITentPostType type) where T : class;
        Task<TentPost<T>> GetPostAsync<T>(string userId, string postId, string versionId) where T : class;
        Task<IList<TentPost<T>>> GetPostsAsync<T>(string userId, string postId) where T : class;
        Task<IList<TentPost<T>>> GetBulkPostsAsync<T>(IList<TentPostReference> references) where T : class;
        Task UpdatePostAsync<T>(TentPost<T> post) where T : class;
        Task DeletePostAsync<T>(TentPost<T> post, bool specificVersion = false) where T : class;
        Task DeletePostAsync(string userId, string postId, string versionId = null);
    }
}