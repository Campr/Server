using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Models.Tent.PostContent;
using Campr.Server.Lib.Repositories;
using Campr.Server.Lib.Services;

namespace Campr.Server.Lib.Logic
{
    class UserLogic : IUserLogic
    {
        public UserLogic(IUserRepository userRepository,
            IUserFactory userFactory,
            IDiscoveryService discoveryService,
            ILoggingService loggerService,
            IUriHelpers uriHelpers,
            ICryptoHelpers cryptoHelpers)
        {
            Ensure.Argument.IsNotNull(userRepository, nameof(userRepository));
            Ensure.Argument.IsNotNull(userFactory, nameof(userFactory));
            Ensure.Argument.IsNotNull(discoveryService, nameof(discoveryService));
            Ensure.Argument.IsNotNull(loggerService, nameof(loggerService));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            Ensure.Argument.IsNotNull(cryptoHelpers, nameof(cryptoHelpers));
            
            this.userRepository = userRepository;
            this.userFactory = userFactory;
            this.discoveryService = discoveryService;
            this.loggerService = loggerService;
            this.uriHelpers = uriHelpers;
            this.cryptoHelpers = cryptoHelpers;
        }
        
        private readonly IUserRepository userRepository;
        private readonly IUserFactory userFactory;
        private readonly IDiscoveryService discoveryService;
        private readonly ILoggingService loggerService;
        private readonly IUriHelpers uriHelpers;
        private readonly ICryptoHelpers cryptoHelpers;

        public Task<string> GetUserIdAsync(string entityOrHandle)
        {
            // Make sure we have something to work with.
            if (string.IsNullOrWhiteSpace(entityOrHandle))
                return null;

            // If this is a Campr handle or entity, search by handle.
            string camprHandle = null;
            if (this.uriHelpers.IsCamprHandle(entityOrHandle) || this.uriHelpers.IsCamprEntity(entityOrHandle, out camprHandle))
                return this.userRepository.GetIdFromHandleAsync(camprHandle ?? entityOrHandle);

            // Otherwise, search by entity.
            return this.userRepository.GetIdFromEntityAsync(entityOrHandle);
        }

        public async Task<User> GetUserAsync(string entityOrHandle)
        {
            if (string.IsNullOrWhiteSpace(entityOrHandle))
                return null;
            
            // Retry in case of conflicts when creating new users.
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    // First, try to retrieve the user internally.
                    var userId = await this.GetUserIdAsync(entityOrHandle);
                    if (!string.IsNullOrWhiteSpace(userId))
                        return await this.userRepository.GetAsync(userId);

                    // If this is a Campr handle, and we didn't find a user, return now.
                    if (this.uriHelpers.IsCamprHandle(entityOrHandle))
                        return null;

                    // Otherwise, retrieve the entity's profile (with discovery).
                    var metaPost = await this.discoveryService.DiscoverUriAsync<TentContentMeta>(new Uri(entityOrHandle, UriKind.Absolute));
                    if (metaPost != null)
                    {
                        // If the actual entity was different, Try to find the corresponding user in our db.
                        if (metaPost.Content.Entity != entityOrHandle)
                        {
                            userId = await this.GetUserIdAsync(metaPost.Entity);
                            if (!string.IsNullOrWhiteSpace(userId))
                                return await this.userRepository.GetAsync(userId);

                            entityOrHandle = metaPost.Entity;
                        }
                    }

                    // Otherwise, create the user.
                    var newUser = this.userFactory.CreateUserFromEntity(entityOrHandle);

                    // Save both.
                    await this.userRepository.UpdateAsync(newUser);

                    // If needed, create the first metaPost for this user.
                    //if (metaPost != null)
                    //{
                    //    await this.postLogic.CreatePostAsync(newUser, metaPost);
                    //}

                    return newUser;
                }
                catch (Exception ex)
                {
                    this.loggerService.Exception(ex, "Failed to retrieve user: {0}", entityOrHandle);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            return null;
        }

        public async Task<User> CreateUserAsync(string name, string email, string password, string handle)
        {
            // Create the new user object.
            var user = this.userFactory.CreateUserFromHandle(handle);
            user.Email = email;

            // Create the password hash and corresponding salt.
            byte[] passwordSalt;
            user.Password = this.cryptoHelpers.CreatePasswordKeyAndSalt(password, out passwordSalt);
            user.PasswordSalt = passwordSalt;

            //// Create the Meta Post.
            //var meta = new TentContentMeta
            //{
            //    Entity = this.uriHelpers.GetCamprTentEntity(handle),
            //    Profile = new ApiMetaProfile
            //    {
            //        Name = name
            //    },
            //    Servers = new List<ApiMetaServer>
            //    {
            //        this.tentMetaServerFactory.FromUser(user)
            //    }
            //};

            //// Generate the user's avatar.
            //var avatar = this.imageEngine.GenerateDefaultAvatar(user.Handle, 500);
            //var avatarAttachment = this.tentAttachmentFactory.FromByteArray("default_avatar.jpg", "avatar", "image/jpeg", avatar);

            // Save the newly created objects to the data store.
            await this.userRepository.UpdateAsync(user);

            //// Save the Meta Post.
            //await this.postLogic.CreateNewPostAsync(user, this.tentConstants.MetaPostType(), meta, true, null, null, new []
            //{
            //    avatarAttachment
            //});

            //// Save an empty Campr Profile Post.
            //await this.postLogic.CreateNewPostAsync<object>(user, this.tentConstants.CamprProfilePostType(), null);

            return user;
        }

        //public DbSession CreateSession(DbUser user)
        //{
        //    var session = this.dbSessionFactory.FromUser(user);
        //    this.sessionRepository.CreateSession(session);
        //    return session;
        //}
    }
}