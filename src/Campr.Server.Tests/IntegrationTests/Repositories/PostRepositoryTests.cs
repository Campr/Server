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
        public async Task PostVersions()
        {
            const string entity1 = "http://external1.tent.is";
            const string entity2 = "http://external2.tent.is";
            var user = new User { Id = "userid" };

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
    }
}