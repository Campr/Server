using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Campr.Server.Lib.Data;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Repositories
{
    class PostRepository : IPostRepository
    { 
        public PostRepository(IDbClient client)
        {
            Ensure.Argument.IsNotNull(client, nameof(client));
            this.client = client;
            this.prefix = "post_";
        }

        private readonly IDbClient client;
        private readonly string prefix;
        
        public async Task<TentPost<T>> GetPostLastVersionAsync<T>(string userId, string postId) where T : class
        {
            using (var bucket = this.client.GetBucket())
            {
                // Create the view query to retrieve our post last version.
                var query = bucket.CreateQuery("posts", "posts_lastversion")
                    .Key(new [] { userId, postId }, true)
                    .Group(true)
                    .Limit(1);

                // Run this query on our bucket.
                var results = await bucket.QueryAsync<ViewPostResult>(query);

                // Retrieve and return the first result.
                var documentId = results.Rows.FirstOrDefault()?.Value?.DocId;
                if (string.IsNullOrEmpty(documentId))
                {
                    return null;
                }

                var operation = bucket.Get<TentPost<T>>(documentId);
                return operation.Value;
            }
        }

        public async Task<TentPost<T>> GetPostLastVersionByTypeAsync<T>(string userId, ITentPostType type) where T : class
        {
            using (var bucket = this.client.GetBucket())
            {
                // Create the view query to retrieve our post last version.
                var query = bucket.CreateQuery("posts", "posts_type_lastversion").Group(true);
                if (type.WildCard)
                {
                    query = query
                        .StartKey(new [] { userId, type.Type + '#' }, true)
                        .EndKey(new [] { userId, type.Type + '$' }, true)
                        .InclusiveEnd(false);
                }
                else
                {
                    query = query.Key(new [] { userId, type.ToString() }, true);
                }

                // Run this query on our bucket.
                var results = await bucket.QueryAsync<ViewPostResult>(query);

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

                var operation = bucket.Get<TentPost<T>>(documentId);
                return operation.Value;
            }
        }

        public async Task<TentPost<T>> GetPostAsync<T>(string userId, string postId, string versionId) where T : class
        {
            using (var bucket = this.client.GetBucket())
            {
                // Create the view query to retrieve our post version.
                var query = bucket.CreateQuery("posts", "posts_versions")
                    .Key(new [] { userId, postId, versionId }, true)
                    .Limit(1);

                // Run this query on our bucket.
                var results = await bucket.QueryAsync<object>(query);

                // Retrieve and return the first result.
                var documentId = results.Rows.FirstOrDefault()?.Id;
                if (string.IsNullOrEmpty(documentId))
                {
                    return null;
                }

                var operation = bucket.Get<TentPost<T>>(documentId);
                return operation.Value;
            }
        }

        public async Task<IList<TentPost<T>>> GetPostsAsync<T>(string userId, string postId) where T : class
        {
            using (var bucket = this.client.GetBucket())
            {
                // Create the view query to retrieve our post version.
                var query = bucket.CreateQuery("posts", "posts_versions")
                    .StartKey(new [] { userId, postId, "0" }, true)
                    .EndKey(new [] { userId, postId, "z" }, true);

                // Run this query on our bucket.
                var results = await bucket.QueryAsync<TentPost<T>>(query);

                // Retrieve and return the corresponding documents.
                var documentIds = results.Rows.Select(r => r.Id).ToList();
                var operations = bucket.Get<TentPost<T>>(documentIds);

                return operations
                    .Where(o => o.Value.Success)
                    .Select(o => o.Value.Value)
                    .ToList();
            }
        }

        public async Task<IList<TentPost<T>>> GetBulkPostsAsync<T>(IList<TentPostReference> references) where T : class
        {
            using (var bucket = this.client.GetBucket())
            {
                // Directly retrieve the documents for the provided references.
                var documentIds = references.Select(this.GetPostId).ToList();
                var operations = bucket.Get<TentPost<T>>(documentIds);

                return operations
                    .Where(o => o.Value.Success)
                    .Select(o => o.Value.Value)
                    .ToList();
            }
        }

        public async Task UpdatePostAsync<T>(TentPost<T> post) where T : class 
        {
            // Insert this post in the db.
            using (var bucket = this.client.GetBucket())
            {
                bucket.Upsert(this.GetPostId(post), post);
            }
        }

        public Task DeletePostAsync<T>(TentPost<T> post, bool specificVersion = false) where T : class
        {
            return this.DeletePostAsync(post.UserId, post.Id, specificVersion ? post.Version.Id : null);
        }

        public async Task DeletePostAsync(string userId, string postId, string versionId = null)
        {
            // Retrieve all the documents to update.
            var postsToDelete = new List<TentPost<object>>();
            if (string.IsNullOrWhiteSpace(versionId))
            {
                postsToDelete.Add(await this.GetPostAsync<object>(userId, postId, versionId));
            }
            else
            {
                postsToDelete.AddRange(await this.GetPostsAsync<object>(userId, postId));
            }

            // Set the deleted date on those documents.
            var deletedAt = DateTime.UtcNow;
            postsToDelete.ForEach(p => p.DeletedAt = deletedAt);

            // Update those documents on the Db.
            using (var bucket = this.client.GetBucket())
            {
                bucket.Upsert(postsToDelete.ToDictionary(this.GetPostId));
            }
        }

        private string GetPostId<T>(TentPost<T> post) where T : class
        {
            return $"{this.prefix}{post.UserId}_{post.Id}_{post.Version.Id}";
        }

        private string GetPostId(TentPostReference postReference)
        {
            return $"{this.prefix}{postReference.UserId}_{postReference.PostId}_{postReference.VersionId}";
        }
    }
}