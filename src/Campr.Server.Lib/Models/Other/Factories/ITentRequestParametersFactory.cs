using System.Collections.Generic;
using Campr.Server.Lib.Enums;

namespace Campr.Server.Lib.Models.Other.Factories
{
    public interface ITentRequestParametersFactory
    {
        ITentRequestParameters FromQueryString(IReadOnlyDictionary<string, IList<IList<string>>> queryString, CacheControlValue cacheControl);
    }
}