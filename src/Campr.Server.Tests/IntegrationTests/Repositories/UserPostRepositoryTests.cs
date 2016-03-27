using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Helpers;
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
    public class UserPostRepositoryTests : IClassFixture<RethinkDbFixture>
    {
        public UserPostRepositoryTests()
        {
            this.userPostRepository = ServiceProvider.Current.GetService<IUserPostRepository>();
            this.postFactory = ServiceProvider.Current.GetService<ITentPostFactory>();
            this.postTypeFactory = ServiceProvider.Current.GetService<ITentPostTypeFactory>();
            this.modelHelpers = ServiceProvider.Current.GetService<IModelHelpers>();
        }

        private readonly IUserPostRepository userPostRepository;
        private readonly ITentPostFactory postFactory;
        private readonly ITentPostTypeFactory postTypeFactory;
        private readonly IModelHelpers modelHelpers;

        [Fact]
        public async Task UserPostVersionAndLastVersion()
        {
            var ownerId = Guid.NewGuid().ToString("N");
            var user = new User { Id = Guid.NewGuid().ToString("N") };
            var date1 = DateTime.UtcNow;
            var date2 = DateTime.UtcNow.AddSeconds(1);

            // Create a new post and use it to update the corresponding user post.
            var newPost = this.postFactory.FromContent(user, new TentContentMeta
            {
                Entity = "http://external1.tent.is"
            }, this.postTypeFactory.FromString("https://test.com/type")).Post();

            // Compute the VersionId for this post.
            newPost.Version.Id = this.modelHelpers.GetVersionIdFromPost(newPost);
            newPost.Version.ReceivedAt = date1;
            newPost.Version.PublishedAt = date1;
            newPost.ReceivedAt = date1;
            newPost.PublishedAt = date1;

            // Save the user post.
            await this.userPostRepository.UpdateAsync(ownerId, newPost, false);

            // Create a new version of the same post.
            var versionId1 = newPost.Version.Id;
            newPost.Content.Entity = "http://external2.tent.is";
            newPost.Version = new TentVersion
            {
                UserId = user.Id,
                Type = newPost.Type,
                ReceivedAt = date2,
                PublishedAt = date2
            };
            newPost.Version.Id = this.modelHelpers.GetVersionIdFromPost(newPost);

            // Update the user post.
            await this.userPostRepository.UpdateAsync(ownerId, newPost, false);

            // Retrieve the last version for this user post.
            var userPostLastVersion = await this.userPostRepository.GetAsync(ownerId, user.Id, newPost.Id);

            Assert.NotNull(userPostLastVersion);
            Assert.Equal(newPost.Version.Id, userPostLastVersion.VersionId);

            // Retrieve a specific version for this user post.
            var userPostVersion1 = await this.userPostRepository.GetAsync(ownerId, user.Id, newPost.Id, versionId1);

            Assert.NotNull(userPostVersion1);
            Assert.Equal(versionId1, userPostVersion1.VersionId);
        }

        [Fact]
        public async Task UserPostDelete()
        {
            var ownerId = Guid.NewGuid().ToString("N");
            var user = new User { Id = Guid.NewGuid().ToString("N") };
            var date = DateTime.UtcNow;

            // Create a new post and use it to update the corresponding user post.
            var newPost = this.postFactory.FromContent(user, new TentContentMeta
            {
                Entity = "http://external1.tent.is"
            }, this.postTypeFactory.FromString("https://test.com/type")).Post();

            // Compute the VersionId for this post.
            newPost.Version.Id = this.modelHelpers.GetVersionIdFromPost(newPost);
            newPost.Version.ReceivedAt = date;
            newPost.Version.PublishedAt = date;
            newPost.ReceivedAt = date;
            newPost.PublishedAt = date;

            // Save the user post.
            await this.userPostRepository.UpdateAsync(ownerId, newPost, false);

            // Delete this user post and all its versions.
            await this.userPostRepository.DeleteAsync(ownerId, newPost);

            // Make sure this post can't be found anymore.
            var userPostLastVersion = await this.userPostRepository.GetAsync(ownerId, user.Id, newPost.Id);
            Assert.Null(userPostLastVersion);

            var userPostVersion = await this.userPostRepository.GetAsync(ownerId, user.Id, newPost.Id, newPost.Version.Id);
            Assert.Null(userPostVersion);
        }

        [Fact]
        public async Task UserPostDeleteVersion()
        {
            var ownerId = Guid.NewGuid().ToString("N");
            var user = new User { Id = Guid.NewGuid().ToString("N") };
            var date = DateTime.UtcNow;

            // Create a new post and use it to update the corresponding user post.
            var newPost = this.postFactory.FromContent(user, new TentContentMeta
            {
                Entity = "http://external1.tent.is"
            }, this.postTypeFactory.FromString("https://test.com/type")).Post();

            // Compute the VersionId for this post.
            newPost.Version.Id = this.modelHelpers.GetVersionIdFromPost(newPost);
            newPost.Version.ReceivedAt = date;
            newPost.Version.PublishedAt = date;
            newPost.ReceivedAt = date;
            newPost.PublishedAt = date;

            // Save the user post.
            await this.userPostRepository.UpdateAsync(ownerId, newPost, false);

            // Delete this user post and all its versions.
            await this.userPostRepository.DeleteAsync(ownerId, newPost, true);

            // Make sure this post can't be found anymore.
            var userPostLastVersion = await this.userPostRepository.GetAsync(ownerId, user.Id, newPost.Id);
            Assert.Null(userPostLastVersion);

            var userPostVersion = await this.userPostRepository.GetAsync(ownerId, user.Id, newPost.Id, newPost.Version.Id);
            Assert.Null(userPostVersion);
        }

        [Fact]
        public async Task UserPostDeleteLastVersion()
        {
            var ownerId = Guid.NewGuid().ToString("N");
            var user = new User { Id = Guid.NewGuid().ToString("N") };
            var date1 = DateTime.UtcNow;
            var date2 = DateTime.UtcNow.AddSeconds(1);

            // Create a new post and use it to update the corresponding user post.
            var newPost = this.postFactory.FromContent(user, new TentContentMeta
            {
                Entity = "http://external1.tent.is"
            }, this.postTypeFactory.FromString("https://test.com/type")).Post();

            // Compute the VersionId for this post.
            newPost.Version.Id = this.modelHelpers.GetVersionIdFromPost(newPost);
            newPost.Version.ReceivedAt = date1;
            newPost.Version.PublishedAt = date1;
            newPost.ReceivedAt = date1;
            newPost.PublishedAt = date1;

            // Save the user post.
            await this.userPostRepository.UpdateAsync(ownerId, newPost, false);

            // Create a new version of the same post.
            var versionId1 = newPost.Version.Id;
            newPost.Content.Entity = "http://external2.tent.is";
            newPost.Version = new TentVersion
            {
                UserId = user.Id,
                Type = newPost.Type,
                ReceivedAt = date2,
                PublishedAt = date2
            };
            newPost.Version.Id = this.modelHelpers.GetVersionIdFromPost(newPost);

            // Update the user post.
            await this.userPostRepository.UpdateAsync(ownerId, newPost, false);

            // Delete the last version for this user post.
            await this.userPostRepository.DeleteAsync(ownerId, newPost, true);

            // Check that the first version still exists.
            var userPostVersion1 = await this.userPostRepository.GetAsync(ownerId, user.Id, newPost.Id, versionId1);
            Assert.NotNull(userPostVersion1);

            // Check that the second version was deleted.
            var userPostVersion2 = await this.userPostRepository.GetAsync(ownerId, user.Id, newPost.Id, newPost.Version.Id);
            Assert.Null(userPostVersion2);

            // Check that the last version is now back to version 1.
            var userPostLastVersion = await this.userPostRepository.GetAsync(ownerId, user.Id, newPost.Id);
            Assert.NotNull(userPostLastVersion);
            Assert.Equal(versionId1, userPostLastVersion.VersionId);
        }
    }
}