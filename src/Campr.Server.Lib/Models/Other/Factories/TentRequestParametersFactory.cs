using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Logic;

namespace Campr.Server.Lib.Models.Other.Factories
{
    class TentRequestParametersFactory : ITentRequestParametersFactory
    {
        public TentRequestParametersFactory(
            IUserLogic userLogic,
            IUriHelpers uriHelpers, 
            ICryptoHelpers cryptoHelpers,
            IQueryStringHelpers queryStringHelpers,
            ITentRequestPostFactory requestPostFactory, 
            ITentRequestDateFactory requestDateFactory, 
            ITentPostTypeFactory postTypeFactory, 
            IGeneralConfiguration configuration)
        {
            Ensure.Argument.IsNotNull(userLogic, nameof(userLogic));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            Ensure.Argument.IsNotNull(cryptoHelpers, nameof(cryptoHelpers));
            Ensure.Argument.IsNotNull(queryStringHelpers, nameof(queryStringHelpers));
            Ensure.Argument.IsNotNull(requestPostFactory, nameof(requestPostFactory));
            Ensure.Argument.IsNotNull(requestDateFactory, nameof(requestDateFactory));
            Ensure.Argument.IsNotNull(postTypeFactory, nameof(postTypeFactory));
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));

            this.userLogic = userLogic;
            this.uriHelpers = uriHelpers;
            this.cryptoHelpers = cryptoHelpers;
            this.queryStringHelpers = queryStringHelpers;
            this.requestPostFactory = requestPostFactory;
            this.requestDateFactory = requestDateFactory;
            this.postTypeFactory = postTypeFactory;
            this.configuration = configuration;
        }

        private readonly IUserLogic userLogic;
        private readonly IUriHelpers uriHelpers;
        private readonly ICryptoHelpers cryptoHelpers;
        private readonly IQueryStringHelpers queryStringHelpers;
        private readonly ITentRequestPostFactory requestPostFactory;
        private readonly ITentRequestDateFactory requestDateFactory;
        private readonly ITentPostTypeFactory postTypeFactory;
        private readonly IGeneralConfiguration configuration;

        public ITentRequestParameters FromQueryString(IReadOnlyDictionary<string, IList<IList<string>>> queryString, CacheControlValue cacheControl)
        {
            // Create the resulting object.
            var result = new TentRequestParameters(this.userLogic, this.queryStringHelpers, this.requestDateFactory)
            {
                CacheControl = cacheControl
            };

            // Entities.
            var entities = queryString.TryGetValue("entities");
            if (this.ReadSingle(entities) == "followings")
                result.OnlyFromFollowings = true;
            else
                result.Entities = entities?.FirstOrDefault()?.Select(this.uriHelpers.UrlDecode).ToList();

            // Version.
            result.VersionId = queryString.TryGetValue("version")?.FirstOrDefault()?.FirstOrDefault();

            // Mentions.
            result.Mentioning = this.ReadPostIntersection(queryString.TryGetValue("mentions"));

            // Negative mentions.
            result.NotMentioning = this.ReadPostIntersection(queryString.TryGetValue("-mentions"));

            // Limit.
            var limit = this.ReadSingle(queryString.TryGetValue("limit"))?.TryParseInt() ?? this.configuration.DefaultPostLimit;
            result.RequestLimit = limit > this.configuration.MaxPostLimit ? this.configuration.MaxPostLimit : limit;
            result.Limit = result.RequestLimit + 1;

            // Skip.
            result.Skip = this.ReadSingle(queryString.TryGetValue("skip"))?.TryParseInt();

            // Bewit.
            result.Bewit = this.ReadSingle(queryString.TryGetValue("bewit"));

            // Max refs.
            result.MaxRefs = this.ReadSingle(queryString.TryGetValue("max_refs"))?.TryParseInt();

            // Profiles.
            result.Profiles = RequestProfilesEnum.None;

            var profileValues = queryString.TryGetValue("profiles")?.FirstOrDefault();
            if (profileValues != null)
            {
                if (profileValues.Contains("entity"))
                    result.Profiles |= RequestProfilesEnum.Entity;

                if (profileValues.Contains("refs"))
                    result.Profiles |= RequestProfilesEnum.Refs;

                if (profileValues.Contains("mentions"))
                    result.Profiles |= RequestProfilesEnum.Mentions;

                if (profileValues.Contains("permissions"))
                    result.Profiles |= RequestProfilesEnum.Permissions;

                if (profileValues.Contains("parents"))
                    result.Profiles |= RequestProfilesEnum.Parents;
            }

            // Time range parameters.
            if (queryString.ContainsKey("since"))
                result.Since = this.requestDateFactory.FromString(this.ReadSingle(queryString["since"]));
            else if (queryString.ContainsKey("since_post"))
                result.Since = this.requestDateFactory.FromString(this.ReadSingle(queryString["since_post"]));
            else if (queryString.ContainsKey("until"))
                result.Until = this.requestDateFactory.FromString(this.ReadSingle(queryString["until"]));
            else if (queryString.ContainsKey("until_post"))
                result.Until = this.requestDateFactory.FromString(this.ReadSingle(queryString["until_post"]));

            if (queryString.ContainsKey("before"))
                result.Before = this.requestDateFactory.FromString(this.ReadSingle(queryString["before"]));
            else if (queryString.ContainsKey("before_post"))
                result.Before = this.requestDateFactory.FromString(this.ReadSingle(queryString["before_post"]));

            // Sort by.
            if (queryString.ContainsKey("sort_by"))
                switch (this.ReadSingle(queryString["sort_by"]))
                {
                    case "published_at":
                        result.SortBy = RequestSortByEnum.PublishedDate;
                        break;
                    case "version.published_at":
                        result.SortBy = RequestSortByEnum.VersionPublishedDate;
                        break;
                    case "version.received_at":
                        result.SortBy = RequestSortByEnum.VersionReceivedDate;
                        break;
                    default:
                        result.SortBy = RequestSortByEnum.ReceivedDate;
                        break;
                }

            // Post types.
            result.PostTypes = queryString.TryGetValue("types")?
                .FirstOrDefault()?
                .Select(this.uriHelpers.UrlDecode)
                .Where(this.uriHelpers.IsValidUri)
                .Select(u => this.postTypeFactory.FromString(u))
                .ToList();

            // External query.
            var externalQuery = this.ReadSingle(queryString.TryGetValue("query"));
            if (!string.IsNullOrWhiteSpace(externalQuery))
            {
                // This is an encrypted parameter, we need to decrypt it.
                var key = this.configuration.EncryptionKey;
                result.ExternalQuery = this.cryptoHelpers.DecryptString(externalQuery, key);
            }

            return result;
        }

        private string ReadSingle(IList<IList<string>> src)
        {
            return src == null || src.Count == 0 ? null : src.First().FirstOrDefault();
        }

        private IList<IList<ITentRequestPost>> ReadPostIntersection(IEnumerable<IList<string>> src)
        {
            return src?.Select(i => (IList<ITentRequestPost>)i.Select(u => this.requestPostFactory.FromString(u)).ToList()).ToList();
        }
    }
}