using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Helpers
{
    class UriHelpers : IUriHelpers
    {
        public UriHelpers(ITentServConfiguration configuration)
        {
            Ensure.Argument.IsNotNull(configuration, "configuration");
            this.configuration = configuration;
        }

        private readonly Regex isHandleRegex = new Regex("^[0-9a-z-]{3,30}\\z");
        private readonly Regex isHandlePathRegex = new Regex("^[0-9a-z-]{3,30}");
        private readonly Regex isInternalEntityRegex = new Regex("^https://([0-9a-z-]{3,30}).campr.me/?\\z");
        private readonly Regex isCamprUserDomainRegex = new Regex("^([0-9a-z-]{3,30}).campr.me\\z");

        private readonly ITentServConfiguration configuration;

        public string UrlEncode(string src)
        {
            return string.IsNullOrWhiteSpace(src) ? null : WebUtility.UrlEncode(src);
        }

        public string UrlDecode(string url)
        {
            return WebUtility.UrlDecode(url);
        }

        public string UrlTokenEncode(byte[] src)
        {
            // Convert to Base64.
            var base64 = Convert.ToBase64String(src);
            
            // Modify for Url and return.
            return base64
                .Replace("=", string.Empty)
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public byte[] UrlTokenDecode(string url)
        {
            // Replace characters.
            url = url
                .Replace('-', '+')
                .Replace('_', '/');

            // Fix padding.
            url = url.PadRight(url.Length + (4 - url.Length % 4) % 4, '=');

            // Convert back.
            return Convert.FromBase64String(url);
        }

        public Uri RemoveUriQuery(Uri src)
        {
            var uriBuilder = new UriBuilder(src)
            {
                Query = string.Empty
            };

            return uriBuilder.Uri;
        }

        public bool IsCamprHandle(string handle)
        {
            return this.isHandleRegex.IsMatch(handle);
        }

        public bool IsCamprEntity(string entity, out string handle)
        {
            var match = this.isInternalEntityRegex.Match(entity);
            if (!match.Success || match.Groups.Count < 2)
            {
                handle = null;
                return false;
            }

            handle = match.Groups[1].Value;

            return true;
        }

        public bool IsCamprDomain(string domain, out string handle)
        {
            var match = this.isCamprUserDomainRegex.Match(domain);
            if (!match.Success || match.Groups.Count < 2)
            {
                handle = null;
                return false;
            }

            handle = match.Groups[1].Value;

            return true;
        }

        public bool IsValidUri(string src)
        {
            Uri uri;
            return Uri.TryCreate(src, UriKind.Absolute, out uri);
        }

        public string ExtractUsernameFromPath(string path)
        {
            // Validate input.
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            // Try to extract a username using the corresponding Regex.
            var match = this.isHandlePathRegex.Match(path);
            if (!match.Success)
            {
                return null;
            }

            return match.Groups[0].Value;
        }

        public string GetStandardEntity(string entity)
        {
            while (entity.Any() && entity.Last() == '/')
            {
                entity = entity.Remove(entity.Count() - 1);
            }

            return entity;
        }

        public string GetCamprTentEntity(string userHandle)
        {
            return string.Format(this.configuration.CamprEntityBaseUrl(), userHandle);
        }

        public Uri GetCamprPostUri(string userHandle, string postId)
        {
            return new Uri(this.configuration.CamprBaseUrl() + this.GetCamprPostPath(userHandle, postId), UriKind.Absolute);
        }

        public Uri GetCamprPostBewitUri(string userHandle, string postId, string bewit)
        {
            var builder = new UriBuilder(this.GetCamprPostUri(userHandle, postId))
            {
                Query = "bewit=" + bewit
            };

            return builder.Uri;
        }

        public Uri GetCamprAttachmentUri(string userHandle, string entity, string digest)
        {
            return new Uri(this.configuration.CamprBaseUrl() + string.Format("/{0}/attachments/{1}/{2}", userHandle.ToLowerInvariant(), this.UrlEncode(entity), digest));
        }

        public string GetCamprPostPath(string userHandle, string postId)
        {
            return string.Format(CultureInfo.InvariantCulture, "/{0}/posts/{1}/{2}", userHandle, this.UrlEncode(this.GetCamprTentEntity(userHandle)), postId);
        }

        public Uri GetCamprUriFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = string.Empty;
            }

            if (path.Length > 0 && path[0] != '/')
            {
                path = '/' + path;
            }

            return new Uri(this.configuration.CamprBaseUrl() + path);
        }

        public Uri GetCamprUriFromUri(Uri uri)
        {
            Ensure.Argument.IsNotNull(uri, "uri");

            var uriBuilder = new UriBuilder(uri)
            {
                Host = this.configuration.CamprBaseDomain(),
                Port = 443
            };

            return uriBuilder.Uri;
        }
    }
}