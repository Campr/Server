using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Repositories;
using Campr.Server.Tests.Integration.Fixtures;
using Campr.Server.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Campr.Server.Tests.Integration.Repositories
{
    public class UserRepositoryTests : IClassFixture<CouchbaseBucketFixture>
    {
        public UserRepositoryTests(CouchbaseBucketFixture couchbaseBucket)
        {
            this.couchbaseBucket = couchbaseBucket;
            this.userFactory = ServiceProvider.Current.GetService<IUserFactory>();
            this.userRepository = ServiceProvider.Current.GetService<IUserRepository>();
        }
        
        private readonly CouchbaseBucketFixture couchbaseBucket;
        private readonly IUserFactory userFactory;
        private readonly IUserRepository userRepository;

        [Fact]
        public async Task CreateInternalUser()
        {
            const string handle = "user1";
            const string email = "user1@campr.me";

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
        }

        [Fact]
        public async Task CreateExternalUser()
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
    }
}
