using System;
using Couchbase.Core;

namespace Campr.Server.Lib.Connectors.Buckets
{
    public interface ITentBuckets : IDisposable
    {
        IBucket Main { get; }
    }
}