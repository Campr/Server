using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Tent;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace Campr.Server.Lib.Repositories
{
    abstract class BaseRepository<T> : IBaseRepository<T> where T : DbModelBase
    {
        protected BaseRepository(IRethinkConnection db, Table table)
        {
            Ensure.Argument.IsNotNull(db, nameof(db));
            Ensure.Argument.IsNotNull(table, nameof(table));

            this.Db = db;
            this.Table = table;
        }

        protected IRethinkConnection Db { get; }
        protected Table Table { get; }

        public virtual Task<T> GetAsync(object id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.Db.Run(c => this.Table.Get(id).RunResultAsync<T>(c, null, cancellationToken), cancellationToken);
        }

        public virtual Task UpdateAsync(T newT, CancellationToken cancellationToken = default(CancellationToken))
        {
            // If needed, set the creation date.
            if (!newT.CreatedAt.HasValue)
                newT.CreatedAt = DateTime.UtcNow;

            return this.Db.Run(async c =>
            {
                var result = await this.Table
                    .Insert(newT)
                    .OptArg("conflict", "replace")
                    .RunResultAsync(c, null, cancellationToken);

                result.AssertNoErrors();
            }, cancellationToken);
        }
    }
}