using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Repositories;
using Campr.Server.Tests.Infrastructure;
using Campr.Server.Tests.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Campr.Server.Tests.IntegrationTests.Repositories
{
    public class UserRepositoryTests : IClassFixture<RethinkDbFixture>
    {
        public UserRepositoryTests()
        {
            this.userFactory = ServiceProvider.Current.GetService<IUserFactory>();
            this.userRepository = ServiceProvider.Current.GetService<IUserRepository>();
        }
        
        private readonly IUserFactory userFactory;
        private readonly IUserRepository userRepository;

        [Fact]
        public async Task InternalUser()
        {
            const string handle = "user1";
            const string email = "user1email1@campr.me";

            // Create a new internal user.
            var newUser = this.userFactory.CreateUserFromHandle(handle);
            newUser.Email = email;

            // Save it to the Db.
            await this.userRepository.UpdateAsync(newUser);

            // Retrieve the newly created user by handle.
            var user = await this.userRepository.GetFromHandleAsync(handle);

            Assert.NotNull(user);
            Assert.Equal(email, user.Email);

            // Retrieve the Id by handle.
            var userId1 = await this.userRepository.GetIdFromHandleAsync(handle);
            Assert.Equal(newUser.Id, userId1);

            // Retrieve the user id by email.
            var userId2 = await this.userRepository.GetIdFromEmailAsync(email);
            Assert.Equal(newUser.Id, userId2);

            // Update the email for this user.
            user.Email = "user1email2@campr.me";
            await this.userRepository.UpdateAsync(user);

            // Retrieve the user by handle.
            var updatedUser = await this.userRepository.GetFromHandleAsync(handle);

            Assert.NotNull(updatedUser);
            Assert.Equal(newUser.Id, updatedUser.Id);
            Assert.Equal(user.Email, updatedUser.Email);
            
            var emailUserId = await this.userRepository.GetIdFromEmailAsync(user.Email);
            Assert.Equal(user.Id, emailUserId);
        }

        [Fact]
        public async Task ExternalUser()
        {
            const string entity = "http://external1.tent.is";
            
            // Create a new external user.
            var newUser = this.userFactory.CreateUserFromEntity(entity);
            
            // Save it to the Db.
            await this.userRepository.UpdateAsync(newUser);

            // Retrieve the newly created user.
            var user = await this.userRepository.GetFromEntityAsync(entity);

            Assert.NotNull(user);
            Assert.Equal(newUser.Id, user.Id);

            // Retrieve the Id by entity.
            var userId = await this.userRepository.GetIdFromEntityAsync(entity);
            Assert.Equal(newUser.Id, userId);
        }

        [Fact]
        public async Task UserVersions()
        {
            const string email1 = "user2email1@campr.me";
            const string email2 = "user2email2@campr.me";

            // Create a new user.
            var newUser = this.userFactory.CreateUserFromHandle("user2");
            newUser.Email = email1;

            // Save it to the Db.
            await this.userRepository.UpdateAsync(newUser);
            var version1 = newUser.VersionId;

            // Update the email address.
            newUser.Email = email2;

            // Save it again.
            await this.userRepository.UpdateAsync(newUser);

            // Retrieve the last version.
            var userLastVersion = await this.userRepository.GetAsync(newUser.Id);

            Assert.NotNull(userLastVersion);
            Assert.Equal(newUser.VersionId, userLastVersion.VersionId);
            Assert.Equal(email2, userLastVersion.Email);

            // Retrieve the previous version.
            var userVersion1 = await this.userRepository.GetAsync(newUser.Id, version1);

            Assert.NotNull(userVersion1);
            Assert.Equal(version1, userVersion1.VersionId);
            Assert.Equal(email1, userVersion1.Email);
        }

        [Fact]
        public async Task NonExistingUsers()
        {
            // Id by handle.
            var userIdFromHandle = await this.userRepository.GetIdFromHandleAsync("nonexistent");
            Assert.Null(userIdFromHandle);

            // Id by entity.
            var userIdFromEntity = await this.userRepository.GetIdFromEntityAsync("nonexistent");
            Assert.Null(userIdFromEntity);

            // Id by email.
            var userIdFromEmail = await this.userRepository.GetIdFromEmailAsync("nonexistent");
            Assert.Null(userIdFromEmail);

            // User by handle.
            var userFromHandle = await this.userRepository.GetFromHandleAsync("nonexistent");
            Assert.Null(userFromHandle);

            // User by entity.
            var userFromEntity = await this.userRepository.GetFromEntityAsync("nonexistent");
            Assert.Null(userFromEntity);
        }
    }
}
