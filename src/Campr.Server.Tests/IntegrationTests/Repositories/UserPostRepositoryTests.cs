using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Enums;
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
            this.userRepository = ServiceProvider.Current.GetService<IUserRepository>();
            this.userPostRepository = ServiceProvider.Current.GetService<IUserPostRepository>();
            this.postFactory = ServiceProvider.Current.GetService<ITentPostFactory>();
            this.postTypeFactory = ServiceProvider.Current.GetService<ITentPostTypeFactory>();
            this.feedRequestFactory = ServiceProvider.Current.GetService<ITentFeedRequestFactory>();
            this.requestPostFactory = ServiceProvider.Current.GetService<ITentRequestPostFactory>();
            this.modelHelpers = ServiceProvider.Current.GetService<IModelHelpers>();
        }

        private readonly IUserRepository userRepository;
        private readonly IUserPostRepository userPostRepository;
        private readonly ITentPostFactory postFactory;
        private readonly ITentPostTypeFactory postTypeFactory;
        private readonly ITentFeedRequestFactory feedRequestFactory;
        private readonly ITentRequestPostFactory requestPostFactory;
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
            newPost.Version.ReceivedAt = date1;
            newPost.Version.PublishedAt = date1;
            newPost.ReceivedAt = date1;
            newPost.PublishedAt = date1;
            newPost.Version.Id = this.modelHelpers.GetVersionIdFromPost(newPost);

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
        public async Task UserPostFeedRequest()
        {
            var ownerId = Guid.NewGuid().ToString("N");

            var user1 = new User { Id = Guid.NewGuid().ToString("N") };
            var user2 = new User { Id = Guid.NewGuid().ToString("N") };

            await this.userRepository.UpdateAsync(user1);
            await this.userRepository.UpdateAsync(user2);

            var type1 = this.postTypeFactory.FromString("https://test.com/type#type1");
            var type2 = this.postTypeFactory.FromString("https://test.com/type2#type2");

            // Create three new posts and use them to update the corresponding user posts.
            var newPost1 = this.postFactory.FromContent(user1, new TentContentMeta(), type1)
                .WithMentions(new TentMention
                {
                    UserId = user2.Id
                })
                .Post();

            var newPost2 = this.postFactory.FromContent(user2, new TentContentMeta(), type1).Post();
            newPost2.ReceivedAt = newPost2.PublishedAt.GetValueOrDefault().AddSeconds(2);

            var newPost3 = this.postFactory.FromContent(user2, new TentContentMeta(), type2)
                .WithMentions(new TentMention
                {
                    UserId = newPost1.UserId,
                    PostId = newPost1.Id,
                    VersionId = newPost1.Version.Id
                })
                .Post();
            newPost3.ReceivedAt = newPost3.PublishedAt.GetValueOrDefault().AddSeconds(4);

            // Save the corresponding User Posts.
            await this.userPostRepository.UpdateAsync(ownerId, newPost1, false);
            await this.userPostRepository.UpdateAsync(ownerId, newPost2, true);
            await this.userPostRepository.UpdateAsync(ownerId, newPost3, false);

            // Create a feed request to retrieve all the posts.
            var feedRequestAll = this.feedRequestFactory.Make();
            var feedResultAll = await this.userPostRepository.GetAsync(ownerId, feedRequestAll);

            Assert.Equal(3, feedResultAll.Count);
            Assert.Equal(newPost3.Id, feedResultAll[0].PostId);
            Assert.Equal(newPost1.Id, feedResultAll[2].PostId);

            // Create a feed request to retrieve all the posts by User2.
            var feedRequestFromUser2 = this.feedRequestFactory.Make()
                .AddUsers(user2);
            var feedResultFromUser2 = await this.userPostRepository.GetAsync(ownerId, feedRequestFromUser2);

            Assert.Equal(2, feedResultFromUser2.Count);
            Assert.Equal(newPost3.Id, feedResultFromUser2[0].PostId);
            Assert.Equal(newPost2.Id, feedResultFromUser2[1].PostId);

            // Create a feed request to retrieve all the posts of Type1.
            var feedRequestOfType1 = this.feedRequestFactory.Make()
                .AddTypes(type1);
            var feedResultOfType1 = await this.userPostRepository.GetAsync(ownerId, feedRequestOfType1);

            Assert.Equal(2, feedResultOfType1.Count);
            Assert.Equal(newPost2.Id, feedResultOfType1[0].PostId);
            Assert.Equal(newPost1.Id, feedResultOfType1[1].PostId);

            // Create a feed request to retrieve all the posts of a wildcard type.
            var feedRequestOfWildcardType1 = this.feedRequestFactory.Make()
                .AddTypes(this.postTypeFactory.FromString(type1.Type));
            var feedResultOfWildcardType1 = await this.userPostRepository.GetAsync(ownerId, feedRequestOfWildcardType1);

            Assert.Equal(2, feedResultOfWildcardType1.Count);
            Assert.Equal(newPost2.Id, feedResultOfWildcardType1[0].PostId);
            Assert.Equal(newPost1.Id, feedResultOfWildcardType1[1].PostId);

            // Create a feed request that combines conditions (User + Type).
            var feedRequestUserAndType = this.feedRequestFactory.Make()
                .AddUsers(user2)
                .AddTypes(type1);
            var feedResultUserAndType = await this.userPostRepository.GetAsync(ownerId, feedRequestUserAndType);

            Assert.Equal(1, feedResultUserAndType.Count);
            Assert.Equal(newPost2.Id, feedResultUserAndType[0].PostId);

            // Create a feed request that skips the first item.
            var feedRequestSkip = this.feedRequestFactory.Make()
                .AddSkip(1);
            var feedResultSkip = await this.userPostRepository.GetAsync(ownerId, feedRequestSkip);

            Assert.Equal(2, feedResultSkip.Count);
            Assert.Equal(newPost2.Id, feedResultSkip[0].PostId);
            Assert.Equal(newPost1.Id, feedResultSkip[1].PostId);

            // Create a feed request that limits the results to 2.
            var feedRequestLimit = this.feedRequestFactory.Make()
                .AddLimit(2);
            var feedResultLimit = await this.userPostRepository.GetAsync(ownerId, feedRequestLimit);

            Assert.Equal(2, feedResultLimit.Count);
            Assert.Equal(newPost3.Id, feedResultLimit[0].PostId);
            Assert.Equal(newPost2.Id, feedResultLimit[1].PostId);

            // Create a feed request that combines Skip + Limit.
            var feedRequestSkipAndLimit = this.feedRequestFactory.Make()
                .AddSkip(1)
                .AddLimit(1);
            var feedResultSkipAndLimit = await this.userPostRepository.GetAsync(ownerId, feedRequestSkipAndLimit);

            Assert.Equal(1, feedResultSkipAndLimit.Count);
            Assert.Equal(newPost2.Id, feedResultSkipAndLimit[0].PostId);

            // Create a feed request to find all the posts until Post1.
            var feedRequestUntilPost1 = this.feedRequestFactory.Make()
                .AddPostBoundary(this.requestPostFactory.FromPost(newPost1), TentFeedRequestBoundaryType.Until);
            var feedResultUntilPost1 = await this.userPostRepository.GetAsync(ownerId, feedRequestUntilPost1);

            Assert.Equal(2, feedResultUntilPost1.Count);  
            Assert.Equal(newPost3.Id, feedResultUntilPost1[0].PostId);
            Assert.Equal(newPost2.Id, feedResultUntilPost1[1].PostId);

            // Create a feed request to find all posts since Post1.
            var feedRequestSincePost1 = this.feedRequestFactory.Make()
                .AddPostBoundary(this.requestPostFactory.FromPost(newPost1), TentFeedRequestBoundaryType.Since);
            var feedResultSincePost1 = await this.userPostRepository.GetAsync(ownerId, feedRequestSincePost1);

            Assert.Equal(2, feedResultSincePost1.Count);
            Assert.Equal(newPost2.Id, feedResultSincePost1[0].PostId); // Notice the inverted order here.
            Assert.Equal(newPost3.Id, feedResultSincePost1[1].PostId);

            // Create a feed request to find the first post since Post1.
            var feedRequestFirstSincePost1 = this.feedRequestFactory.Make()
                .AddPostBoundary(this.requestPostFactory.FromPost(newPost1), TentFeedRequestBoundaryType.Since)
                .AddLimit(1);
            var feedResultFirstSincePost1 = await this.userPostRepository.GetAsync(ownerId, feedRequestFirstSincePost1);

            Assert.Equal(1, feedResultFirstSincePost1.Count);
            Assert.Equal(newPost2.Id, feedResultFirstSincePost1[0].PostId);

            // Create a feed request to find all posts before Post3.
            var feedRequestBeforePost3 = this.feedRequestFactory.Make()
                .AddPostBoundary(this.requestPostFactory.FromPost(newPost3), TentFeedRequestBoundaryType.Before);
            var feedResultBeforePost3 = await this.userPostRepository.GetAsync(ownerId, feedRequestBeforePost3);

            Assert.Equal(2, feedResultBeforePost3.Count);
            Assert.Equal(newPost2.Id, feedResultBeforePost3[0].PostId);
            Assert.Equal(newPost1.Id, feedResultBeforePost3[1].PostId);
                
            // Create a feed request to find posts that mention User2.
            var feedRequestMentionsUser2 = this.feedRequestFactory.Make()
                .AddMentions(this.requestPostFactory.FromUser(user2));
            var feedResultMentionsUser2 = await this.userPostRepository.GetAsync(ownerId, feedRequestMentionsUser2);

            Assert.Equal(1, feedResultMentionsUser2.Count);
            Assert.Equal(newPost1.Id, feedResultMentionsUser2[0].PostId);

            // Create a feed request to find posts that don't mention User2.
            var feedRequestNotMentionsUser2 = this.feedRequestFactory.Make()
                .AddNotMentions(this.requestPostFactory.FromUser(user2));
            var feedResultNotMentionsUser2 = await this.userPostRepository.GetAsync(ownerId, feedRequestNotMentionsUser2);
             
            Assert.Equal(2, feedResultNotMentionsUser2.Count);
            Assert.Equal(newPost3.Id, feedResultNotMentionsUser2[0].PostId);
            Assert.Equal(newPost2.Id, feedResultNotMentionsUser2[1].PostId);

            // Create a feed request to find posts that mention NewPost1.
            var feedRequestMentionsPost1 = this.feedRequestFactory.Make()
                .AddMentions(this.requestPostFactory.FromPost(newPost1));
            var feedResultMentionsPost1 = await this.userPostRepository.GetAsync(ownerId, feedRequestMentionsPost1);

            Assert.Equal(1, feedResultMentionsPost1.Count);
            Assert.Equal(newPost3.Id, feedResultMentionsPost1[0].PostId);

            // Create a feed request to find posts that mention NewPost2 (none).
            var feedRequestMentionsPost2 = this.feedRequestFactory.Make()
                .AddMentions(this.requestPostFactory.FromPost(newPost2));
            var feedResultMentionsPost2 = await this.userPostRepository.GetAsync(ownerId, feedRequestMentionsPost2);

            Assert.Equal(0, feedResultMentionsPost2.Count);

            // Create a feed request to find posts that don't mention NewPost3 (all).
            var feedRequestNotMentionsPost3 = this.feedRequestFactory.Make()
                .AddNotMentions(this.requestPostFactory.FromPost(newPost3));
            var feedResultNotMentionsPost3 = await this.userPostRepository.GetAsync(ownerId, feedRequestNotMentionsPost3);

            Assert.Equal(3, feedResultNotMentionsPost3.Count);
            Assert.Equal(newPost3.Id, feedResultNotMentionsPost3[0].PostId);
            Assert.Equal(newPost1.Id, feedResultNotMentionsPost3[2].PostId);
        }

        [Fact]
        public async Task UserPostFeedRequestCount()
        {
            var ownerId = Guid.NewGuid().ToString("N");

            var user1 = new User { Id = Guid.NewGuid().ToString("N") };
            var user2 = new User { Id = Guid.NewGuid().ToString("N") };

            await this.userRepository.UpdateAsync(user1);
            await this.userRepository.UpdateAsync(user2);

            var type1 = this.postTypeFactory.FromString("https://test.com/type#type1");
            var type2 = this.postTypeFactory.FromString("https://test.com/type2#type2");

            // Create three new posts and use them to update the corresponding user posts.
            var newPost1 = this.postFactory.FromContent(user1, new TentContentMeta(), type1)
                .WithMentions(new TentMention
                {
                    UserId = user2.Id
                })
                .Post();

            var newPost2 = this.postFactory.FromContent(user2, new TentContentMeta(), type1).Post();
            newPost2.ReceivedAt = newPost2.PublishedAt.GetValueOrDefault().AddSeconds(2);

            var newPost3 = this.postFactory.FromContent(user2, new TentContentMeta(), type2)
                .WithMentions(new TentMention
                {
                    UserId = newPost1.UserId,
                    PostId = newPost1.Id,
                    VersionId = newPost1.Version.Id
                })
                .Post();
            newPost3.ReceivedAt = newPost3.PublishedAt.GetValueOrDefault().AddSeconds(4);

            // Save the corresponding User Posts.
            await this.userPostRepository.UpdateAsync(ownerId, newPost1, false);
            await this.userPostRepository.UpdateAsync(ownerId, newPost2, true);
            await this.userPostRepository.UpdateAsync(ownerId, newPost3, false);

            // Create a feed request to count all the posts.
            var feedRequestAll = this.feedRequestFactory.Make();
            var feedCountAll = await this.userPostRepository.CountAsync(ownerId, feedRequestAll);

            Assert.Equal(3, feedCountAll);

            // Create a feed request to count all the posts by User2.
            var feedRequestFromUser2 = this.feedRequestFactory.Make()
                .AddUsers(user2);
            var feedCountFromUser2 = await this.userPostRepository.CountAsync(ownerId, feedRequestFromUser2);

            Assert.Equal(2, feedCountFromUser2);

            // Make sure the count isn't affected by skipping posts.
            var feedRequestAllSkip = this.feedRequestFactory.Make()
                .AddSkip(2);
            var feedCountAllSkip = await this.userPostRepository.CountAsync(ownerId, feedRequestAllSkip);

            Assert.Equal(3, feedCountAllSkip);

            // Make sure the count isn't affected by limiting posts.
            var feedRequestAllLimit = this.feedRequestFactory.Make()
                .AddLimit(2);
            var feedCountAllLimit = await this.userPostRepository.CountAsync(ownerId, feedRequestAllLimit);

            Assert.Equal(3, feedCountAllLimit);
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