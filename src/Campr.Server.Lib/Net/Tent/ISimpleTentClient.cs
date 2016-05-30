using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Net.Tent
{
    public interface ISimpleTentClient
    {
        Task<TentPost<T>> GetAsync<T>(Uri postUri, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
    }
}