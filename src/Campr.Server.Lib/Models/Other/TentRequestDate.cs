using System;
using System.Globalization;
using System.Threading.Tasks;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Logic;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Repositories;

namespace Campr.Server.Lib.Models.Other
{
    class TentRequestDate : ITentRequestDate
    {
        #region Constructor.

        public TentRequestDate(IUserLogic userLogic, 
            IPostRepository postRepository,
            IUriHelpers uriHelpers)
        {
            Ensure.Argument.IsNotNull(userLogic, nameof(userLogic));
            Ensure.Argument.IsNotNull(postRepository, nameof(postRepository));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));

            this.uriHelpers = uriHelpers;
            this.userLogic = userLogic;
            this.postRepository = postRepository;
        }

        private readonly IUserLogic userLogic;
        private readonly IPostRepository postRepository;
        private readonly IUriHelpers uriHelpers;

        #endregion

        #region Public properties.

        public DateTime? Date { get; set; }
        public string Version { get; set; }
        public string Entity { get; set; }
        public string PostId { get; set; }

        #endregion

        #region Computed properties.

        public bool IsValid => this.Date.HasValue;

        #endregion
        
        #region Methods.

        public async Task ResolvePost(Func<TentPost, DateTime?> sortPropertySelector)
        {
            Ensure.Argument.IsNotNull(sortPropertySelector, "sortPropertySelector");

            // If there's nothing to resolve, return.
            if (this.IsValid 
                || string.IsNullOrWhiteSpace(this.Entity)
                || string.IsNullOrWhiteSpace(this.PostId))
            {
                return;
            }

            // Retrieve the UserId for this entity.
            var userId = await this.userLogic.GetUserIdAsync(this.Entity);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            // Retrieve the Post.
            var post = await this.postRepository.GetLastVersionAsync<object>(userId, this.PostId);
            if (post == null)
            {
                return;
            }

            // Extract data from the post.
            this.Date = sortPropertySelector(post);
            this.Version = post.Version.Id;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(this.Version)
                ? this.Date.GetValueOrDefault().ToUnixTime().ToString()
                : string.Format(CultureInfo.InvariantCulture, "{0}+{1}", this.Date.GetValueOrDefault().ToUnixTime(), this.uriHelpers.UrlEncode(this.Version));
        }

        #endregion
    }
}
