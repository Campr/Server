using System.Collections.Generic;
using System.Linq;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Helpers
{
    class QueryStringHelpers : IQueryStringHelpers
    {
        #region Constructor & Private variables.

        public QueryStringHelpers(IUriHelpers uriHelpers)
        {
            Ensure.Argument.IsNotNull(uriHelpers, "uriHelpers");
            this.uriHelpers = uriHelpers;
        }

        private readonly IUriHelpers uriHelpers;

        #endregion

        #region Interface implementation.

        public IReadOnlyDictionary<string, IList<IList<string>>> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, IList<IList<string>>>();

            // Validate the string.
            if (string.IsNullOrEmpty(queryString) || queryString.Length < 1)
            {
                return result;
            }

            // Remove the leading "?".
            if (queryString[0] == '?')
            {
                queryString = queryString.Substring(1);
            }

            // Split keys and values.

            foreach (var keyValue in queryString.Split('&'))
            {
                var keyValueSeparatorIndex = keyValue.IndexOf('=');
                if (keyValueSeparatorIndex < 0)
                {
                    continue;
                }

                // Extract the key.
                var key = keyValue.Substring(0, keyValueSeparatorIndex);
                if (!result.ContainsKey(key))
                {
                    result[key] = new List<IList<string>>();
                }

                // Extract the value.
                var value = this.uriHelpers.UrlDecode(keyValue.Substring(keyValueSeparatorIndex + 1));
                result[key].Add(value.Split(',').ToList());
            }

            return result;
        }
        
        public string BuildQueryString(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            if (parameters == null)
            {
                return null;
            }

            return string.Join("&", parameters.SelectMany(kv =>
            {
                var andList = kv.Value as IEnumerable<object>;
                if (andList != null)
                {
                    return andList.Select(v =>
                    {
                        var result = kv.Key + '=';
                        var orList = v as IEnumerable<object>;
                        if (orList != null)
                        {
                            result += string.Join(",", orList.Select(this.GetQueryStringParameterValue));
                        }
                        else
                        {
                            result += this.GetQueryStringParameterValue(v);
                        }
                        return result;
                    });
                }

                return new[] { kv.Key + '=' + this.GetQueryStringParameterValue(kv.Value) };
            }));
        }

        #endregion

        #region Private methods.

        private string GetQueryStringParameterValue(object src)
        {
            return src is string ? this.uriHelpers.UrlEncode((string)src) : src.ToString();
        }

        #endregion
    }
}