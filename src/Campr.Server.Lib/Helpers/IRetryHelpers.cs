using System;
using System.Threading;
using System.Threading.Tasks;

namespace Campr.Server.Lib.Helpers
{
    public interface IRetryHelpers
    {
        Task RetryAsync(Func<Task> worker, CancellationToken cancellationToken = default(CancellationToken));
        Task<T> RetryAsync<T>(Func<Task<T>> worker, CancellationToken cancellationToken = default(CancellationToken));
    }
}