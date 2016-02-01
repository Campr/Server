using System;
using Microsoft.AspNet.Http;

namespace Campr.Server.Lib.Extensions
{
    public static class HttpRequestExtensions
    {
        public static Uri ToUri(this HttpRequest request)
        {
            // We split the host to account for the port.
            var hostComponents = request.Host.ToUriComponent().Split(':');

            // Use a Uri builder to create our new Uri.
            var builder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = hostComponents[0],
                Path = request.Path,
                Query = request.QueryString.ToUriComponent()
            };

            // If there was indeed a port, add it to our Uri.
            if (hostComponents.Length == 2)
                builder.Port = Convert.ToInt32(hostComponents[1]);

            return builder.Uri;
        }

    }
}