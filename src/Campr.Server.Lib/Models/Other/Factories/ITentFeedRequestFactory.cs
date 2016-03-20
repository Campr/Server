using System.Collections.Generic;

namespace Campr.Server.Lib.Models.Other.Factories
{
    public interface ITentFeedRequestFactory
    {
        ITentFeedRequest<TPost> Make<TPost>();
        ITentFeedRequest<TPost> FromQueryParameters<TPost>(IReadOnlyDictionary<string, IList<IList<string>>> queryString);
    }
}