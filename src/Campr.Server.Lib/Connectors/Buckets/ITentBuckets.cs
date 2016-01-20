using System;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Core;

namespace Campr.Server.Lib.Connectors.Buckets
{
    public interface ITentBuckets : IDisposable
    {
        Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));
        IBucket Main { get; }
    }
}