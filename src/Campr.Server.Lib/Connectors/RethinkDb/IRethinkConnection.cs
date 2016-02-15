using System;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;

namespace Campr.Server.Lib.Connectors.RethinkDb
{
    public interface IRethinkConnection : IDisposable
    {
        Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<T> Run<T>(Func<IConnection, Task<T>> worker, CancellationToken cancellationToken = default(CancellationToken));
        Task Run(Func<IConnection, Task> worker, CancellationToken cancellationToken = default(CancellationToken));
        RethinkDB R { get; }
        Table Users { get; }
        Table UserVersions { get; }
        Table Posts { get; }
        Table PostVersions { get; }
        Table Attachments { get; }
        Table Bewits { get; }
    }
}