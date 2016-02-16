using System;
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
    }
}