using System;
using System.Linq;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Logic;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Repositories;

namespace Campr.Server.Lib.Models.Other.Factories
{
    class TentRequestPostFactory : ITentRequestPostFactory
    {
        public TentRequestPostFactory(
            IUserLogic userLogic,
            IUserRepository userRepository, 
            IPostRepository postRepository,
            IUriHelpers uriHelpers)
        {
            Ensure.Argument.IsNotNull(userLogic, nameof(userLogic));
            Ensure.Argument.IsNotNull(userRepository, nameof(userRepository));
            Ensure.Argument.IsNotNull(postRepository, nameof(postRepository));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));

            this.userLogic = userLogic;
            this.userRepository = userRepository;
            this.postRepository = postRepository;
            this.uriHelpers = uriHelpers;
        }

        private readonly IUserLogic userLogic;
        private readonly IUserRepository userRepository;
        private readonly IPostRepository postRepository;
        private readonly IUriHelpers uriHelpers;

        public ITentRequestPost FromString(string post)
        {
            Ensure.Argument.IsNotNullOrWhiteSpace(post, nameof(post));

            var result = new TentRequestPost(this.userLogic, this.userRepository, this.postRepository, this.uriHelpers);
            var requestPostParts = post.Split(' ');

            // Validate the provided string.
            if (!requestPostParts.Any())
                throw new ArgumentOutOfRangeException(nameof(post), "The provided Tent post isn't valid.");

            // Extract the entity.
            result.Entity = this.uriHelpers.UrlDecode(requestPostParts[0]);

            // And the post id. 
            if (requestPostParts.Length > 1)
                result.PostId = this.uriHelpers.UrlDecode(requestPostParts[1]);

            return result;
        }

        public ITentRequestPost FromUser(User user)
        {
            Ensure.Argument.IsNotNull(user, nameof(user));
            return new TentRequestPost(this.userLogic, this.userRepository, this.postRepository, this.uriHelpers)
            {
                User = user
            };
        }

        public ITentRequestPost FromPost(TentPost post)
        {
            Ensure.Argument.IsNotNull(post, nameof(post));
            return new TentRequestPost(this.userLogic, this.userRepository, this.postRepository, this.uriHelpers)
            {
                UserId = post.UserId,
                Post = post
            };
        }

        public ITentRequestPost FromMention(TentMention mention)
        {
            Ensure.Argument.IsNotNull(mention, nameof(mention));
            return new TentRequestPost(this.userLogic, this.userRepository, this.postRepository, this.uriHelpers)
            {
                UserId = mention.UserId,
                PostId = mention.PostId
            };
        }
    }
}