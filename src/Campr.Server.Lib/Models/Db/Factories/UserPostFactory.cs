using System.Linq;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db.Factories
{
    class UserPostFactory : IUserPostFactory
    {
        public UserPost FromPost(string ownerId, TentPost post)
        {
            return new UserPost
            {
                OwnerId = ownerId,
                UserId = post.UserId,
                PostId = post.Id,
                VersionId = post.Version.Id,
                VersionReceivedAt = post.Version.ReceivedAt.GetValueOrDefault(),
                VersionPublishedAt = post.Version.PublishedAt.GetValueOrDefault(),
                ReceivedAt = post.ReceivedAt.GetValueOrDefault(),
                PublishedAt = post.PublishedAt.GetValueOrDefault(),
                Type = post.Type,
                Mentions = post.Mentions?.Select(this.BuildMention).ToList()
            };
        }

        private UserPostMention BuildMention(TentMention src)
        {
            return new UserPostMention
            {
                UserId = src.UserId,
                PostId = src.PostId,
                VersionId = src.VersionId
            };
        }
    }
}