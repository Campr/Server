using System.Collections.Generic;

namespace Campr.Server.Lib.Helpers
{
    public interface IQueryStringHelpers
    {
        IReadOnlyDictionary<string, IList<IList<string>>> ParseQueryString(string queryString);

        string BuildQueryString(IEnumerable<KeyValuePair<string, object>> parameters);
    }
}