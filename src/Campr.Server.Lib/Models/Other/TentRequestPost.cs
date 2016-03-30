using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Logic;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Repositories;

namespace Campr.Server.Lib.Models.Other
{
    public class TentRequestPost : ITentRequestPost
    {
        #region Constructor.
        
        public TentRequestPost(
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
        
        #endregion

        #region Public properties.

        public string UserId { get; set; }
        public string Entity { get; set; }
        public string PostId { get; set; }

        public User User { get; set; }
        public TentPost Post { get; set; }

        #endregion

        #region Methods.

        public async Task Resolve(CancellationToken cancellationToken = default(CancellationToken))
        {
            // If we don't have a User, retrieve it.
            if (this.User == null)
            {
                // If an entity was provided, use it to retrieve the corresponding user.
                if (!string.IsNullOrWhiteSpace(this.Entity))
                    this.User = await this.userLogic.GetUserAsync(this.Entity, cancellationToken);
                // Otherwise, use the User Id, if any.
                else if (!string.IsNullOrWhiteSpace(this.UserId))
                    this.User = await this.userRepository.GetAsync(this.UserId, cancellationToken);
            }

            // If we're missing a user at this point, throw.
            if (this.User == null)
                throw new Exception("Couldn't resolve the User for this Request Post.");

            // If we don't have a post, retrieve it.
            if (this.Post == null && !string.IsNullOrWhiteSpace(this.PostId))
                this.Post = await this.postRepository.GetAsync<object>(this.User.Id, this.PostId, cancellationToken);
        }

        public override string ToString()
        {
            return this.Post == null 
                ? this.uriHelpers.UrlEncode(this.User.Entity)
                : string.Format(
                    CultureInfo.InvariantCulture, 
                    "{0} {1}", 
                    this.uriHelpers.UrlEncode(this.User.Entity), 
                    this.uriHelpers.UrlEncode(this.Post.Id));
        }

        #endregion
    }
}