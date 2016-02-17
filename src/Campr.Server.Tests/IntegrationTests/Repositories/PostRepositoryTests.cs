﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Models.Other.Factories;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;
using Campr.Server.Lib.Repositories;
using Campr.Server.Tests.Infrastructure;
using Campr.Server.Tests.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Campr.Server.Tests.IntegrationTests.Repositories
{
    public class PostRepositoryTests : IClassFixture<RethinkDbFixture>
    {
        public PostRepositoryTests()
        {
            this.tentPostFactory = ServiceProvider.Current.GetService<ITentPostFactory>();
            this.postTypeFactory = ServiceProvider.Current.GetService<ITentPostTypeFactory>();
            this.postRepository = ServiceProvider.Current.GetService<IPostRepository>();
        }

        private readonly ITentPostFactory tentPostFactory;
        private readonly ITentPostTypeFactory postTypeFactory;
        private readonly IPostRepository postRepository;

        [Fact]
        public async Task PostVersionAndLastVersion()
        {
            const string entity1 = "http://external1.tent.is";
            const string entity2 = "http://external2.tent.is";
            var user = new User { Id = Guid.NewGuid().ToString("N") };

            // Create a new post and add it to the db.
            var newPost = this.tentPostFactory.FromContent(user, new TentContentMeta
            {
                Entity = entity1
            }, this.postTypeFactory.FromString("https://test.com/type"));

            // Save the post to the db.
            await this.postRepository.UpdateAsync(newPost);

            // Keep the first version around.
            var version1 = newPost.Version;

            // Update the post and publish a new version.
            newPost.Content.Entity = entity2;
            newPost.Version = new TentVersion
            {
                UserId = user.Id,
                Type = newPost.Type
            };

            // Save the new version.
            await this.postRepository.UpdateAsync(newPost);

            // Retrieve the last version for this post.
            var postLastVersion = await this.postRepository.GetLastVersionAsync<TentContentMeta>(user.Id, newPost.Id);

            Assert.NotNull(postLastVersion);
            Assert.Equal(newPost.Version.Id, postLastVersion.Version.Id);
            Assert.Equal(entity2, postLastVersion.Content.Entity);

            // Retrieve the first version.
            var postVersion1 = await this.postRepository.GetAsync<TentContentMeta>(user.Id, newPost.Id, version1.Id);

            Assert.NotNull(postVersion1);
            Assert.Equal(version1.Id, postVersion1.Version.Id);
            Assert.Equal(entity1, postVersion1.Content.Entity);
        }

        [Fact]
        public async Task PostLastVersionOfType()
        {
            const string entity1 = "http://external1.tent.is";
            const string entity2 = "http://external2.tent.is";
            const string entity3 = "http://external3.tent.is";
            const string entity4 = "http://external4.tent.is";

            var type1 = this.postTypeFactory.FromString("https://test.com/type#type1");
            var type2 = this.postTypeFactory.FromString("https://test.com/type#type2");
            var typeWildcard = this.postTypeFactory.FromString("https://test.com/type");
            var user = new User { Id = Guid.NewGuid().ToString("N") };

            // Create two new posts of the same type.
            var newPost1 = this.tentPostFactory.FromContent(user, new TentContentMeta
            {
                Entity = entity1
            }, type1);

            var newPost2 = this.tentPostFactory.FromContent(user, new TentContentMeta
            {
                Entity = entity2
            }, type1);

            // Save the posts to the db.
            await this.postRepository.UpdateAsync(newPost1);
            await this.postRepository.UpdateAsync(newPost2);

            // Retrieve the last post version of this type.
            var lastPostVersionOfType1 = await this.postRepository.GetLastVersionOfTypeAsync<TentContentMeta>(user.Id, type1);

            Assert.NotNull(lastPostVersionOfType1);
            Assert.Equal(type1.ToString(), lastPostVersionOfType1.Type);
            Assert.Equal(entity2, lastPostVersionOfType1.Content.Entity);

            // Update this first post.
            newPost1.Content.Entity = entity3;
            newPost1.Version = new TentVersion
            {
                UserId = user.Id,
                Type = type1.ToString()
            };

            await this.postRepository.UpdateAsync(newPost1);

            // Verify that it's now the last post version of this type.
            var lastPostVersionOfType2 = await this.postRepository.GetLastVersionOfTypeAsync<TentContentMeta>(user.Id, type1);

            Assert.NotNull(lastPostVersionOfType2);
            Assert.Equal(type1.ToString(), lastPostVersionOfType2.Type);
            Assert.Equal(entity3, lastPostVersionOfType2.Content.Entity);

            // Create a new post with a different subtype.
            var newPost3 = this.tentPostFactory.FromContent(user, new TentContentMeta
            {
                Entity = entity4
            }, type2);

            await this.postRepository.UpdateAsync(newPost3);

            // Make sure that the previous request still returns the same post.
            var lastPostVersionOfType3 = await this.postRepository.GetLastVersionOfTypeAsync<TentContentMeta>(user.Id, type1);

            Assert.NotNull(lastPostVersionOfType3);
            Assert.Equal(type1.ToString(), lastPostVersionOfType3.Type);
            Assert.Equal(entity3, lastPostVersionOfType3.Content.Entity);

            // Perform a wildcard request, and make sure we get the new post.
            var lastPostVersionOfType4 = await this.postRepository.GetLastVersionOfTypeAsync<TentContentMeta>(user.Id, typeWildcard);

            Assert.NotNull(lastPostVersionOfType4);
            Assert.Equal(type2.ToString(), lastPostVersionOfType4.Type);
            Assert.Equal(entity4, lastPostVersionOfType4.Content.Entity);
        }

        [Fact]
        public async Task PostBulkRequest()
        {
            const string entity1 = "http://external1.tent.is";
            const string entity2 = "http://external2.tent.is";

            var type = this.postTypeFactory.FromString("https://test.com/type#type");
            var user = new User { Id = Guid.NewGuid().ToString("N") };

            // Create two new posts of the same type.
            var newPost1 = this.tentPostFactory.FromContent(user, new TentContentMeta
            {
                Entity = entity1
            }, type);

            var newPost2 = this.tentPostFactory.FromContent(user, new TentContentMeta
            {
                Entity = entity2
            }, type);

            // Save the posts to the db.
            await this.postRepository.UpdateAsync(newPost1);
            await this.postRepository.UpdateAsync(newPost2);
            
            // Retrieve these two posts in bulk.
            var references = new List<TentPostReference>
            {
                new TentPostReference { UserId = newPost1.UserId, PostId = newPost1.Id, VersionId = newPost1.Version.Id },
                new TentPostReference { UserId = newPost2.UserId, PostId = newPost2.Id, VersionId = newPost2.Version.Id }
            };

            var posts = await this.postRepository.GetBulkAsync<TentContentMeta>(references);

            // Check that the right posts were returned, in the correct order.
            Assert.NotNull(posts);
            Assert.Equal(2, posts.Count);

            Assert.True(posts.Any(p => p.Id == newPost1.Id));
            Assert.True(posts.Any(p => p.Id == newPost2.Id));
        }

        [Fact]
        public async Task PostDelete()
        {
            var type = this.postTypeFactory.FromString("https://test.com/type#type");
            var user = new User { Id = Guid.NewGuid().ToString("N") };

            // Create a new post and save it.
            var newPost = this.tentPostFactory.FromContent(user, new TentContentMeta
            {
                Entity = "http://external1.tent.is"
            }, type);

            await this.postRepository.UpdateAsync(newPost);

            // Delete this post and all its versions.
            await this.postRepository.DeleteAsync(newPost);

            // Make sure this post can't be found anymore.
            var postLastVersion = await this.postRepository.GetLastVersionAsync<TentContentMeta>(user.Id, newPost.Id);
            Assert.Null(postLastVersion);

            var postExactVersion = await this.postRepository.GetAsync<TentContentMeta>(user.Id, newPost.Id, newPost.Version.Id);
            Assert.Null(postExactVersion);
        }

        [Fact]
        public async Task PostDeleteVersion()
        {
            var type = this.postTypeFactory.FromString("https://test.com/type#type");
            var user = new User { Id = Guid.NewGuid().ToString("N") };

            // Create a new post and save it.
            var newPost = this.tentPostFactory.FromContent(user, new TentContentMeta
            {
                Entity = "http://external1.tent.is"
            }, type);

            await this.postRepository.UpdateAsync(newPost);

            // Delete this specific version of this post.
            await this.postRepository.DeleteAsync(newPost, true);

            // Make sure this post can't be found anymore.
            var postLastVersion = await this.postRepository.GetLastVersionAsync<TentContentMeta>(user.Id, newPost.Id);
            Assert.Null(postLastVersion);

            var postExactVersion = await this.postRepository.GetAsync<TentContentMeta>(user.Id, newPost.Id, newPost.Version.Id);
            Assert.Null(postExactVersion);
        }

        [Fact]
        public async Task PostDeleteLastVersion()
        {
            const string entity1 = "http://external1.tent.is";
            const string entity2 = "http://external2.tent.is";

            var type = this.postTypeFactory.FromString("https://test.com/type#type");
            var user = new User { Id = Guid.NewGuid().ToString("N") };

            // Create a new post and save it.
            var newPost = this.tentPostFactory.FromContent(user, new TentContentMeta
            {
                Entity = entity1
            }, type);

            await this.postRepository.UpdateAsync(newPost);

            // Update the post and publish a new version.
            var versionId1 = newPost.Version.Id;

            newPost.Content.Entity = entity2;
            newPost.Version = new TentVersion
            {
                UserId = user.Id,
                Type = newPost.Type
            };

            // Save the new version.
            await this.postRepository.UpdateAsync(newPost);

            // Delete the last version of this post.
            await this.postRepository.DeleteAsync(newPost, true);

            // Check that the first version still exists.
            var postVersion1 = await this.postRepository.GetAsync<TentContentMeta>(user.Id, newPost.Id, versionId1);
            Assert.NotNull(postVersion1);

            // Check that this second version doesn't exist anymore.
            var postVersion2 = await this.postRepository.GetAsync<TentContentMeta>(user.Id, newPost.Id, newPost.Version.Id);
            Assert.Null(postVersion2);

            // Check that the last version is now back to version 1.
            var postLastVersion = await this.postRepository.GetLastVersionAsync<TentContentMeta>(user.Id, newPost.Id);
            Assert.NotNull(postLastVersion);
            Assert.Equal(entity1, postLastVersion.Content.Entity);
        }
    }
}