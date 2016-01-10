using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Services
{
    /// <summary>
    /// Performs Tent Discovery.
    /// </summary>
    public interface IDiscoveryService
    {
        /// <summary>
        /// Perform Tent Discovery on a Uri and retrieve the corresponding Meta post.
        /// </summary>
        /// <param name="targetUri">The <see cref="Uri"/> on which to perform Tent Discovery.</param>
        /// <returns>The discovered Meta post.</returns>
        Task<TentPost<T>> DiscoverUriAsync<T>(Uri targetUri) where T : class;
    }
}
