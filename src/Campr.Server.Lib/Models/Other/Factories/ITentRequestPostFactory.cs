using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Other.Factories
{
    public interface ITentRequestPostFactory
    {
        ITentRequestPost FromString(string post);
        ITentRequestPost FromPost(TentPost post);
    }
}