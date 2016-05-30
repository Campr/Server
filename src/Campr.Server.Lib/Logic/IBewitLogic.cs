using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Logic
{
    public interface IBewitLogic
    {
        Task<string> CreateBewitForPostAsync(User user, TentPost post, CancellationToken cancellationToken = default(CancellationToken));
        Task<string> CreateBewitForPostAsync(User user, TentPost post, TimeSpan expiresIn, CancellationToken cancellationToken = default(CancellationToken));
    }
}