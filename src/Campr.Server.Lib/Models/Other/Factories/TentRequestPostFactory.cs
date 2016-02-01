using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Logic;

namespace Campr.Server.Lib.Models.Other.Factories
{
    class TentRequestPostFactory : ITentRequestPostFactory
    {
        public TentRequestPostFactory(
            IUserLogic userLogic, 
            IUriHelpers uriHelpers)
        {
            Ensure.Argument.IsNotNull(userLogic, nameof(userLogic));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));

            this.userLogic = userLogic;
            this.uriHelpers = uriHelpers;
        }

        private readonly IUserLogic userLogic;
        private readonly IUriHelpers uriHelpers;

        public ITentRequestPost FromString(string post)
        {
            var result = new TentRequestPost(this.userLogic, this.uriHelpers);
            var requestPostParts = post.Split(' ');

            // Extract the entity.
            if (requestPostParts.Length > 0)
                result.Entity = this.uriHelpers.UrlDecode(requestPostParts[0]);

            // And the post id. 
            if (requestPostParts.Length > 1)
                result.PostId = this.uriHelpers.UrlDecode(requestPostParts[1]);

            return result;
        }
    }
}