using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db.Factories
{
    public interface IUserPostFactory
    {
        UserPost FromPost(string ownerId, TentPost post, bool isFromFollowing);
    }
}