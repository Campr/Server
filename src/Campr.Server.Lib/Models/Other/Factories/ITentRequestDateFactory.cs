using Campr.Server.Lib.Enums;

namespace Campr.Server.Lib.Models.Other.Factories
{
    public interface ITentRequestDateFactory
    {
        ITentRequestDate FromString(string date);
        ITentRequestDate FromPost(ITentRequestPost post, TentFeedRequestSort sortBy);
        ITentRequestDate MinValue();
    }
}