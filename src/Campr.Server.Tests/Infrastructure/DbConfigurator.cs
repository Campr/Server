using System.Collections.Generic;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Infrastructure;
using RethinkDb.Driver;
using RethinkDb.Driver.Model;

namespace Campr.Server.Tests.Infrastructure
{
    public class DbConfigurator
    {
        public DbConfigurator(
            IExternalConfiguration externalConfiguration, 
            IRethinkConnection db)
        {
            Ensure.Argument.IsNotNull(externalConfiguration, nameof(externalConfiguration));
            Ensure.Argument.IsNotNull(db, nameof(db));

            this.externalConfiguration = externalConfiguration;
            this.db = db;
        }

        private readonly IExternalConfiguration externalConfiguration;
        private readonly IRethinkConnection db;
        private readonly string dbName = "camprtest";

        public async Task Reset()
        {
            var r = new RethinkDB();

            // Create a new connection in order to drop the Db.
            var connection = await r.Connection()
                .Hostname("localhost")
                .ConnectAsync();

            // Retrieve a list of databases and delete the test one if needed.
            var dbList = await r.DbList().RunResultAsync<List<string>>(connection);
            if (dbList.Contains(this.dbName))
            {
                var dbDropResult = await r.DbDrop(this.dbName).RunResultAsync(connection);
                dbDropResult.AssertDatabasesDropped(1);
            }

            // Recreate the database and tables.
            await this.db.InitializeAsync();
        } 
    }
}