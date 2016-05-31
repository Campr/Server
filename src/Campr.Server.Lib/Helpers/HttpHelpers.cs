using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Campr.Server.Lib.Helpers
{
    class HttpHelpers : IHttpHelpers
    {
        public IList<Uri> ReadLinksInHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers, string rel)
        {
            var result = new List<Uri>();

            // Convert header keys to lowercase.
            var headersDictionary = headers.ToDictionary(kv => kv.Key.ToLower(), kv => kv.Value);
            if (!headersDictionary.ContainsKey("link"))
            {
                return result;
            }

            var links = headersDictionary["link"];
            var linkRegex = new Regex(string.Format(
                CultureInfo.InvariantCulture,
                "<(.*)>; rel=\"{0}\"",
                rel));

            result.AddRange(links
                .Select(l => linkRegex.Match(l))
                .Where(m => m.Success)
                .Select(m => new Uri(m.Groups[1].Value, UriKind.RelativeOrAbsolute)));

            return result;
        }

        //public string ReadRelInContentType(MediaTypeHeaderValue contentType)
        //{
        //    // Read the rel parameter of the Content Type header.
        //    return contentType?.Parameters.FirstOrDefault(p => p.Name == "rel")?.Value.Trim('"');
        //}
    }
}