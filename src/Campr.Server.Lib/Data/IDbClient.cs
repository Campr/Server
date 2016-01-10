using Couchbase.Core;

namespace Campr.Server.Lib.Data
{
    public interface IDbClient
    {
        IBucket GetBucket();
    }
}