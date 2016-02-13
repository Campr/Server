using System;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;

namespace Campr.Server.Lib.Connectors.RethinkDb
{
    public interface IRethinkConnection : IDisposable
    {
        Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<T> Run<T>(Func<IConnection, Task<T>> worker);
        Task Run(Func<IConnection, Task> worker);
        Table Users { get; }
        Table Posts { get; }
    }
}