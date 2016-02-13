using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Json;
using Campr.Server.Lib.Services;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;

namespace Campr.Server.Lib.Connectors.RethinkDb
{
    class RethinkConnection : IRethinkConnection
    {
        public RethinkConnection(
            ILoggingService loggingService, 
            IDbContractResolver dbContractResolver,
            IGeneralConfiguration configuration)
        {
            Ensure.Argument.IsNotNull(loggingService, nameof(loggingService));
            Ensure.Argument.IsNotNull(dbContractResolver, nameof(dbContractResolver));
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));

            this.loggingService = loggingService;

            this.dbName = this.GetDbName(configuration.Environment);
            this.r = new RethinkDB();
            this.initializer = new TaskRunner(this.InitializeOnceAsync);

            // Configure the static RethinkDb serializer.
            Converter.Serializer.ContractResolver = dbContractResolver;
        }

        private readonly ILoggingService loggingService;
        private readonly TaskRunner initializer;

        private readonly string dbName;
        private readonly RethinkDB r;
        private Connection connection;

        public Task InitializeAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return this.initializer.RunOnce(cancellationToken);
        }

        private async Task InitializeOnceAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                // Create the connection.
                this.connection = await this.r.Connection()
                    .Hostname("localhost")
                    .ConnectAsync();

                // Retrieve a list of databases and create ours if it doesn't exist.
                var dbList = await this.r.DbList().RunResultAsync<List<string>>(this.connection);
                if (!dbList.Contains(this.dbName))
                {
                    var dbCreateResult = await this.r.DbCreate(this.dbName).RunResultAsync(this.connection);
                    dbCreateResult.AssertDatabasesCreated(1);
                }

                var db = this.r.Db(this.dbName);

                // Retrieve a list of tables for this database.
                var tableList = await db.TableList().RunResultAsync<List<string>>(this.connection);
                
                // Create the missing tables, if any.
                if (!tableList.Contains("users"))
                {
                    var tableCreateResult = await db.TableCreate("users").RunResultAsync(this.connection);
                    tableCreateResult.AssertTablesCreated(1);
                }

                var usersIndexList = await this.Users.IndexList().RunResultAsync<List<string>>(this.connection);
                if (!usersIndexList.Contains("handle"))
                {
                    var indexCreateResult = await this.Users.IndexCreate("handle").RunResultAsync(this.connection);
                    indexCreateResult.AssertNoErrors();
                }
                
                if (!usersIndexList.Contains("entity"))
                {
                    var indexCreateResult = await this.Users.IndexCreate("entity").RunResultAsync(this.connection);
                    indexCreateResult.AssertNoErrors();
                }

                if (!usersIndexList.Contains("email"))
                {
                    var indexCreateResult = await this.Users.IndexCreate("email").RunResultAsync(this.connection);
                    indexCreateResult.AssertNoErrors();
                }

                if (!tableList.Contains("posts"))
                {
                    var tableCreatedResult = await db.TableCreate("posts").RunResultAsync(this.connection);
                    tableCreatedResult.AssertTablesCreated(1);
                }
            }
            catch (Exception ex)
            {
                this.loggingService.Exception(ex, "Error during RethinkDb initialization.");
                throw;
            }
        }

        public Task<T> Run<T>(Func<IConnection, Task<T>> worker)
        {
            return worker(this.connection);
        }

        public Task Run(Func<IConnection, Task> worker)
        {
            return worker(this.connection);
        }

        public Table Users => this.r.Db(this.dbName).Table("users");
        public Table Posts => this.r.Db(this.dbName).Table("posts");
        public Table Attachments => this.r.Db(this.dbName).Table("attachments");
        public Table Bewits => this.r.Db(this.dbName).Table("bewits");

        public void Dispose()
        {
            // Clear the persistent connection.
            this.connection.Dispose();
        }

        private string GetDbName(EnvironmentEnum environment)
        {
            switch (environment)
            {
                case EnvironmentEnum.Production:
                    return "camprprod";
                case EnvironmentEnum.Test:
                    return "camprtest";
                default:
                    return "camprdev";
            }
        }
    }
}