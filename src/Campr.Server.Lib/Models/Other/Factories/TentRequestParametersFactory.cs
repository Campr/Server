using System.Collections.Generic;
using System.Linq;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Models.Other.Factories
{
    class TentRequestParametersFactory : ITentRequestParametersFactory
    {
        public TentRequestParametersFactory(IUriHelpers uriHelpers, 
            ICryptoHelpers cryptoHelpers, 
            ITentRequestPostFactory requestPostFactory, 
            ITentRequestDateFactory requestDateFactory, 
            ITentPostTypeFactory tentPostTypeFactory, 
            ITentServConfiguration configuration, 
            IUnityContainer container)
        {
            Ensure.Argument.IsNotNull(uriHelpers, "uriHelpers");
            Ensure.Argument.IsNotNull(cryptoHelpers, "cryptoHelpers");
            Ensure.Argument.IsNotNull(requestPostFactory, "requestPostFactory");
            Ensure.Argument.IsNotNull(requestDateFactory, "requestDateFactory");
            Ensure.Argument.IsNotNull(tentPostTypeFactory, "tentPostTypeFactory");
            Ensure.Argument.IsNotNull(configuration, "configuration");
            Ensure.Argument.IsNotNull(container, "container");

            this.uriHelpers = uriHelpers;
            this.cryptoHelpers = cryptoHelpers;
            this.requestPostFactory = requestPostFactory;
            this.requestDateFactory = requestDateFactory;
            this.tentPostTypeFactory = tentPostTypeFactory;
            this.configuration = configuration;
            this.container = container;
        }

        private readonly IUriHelpers uriHelpers;
        private readonly ICryptoHelpers cryptoHelpers;
        private readonly ITentRequestPostFactory requestPostFactory;
        private readonly ITentRequestDateFactory requestDateFactory;
        private readonly ITentPostTypeFactory tentPostTypeFactory;
        private readonly ITentServConfiguration configuration;
        private readonly IUnityContainer container;

        public ITentRequestParameters FromQueryString(IReadOnlyDictionary<string, IList<IList<string>>> queryString, CacheControlValue cacheControl)
        {
            var result = this.container.Resolve<TentRequestParameters>();

            // Cache Control.
            result.CacheControl = cacheControl;

            // Entities.
            if (queryString.ContainsKey("entities"))
            {
                if (this.ReadSingle(queryString["entities"]) == "followings")
                {
                    result.OnlyFromFollowings = true;
                }
                else
                {
                    result.Entities = queryString["entities"].First().Select(this.uriHelpers.UrlDecode);
                }
            }

            // Version.
            if (queryString.ContainsKey("version"))
            {
                result.VersionId = queryString["version"].First().First();
            }

            // Mentions.
            if (queryString.ContainsKey("mentions"))
            {
                result.Mentioning = this.ReadPostIntersection(queryString["mentions"]);
            }

            // Negative mentions.
            if (queryString.ContainsKey("-mentions"))
            {
                result.NotMentioning = this.ReadPostIntersection(queryString["-mentions"]);
            }

            // Limit.
            int limit;
            if (queryString.ContainsKey("limit")
                && int.TryParse(this.ReadSingle(queryString["limit"]), out limit))
            {
                result.RequestLimit = limit > this.configuration.MaxPostLimit()
                    ? this.configuration.MaxPostLimit()
                    : limit;
            }
            else
            {
                result.RequestLimit = this.configuration.DefaultPostLimit();
            }

            result.Limit = result.RequestLimit + 1;

            // Skip.
            int skip;
            if (queryString.ContainsKey("skip")
                && int.TryParse(this.ReadSingle(queryString["skip"]), out skip))
            {
                result.Skip = skip;
            }

            // Bewit.
            if (queryString.ContainsKey("bewit"))
            {
                result.Bewit = this.ReadSingle(queryString["bewit"]);
            }

            // Max refs.
            int maxRefs;
            if (queryString.ContainsKey("max_refs")
                && int.TryParse(this.ReadSingle(queryString["max_refs"]), out maxRefs))
            {
                result.MaxRefs = maxRefs;
            }

            // Profiles.
            result.Profiles = RequestProfilesEnum.None;
            IEnumerable<string> profileValues;

            if (queryString.ContainsKey("profiles")
                && (profileValues = queryString["profiles"].FirstOrDefault()) != null)
            {
                if (profileValues.Contains("entity"))
                {
                    result.Profiles |= RequestProfilesEnum.Entity;
                }

                if (profileValues.Contains("refs"))
                {
                    result.Profiles |= RequestProfilesEnum.Refs;
                }

                if (profileValues.Contains("mentions"))
                {
                    result.Profiles |= RequestProfilesEnum.Mentions;
                }

                if (profileValues.Contains("permissions"))
                {
                    result.Profiles |= RequestProfilesEnum.Permissions;
                }

                if (profileValues.Contains("parents"))
                {
                    result.Profiles |= RequestProfilesEnum.Parents;
                }
            }

            // Time range parameters.
            if (queryString.ContainsKey("since"))
            {
                result.Since = this.requestDateFactory.FromString(this.ReadSingle(queryString["since"]));
            }
            else if (queryString.ContainsKey("since_post"))
            {
                result.Since = this.requestDateFactory.FromString(this.ReadSingle(queryString["since_post"]));
            }
            else if (queryString.ContainsKey("until"))
            {
                result.Until = this.requestDateFactory.FromString(this.ReadSingle(queryString["until"]));
            }
            else if (queryString.ContainsKey("until_post"))
            {
                result.Until = this.requestDateFactory.FromString(this.ReadSingle(queryString["until_post"]));
            }

            if (queryString.ContainsKey("before"))
            {
                result.Before = this.requestDateFactory.FromString(this.ReadSingle(queryString["before"]));
            }
            else if (queryString.ContainsKey("before_post"))
            {
                result.Before = this.requestDateFactory.FromString(this.ReadSingle(queryString["before_post"]));
            }

            // Sort by.
            if (queryString.ContainsKey("sort_by"))
            {
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
            }

            // Post types.
            if (queryString.ContainsKey("types"))
            {
                result.PostTypes = queryString["types"].First()
                    .Select(this.uriHelpers.UrlDecode)
                    .Where(this.uriHelpers.IsValidUri)
                    .Select(u => this.tentPostTypeFactory.FromString(u));
            }

            // External query.
            if (queryString.ContainsKey("query"))
            {
                // This is an encrypted parameter, we need to decrypt it.
                var key = this.configuration.EncryptionKey();
                result.ExternalQuery = this.cryptoHelpers.DecryptString(this.ReadSingle(queryString["query"]), key);
            }

            return result;
        }

        private string ReadSingle(IList<IList<string>> src)
        {
            return src.Count == 0 
                ? null 
                : src.First().FirstOrDefault();
        }

        private IEnumerable<IEnumerable<ITentRequestPost>> ReadPostIntersection(IEnumerable<IList<string>> src)
        {
            return src.Select(i => i.Select(u => this.requestPostFactory.FromString(u)).ToList()).ToList();
        }
    }
}