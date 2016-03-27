using System.Collections.Generic;

namespace Campr.Server.Lib.Models.Other.Factories
{
    public interface ITentFeedRequestFactory
    {
        ITentFeedRequest Make();
        ITentFeedRequest FromQueryParameters(IReadOnlyDictionary<string, IList<IList<string>>> queryString);
    }
}