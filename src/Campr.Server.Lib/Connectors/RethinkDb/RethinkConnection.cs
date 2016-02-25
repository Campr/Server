using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Json;
using Campr.Server.Lib.Services;
using Newtonsoft.Json;
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
            this.R = new RethinkDB();
            this.initializer = new TaskRunner(this.InitializeOnceAsync);

            // Configure the static RethinkDb serializer.
            Converter.Serializer.ContractResolver = dbContractResolver;
            Converter.Serializer.DefaultValueHandling = DefaultValueHandling.Include;
            Converter.Serializer.MissingMemberHandling = MissingMemberHandling.Ignore;
            Converter.Serializer.NullValueHandling = NullValueHandling.Ignore;
            Converter.Serializer.TypeNameHandling = TypeNameHandling.None;
        }

        private readonly ILoggingService loggingService;
        private readonly TaskRunner initializer;

        private readonly string dbName;
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
                this.connection = await this.R.Connection()
                    .Hostname("localhost")
                    .ConnectAsync();

                // Retrieve a list of databases and create ours if it doesn't exist.
                var dbList = await this.R.DbList().RunResultAsync<List<string>>(this.connection, null, cancellationToken);
                if (!dbList.Contains(this.dbName))
                {
                    var dbCreateResult = await this.R.DbCreate(this.dbName).RunResultAsync(this.connection, null, cancellationToken);
                    dbCreateResult.AssertDatabasesCreated(1);
                }

                var db = this.R.Db(this.dbName);

                // Retrieve a list of tables for this database.
                var tableList = await db.TableList().RunResultAsync<List<string>>(this.connection, null, cancellationToken);
                
                // Create the missing tables, if any.
                if (!tableList.Contains("users"))
                {
                    var tableCreateResult = await db.TableCreate("users").RunResultAsync(this.connection, null, cancellationToken);
                    tableCreateResult.AssertTablesCreated(1);
                }

                var usersIndexList = await this.Users.IndexList().RunResultAsync<List<string>>(this.connection, null, cancellationToken);
                if (!usersIndexList.Contains("handle"))
                {
                    var indexCreateResult = await this.Users.IndexCreate("handle").RunResultAsync(this.connection, null, cancellationToken);
                    indexCreateResult.AssertNoErrors();
                }
                
                if (!usersIndexList.Contains("entity"))
                {
                    var indexCreateResult = await this.Users.IndexCreate("entity").RunResultAsync(this.connection, null, cancellationToken);
                    indexCreateResult.AssertNoErrors();
                }

                if (!usersIndexList.Contains("email"))
                {
                    var indexCreateResult = await this.Users.IndexCreate("email").RunResultAsync(this.connection, null, cancellationToken);
                    indexCreateResult.AssertNoErrors();
                }

                if (!tableList.Contains("userversions"))
                {
                    var tableCreateResult = await db.TableCreate("userversions")
                        .optArg("primary_key", "key_user_version")
                        .RunResultAsync(this.connection, null, cancellationToken);

                    tableCreateResult.AssertNoErrors();
                }

                if (!tableList.Contains("posts"))
                {
                    var tableCreatedResult = await db.TableCreate("posts")
                        .optArg("primary_key", "key_user_post")
                        .RunResultAsync(this.connection, null, cancellationToken);

                    tableCreatedResult.AssertTablesCreated(1);
                }

                var postsIndexList = await this.Posts.IndexList().RunResultAsync<IList<string>>(this.connection, null, cancellationToken);
                if (!postsIndexList.Contains("user_stype_updatedat"))
                {
                    var indexCreateResult = await this.Posts.IndexCreate("user_stype_versionreceivedat", r =>
                            this.R.Branch(r.HasFields("deleted_at"),
                                null,
                                new object[] { r.G("user"), r.G("type").Split("#", 1).Nth(0), r.G("version").G("received_at") }))
                        .RunResultAsync(this.connection, null, cancellationToken);

                    indexCreateResult.AssertNoErrors();
                }

                if (!postsIndexList.Contains("user_ftype_updatedat"))
                {
                    var indexCreateResult = await this.Posts.IndexCreate("user_ftype_versionreceivedat", r =>
                            this.R.Branch(r.HasFields("deleted_at"),
                                null,
                                new object[] { r.G("user"), r.G("type"), r.G("version").G("received_at") }))
                        .RunResultAsync(this.connection, null, cancellationToken);

                    indexCreateResult.AssertNoErrors();
                }

                if (!tableList.Contains("postversions"))
                {
                    var tableCreatedResult = await db.TableCreate("postversions")
                        .optArg("primary_key", "key_user_post_version")
                        .RunResultAsync(this.connection, null, cancellationToken);

                    tableCreatedResult.AssertTablesCreated(1);
                }

                var postVersionsIndexList = await this.PostVersions.IndexList().RunResultAsync<IList<string>>(this.connection, null, cancellationToken);
                if (!postVersionsIndexList.Contains("user_post_versionreceivedat"))
                {
                    var indexCreateResult = await this.PostVersions.IndexCreate("user_post_versionreceivedat", r =>
                            this.R.Branch(r.HasFields("deleted_at"), 
                                null,
                                new object[] { r.G("user"), r.G("id"), r.G("version").G("received_at") }))
                        .RunResultAsync(this.connection, null, cancellationToken);

                    indexCreateResult.AssertNoErrors();
                }

                if (!tableList.Contains("userposts"))
                {
                    var tableCreatedResult = await db.TableCreate("userposts")
                        .optArg("primary_key", "key_owner_user_post")
                        .RunResultAsync(this.connection, null, cancellationToken);

                    tableCreatedResult.AssertTablesCreated(1);
                }

                var userPostsIndexList = await this.UserPosts.IndexList().RunResultAsync<IList<string>>(this.connection, null, cancellationToken);
                if (!userPostsIndexList.Contains("owner_versionreceivedat"))
                {
                    var indexCreateResult = await this.UserPosts.IndexCreate("owner_versionreceivedat", r =>
                            this.R.Branch(r.HasFields("deleted_at"),
                                null,
                                new object[] { r.G("owner"), r.G("version_received_at") }))
                        .RunResultAsync(this.connection, null, cancellationToken);

                    indexCreateResult.AssertNoErrors();
                }

                if (!userPostsIndexList.Contains("owner_versionpublishedat"))
                {
                    var indexCreateResult = await this.UserPosts.IndexCreate("owner_versionpublishedat", r =>
                            this.R.Branch(r.HasFields("deleted_at"),
                                null,
                                new object[] { r.G("owner"), r.G("version_published_at") }))
                        .RunResultAsync(this.connection, null, cancellationToken);

                    indexCreateResult.AssertNoErrors();
                }

                if (!userPostsIndexList.Contains("owner_receivedat"))
                {
                    var indexCreateResult = await this.UserPosts.IndexCreate("owner_receivedat", r =>
                            this.R.Branch(r.HasFields("deleted_at"),
                                null,
                                new object[] { r.G("owner"), r.G("received_at") }))
                        .RunResultAsync(this.connection, null, cancellationToken);

                    indexCreateResult.AssertNoErrors();
                }

                if (!userPostsIndexList.Contains("owner_publishedat"))
                {
                    var indexCreateResult = await this.UserPosts.IndexCreate("owner_publishedat", r =>
                            this.R.Branch(r.HasFields("deleted_at"),
                                null,
                                new object[] { r.G("owner"), r.G("published_at") }))
                        .RunResultAsync(this.connection, null, cancellationToken);

                    indexCreateResult.AssertNoErrors();
                }
                
                if (!tableList.Contains("userpostversions"))
                {
                    var tableCreatedResult = await db.TableCreate("userpostversions")
                        .optArg("primary_key", "key_owner_user_post_version")
                        .RunResultAsync(this.connection, null, cancellationToken);

                    tableCreatedResult.AssertTablesCreated(1);
                }

                var userPostVersionsIndexList = await this.UserPostVersions.IndexList().RunResultAsync<IList<string>>(this.connection, null, cancellationToken);
                if (!userPostVersionsIndexList.Contains("owner_user_post_versionreceivedat"))
                {
                    var indexCreatedResult = await this.UserPostVersions.IndexCreate("owner_user_post_versionreceivedat", r =>
                            this.R.Branch(r.HasFields("deleted_at"),
                                null,
                                new object[] { r.G("owner"), r.G("user"), r.G("post"), r.G("version_received_at") }))
                        .RunResultAsync(this.connection, null, cancellationToken);

                    indexCreatedResult.AssertNoErrors();
                }
            }
            catch (Exception ex)
            {
                this.loggingService.Exception(ex, "Error during RethinkDb initialization.");
                throw;
            }
        }

        public Task<T> Run<T>(Func<IConnection, Task<T>> worker, CancellationToken cancellationToken = default(CancellationToken))
        {
            return worker(this.connection);
        }

        public Task Run(Func<IConnection, Task> worker, CancellationToken cancellationToken = default(CancellationToken))
        {
            return worker(this.connection);
        }

        public RethinkDB R { get; }
        public Table Users => this.R.Db(this.dbName).Table("users");
        public Table UserVersions => this.R.Db(this.dbName).Table("userversions");
        public Table Posts => this.R.Db(this.dbName).Table("posts");
        public Table PostVersions => this.R.Db(this.dbName).Table("postversions");
        public Table UserPosts => this.R.Db(this.dbName).Table("userposts");
        public Table UserPostVersions => this.R.Db(this.dbName).Table("userpostversions");
        public Table Attachments => this.R.Db(this.dbName).Table("attachments");
        public Table Bewits => this.R.Db(this.dbName).Table("bewits");

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