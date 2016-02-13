using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Campr.Server.Lib.Connectors.Buckets;
using Campr.Server.Lib.Connectors.RethinkDb;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Repositories
{
    class PostRepository : IPostRepository
    { 
        public PostRepository(IRethinkConnection buckets)
        {
            Ensure.Argument.IsNotNull(buckets, nameof(buckets));
            this.buckets = buckets;
            this.prefix = "post_";
        }

        private readonly IRethinkConnection buckets;
        private readonly string prefix;
        
        public async Task<TentPost<T>> GetLastVersionAsync<T>(string userId, string postId) where T : class
        {
            // Create the view query to retrieve our post last version.
            var query = this.buckets.Main.CreateQuery("posts", "posts_lastversion")
                .Key(new [] { userId, postId }, true)
                .Group(true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.buckets.Main.QueryAsync<ViewVersionResult>(query);

            // Retrieve and return the first result.
            var documentId = results.Rows.FirstOrDefault()?.Value?.DocId;
            if (string.IsNullOrEmpty(documentId))
                return null;

            var operation = await this.buckets.Main.GetAsync<TentPost<T>>(documentId);
            return operation.Value;
        }

        public async Task<TentPost<T>> GetLastVersionByTypeAsync<T>(string userId, ITentPostType type) where T : class
        {
            // Create the view query to retrieve our post last version.
            var query = this.buckets.Main.CreateQuery("posts", "posts_type_lastversion").Group(true);
            if (type.WildCard)
                query = query
                    .StartKey(new [] { userId, type.Type + '#' }, true)
                    .EndKey(new [] { userId, type.Type + '$' }, true)
                    .InclusiveEnd(false);
            else
                query = query.Key(new [] { userId, type.ToString() }, true);

            // Run this query on our bucket.
            var results = await this.buckets.Main.QueryAsync<ViewVersionResult>(query);

            // Find the most recent result.
            var documentId = results.Rows
                .OrderByDescending(r => r.Value.Date)
                .FirstOrDefault()?
                .Value?
                .DocId;

            // Retrieve and return that document.
            if (string.IsNullOrEmpty(documentId))
            {
                return null;  
            }

            var operation = await this.buckets.Main.GetAsync<TentPost<T>>(documentId);
            return operation.Value;
        }

        public async Task<TentPost<T>> GetAsync<T>(string userId, string postId, string versionId) where T : class
        {
            // Create the view query to retrieve our post version.
            var query = this.buckets.Main.CreateQuery("posts", "posts_versions")
                .Key(new [] { userId, postId, versionId }, true)
                .Limit(1);

            // Run this query on our bucket.
            var results = await this.buckets.Main.QueryAsync<object>(query);

            // Retrieve and return the first result.
            var documentId = results.Rows.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(documentId))
            {
                return null;
            }

            var operation = await this.buckets.Main.GetAsync<TentPost<T>>(documentId);
            return operation.Value;
        }

        public async Task<IList<TentPost<T>>> GetAllAsync<T>(string userId, string postId) where T : class
        {
            // Create the view query to retrieve our post version.
            var query = this.buckets.Main.CreateQuery("posts", "posts_versions")
                .StartKey(new [] { userId, postId, "0" }, true)
                .EndKey(new [] { userId, postId, "z" }, true);

            // Run this query on our bucket.
            var results = await this.buckets.Main.QueryAsync<TentPost<T>>(query);

            // Retrieve and return the corresponding documents.
            var documentIds = results.Rows.Select(r => r.Id).ToList();
            var operationTasks = documentIds.Select(did => this.buckets.Main.GetAsync<TentPost<T>>(did)).ToList();
            await Task.WhenAll(operationTasks);

            return operationTasks
                .Where(t => t.Result != null && t.Result.Success)
                .Select(t => t.Result.Value)
                .ToList();
        }

        public async Task<IList<TentPost<T>>> GetBulkAsync<T>(IList<TentPostReference> references) where T : class
        {
            // Directly retrieve the documents for the provided references.
            var documentIds = references.Select(this.GetPostId).ToList();
            var operationTasks = documentIds.Select(did => this.buckets.Main.GetAsync<TentPost<T>>(did)).ToList();
            await Task.WhenAll(operationTasks);

            return operationTasks
                .Where(t => t.Result != null && t.Result.Success)
                .Select(t => t.Result.Value)
                .ToList();
        }

        public async Task UpdateAsync<T>(TentPost<T> post) where T : class 
        {
            await this.buckets.Main.UpsertAsync(this.GetPostId(post), post);
        }

        public Task DeleteAsync<T>(TentPost<T> post, bool specificVersion = false) where T : class
        {
            return this.DeleteAsync(post.UserId, post.Id, specificVersion ? post.Version.Id : null);
        }

        public async Task DeleteAsync(string userId, string postId, string versionId = null)
        {
            // Retrieve all the documents to update.
            var postsToDelete = new List<TentPost<object>>();
            if (string.IsNullOrWhiteSpace(versionId))
                postsToDelete.Add(await this.GetAsync<object>(userId, postId, versionId));
            else
                postsToDelete.AddRange(await this.GetAllAsync<object>(userId, postId));

            // Set the deleted date on those documents.
            var deletedAt = DateTime.UtcNow;
            postsToDelete.ForEach(p => p.DeletedAt = deletedAt);

            // Update these documents in the Db.
            var operationTasks = postsToDelete.Select(p => this.buckets.Main.UpsertAsync(this.GetPostId(p), p)).ToList();
            await Task.WhenAll(operationTasks);
        }

        private string GetPostId(TentPost post)
        {
            return $"{this.prefix}{post.UserId}_{post.Id}_{post.Version.Id}";
        }

        private string GetPostId(TentPostReference postReference)
        {
            return $"{this.prefix}{postReference.UserId}_{postReference.PostId}_{postReference.VersionId}";
        }
    }
}