using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Other.Factories
{
    public interface ITentRequestPostFactory
    {
        ITentRequestPost FromString(string post);
        ITentRequestPost FromUser(User user);
        ITentRequestPost FromPost(TentPost post);
    }
}