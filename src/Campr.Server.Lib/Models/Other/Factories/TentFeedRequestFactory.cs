using System.Collections.Generic;
using System.Linq;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Models.Other.Factories
{
    class TentFeedRequestFactory : ITentFeedRequestFactory
    {
        public TentFeedRequestFactory(
            IUriHelpers uriHelpers,
            ITentRequestDateFactory requestDateFactory,
            ITentRequestPostFactory requestPostFactory,
            ITentPostTypeFactory postTypeFactory,
            IGeneralConfiguration configuration)
        {
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            Ensure.Argument.IsNotNull(requestPostFactory, nameof(requestPostFactory));
            Ensure.Argument.IsNotNull(requestDateFactory, nameof(requestDateFactory));
            Ensure.Argument.IsNotNull(postTypeFactory, nameof(postTypeFactory));
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));

            this.uriHelpers = uriHelpers;
            this.requestPostFactory = requestPostFactory;
            this.requestDateFactory = requestDateFactory;
            this.postTypeFactory = postTypeFactory;
            this.configuration = configuration;
        }

        private readonly IUriHelpers uriHelpers;
        private readonly ITentRequestPostFactory requestPostFactory;
        private readonly ITentRequestDateFactory requestDateFactory;
        private readonly ITentPostTypeFactory postTypeFactory;
        private readonly IGeneralConfiguration configuration;

        public ITentFeedRequest Make()
        {
            return new TentFeedRequest(
                this.requestPostFactory,
                this.requestDateFactory);
        }

        public ITentFeedRequest FromQueryParameters(IReadOnlyDictionary<string, IList<IList<string>>> queryString)
        {
            var feedRequest = this.Make();

            // Entities.
            var entities = queryString.TryGetValue("entities");
            var entitiesSingle = this.ReadSingle(entities);
            if (entitiesSingle == "followings")
                feedRequest.AddSpecialEntities(TentFeedRequestSpecialEntities.Followings);
            else if (entitiesSingle == "followers")
                feedRequest.AddSpecialEntities(TentFeedRequestSpecialEntities.Followers);
            else if (entitiesSingle == "friends")
                feedRequest.AddSpecialEntities(TentFeedRequestSpecialEntities.Friends);
            else
                feedRequest.AddEntities(entities?.FirstOrDefault()?.Select(this.uriHelpers.UrlDecode).ToArray());

            // Mentions.
            this.ReadPostIntersection(queryString.TryGetValue("mentions"))?.ForEach(m => feedRequest.AddMentions(m.ToArray()));

            // Negative mentions.
            this.ReadPostIntersection(queryString.TryGetValue("-mentions"))?.ForEach(m => feedRequest.AddNotMentions(m.ToArray()));

            // Limit.
            var limit = this.ReadSingle(queryString.TryGetValue("limit"))?.TryParseUInt() ?? this.configuration.DefaultPostLimit;
            feedRequest.AddLimit(limit > this.configuration.MaxPostLimit ? this.configuration.MaxPostLimit : limit);

            // Skip.
            feedRequest.AddSkip(this.ReadSingle(queryString.TryGetValue("skip"))?.TryParseUInt() ?? 0);

            // Max refs.
            feedRequest.AddMaxRefs(this.ReadSingle(queryString.TryGetValue("max_refs"))?.TryParseUInt() ?? 0);

            // Profiles.
            var profileValues = queryString.TryGetValue("profiles")?.FirstOrDefault();
            if (profileValues != null)
            {
                if (profileValues.Contains("entity"))
                    feedRequest.AddProfiles(TentFeedRequestProfiles.Entity);

                if (profileValues.Contains("refs"))
                    feedRequest.AddProfiles(TentFeedRequestProfiles.Refs);

                if (profileValues.Contains("mentions"))
                    feedRequest.AddProfiles(TentFeedRequestProfiles.Mentions);

                if (profileValues.Contains("permissions"))
                    feedRequest.AddProfiles(TentFeedRequestProfiles.Permissions);

                if (profileValues.Contains("parents"))
                    feedRequest.AddProfiles(TentFeedRequestProfiles.Parents);
            }

            // Time range parameters.
            if (queryString.ContainsKey("since"))
                feedRequest.AddBoundary(this.requestDateFactory.FromString(this.ReadSingle(queryString["since"])), TentFeedRequestBoundaryType.Since);
            else if (queryString.ContainsKey("since_post"))
                feedRequest.AddBoundary(this.requestDateFactory.FromString(this.ReadSingle(queryString["since_post"])), TentFeedRequestBoundaryType.Since);
            else if (queryString.ContainsKey("until"))
                feedRequest.AddBoundary(this.requestDateFactory.FromString(this.ReadSingle(queryString["until"])), TentFeedRequestBoundaryType.Until);
            else if (queryString.ContainsKey("until_post"))
                feedRequest.AddBoundary(this.requestDateFactory.FromString(this.ReadSingle(queryString["until_post"])), TentFeedRequestBoundaryType.Until);

            if (queryString.ContainsKey("before"))
                feedRequest.AddBoundary(this.requestDateFactory.FromString(this.ReadSingle(queryString["before"])), TentFeedRequestBoundaryType.Before);
            else if (queryString.ContainsKey("before_post"))
                feedRequest.AddBoundary(this.requestDateFactory.FromString(this.ReadSingle(queryString["before_post"])), TentFeedRequestBoundaryType.Before);

            // Sort by.
            if (queryString.ContainsKey("sort_by"))
                switch (this.ReadSingle(queryString["sort_by"]))
                {
                    case "version.published_at":
                        feedRequest.SortBy(TentFeedRequestSort.VersionPublishedAt);
                        break;
                    case "version.received_at":
                        feedRequest.SortBy(TentFeedRequestSort.VersionReceivedAt);
                        break;
                    case "published_at":
                        feedRequest.SortBy(TentFeedRequestSort.PublishedAt);
                        break;
                    default:
                        feedRequest.SortBy(TentFeedRequestSort.ReceivedAt);
                        break;
                }

            // Post types.
            feedRequest.AddTypes(queryString.TryGetValue("types")?
                .FirstOrDefault()?
                .Select(this.uriHelpers.UrlDecode)
                .Where(this.uriHelpers.IsValidUri)
                .Select(u => this.postTypeFactory.FromString(u))
                .ToArray());

            return feedRequest;
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