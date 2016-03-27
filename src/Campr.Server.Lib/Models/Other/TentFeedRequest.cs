using System;
using System.Collections.Generic;
using System.Linq;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other.Factories;
using Campr.Server.Lib.Models.Tent;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;

namespace Campr.Server.Lib.Models.Other
{
    class TentFeedRequest : ITentFeedRequest
    {
        #region Constructors & Private fields.

        public TentFeedRequest(
            ITentRequestPostFactory requestPostFactory,
            ITentRequestDateFactory requestDateFactory)
        {
            Ensure.Argument.IsNotNull(requestPostFactory, nameof(requestPostFactory));
            Ensure.Argument.IsNotNull(requestDateFactory, nameof(requestDateFactory));
            
            this.requestPostFactory = requestPostFactory;
            this.requestDateFactory = requestDateFactory;
        }
        
        private readonly ITentRequestPostFactory requestPostFactory;
        private readonly ITentRequestDateFactory requestDateFactory;

        private List<ITentPostType> types;
        private List<string> entities; 
        private List<ITentRequestPost[]> mentions;
        private List<ITentRequestPost[]> notMentions;
        private TentFeedRequestSpecialEntities specialEntities;
        private TentFeedRequestProfiles profiles;
        private IDictionary<TentFeedRequestBoundaryType, ITentRequestDate> boundaries;
        private TentFeedRequestSort sortBy;
        private uint? limit;
        private uint? skip;
        private uint? maxRefs;

        #endregion

        #region Interface implementation.
        
        public ITentFeedRequest AddTypes(params ITentPostType[] newTypes)
        {
            (this.types ?? (this.types = new List<ITentPostType>())).AddRange(newTypes);
            return this;
        }

        public ITentFeedRequest AddEntities(params string[] newEntities)
        {
            Ensure.Argument.IsNotNull(newEntities, nameof(newEntities));
            (this.entities ?? (this.entities = new List<string>())).AddRange(newEntities);
            return this;
        }

        public ITentFeedRequest AddSpecialEntities(TentFeedRequestSpecialEntities newSpecialEntities)
        {
            this.specialEntities |= newSpecialEntities;
            return this;
        }

        public ITentFeedRequest AddMentions(params string[] mentionedEntities)
        {
            Ensure.Argument.IsNotNull(mentionedEntities, nameof(mentionedEntities));
            if (mentionedEntities.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(mentionedEntities), "The list of mentioned entities is empty.");

            (this.mentions ?? (this.mentions = new List<ITentRequestPost[]>())).Add(mentionedEntities.Select(this.requestPostFactory.FromString).ToArray());
            return this;
        }

        public ITentFeedRequest AddMentions(params ITentRequestPost[] mentionedPosts)
        {
            Ensure.Argument.IsNotNull(mentionedPosts, nameof(mentionedPosts));
            if (mentionedPosts.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(mentionedPosts), "The list of mentioned posts is empty.");

            (this.mentions ?? (this.mentions = new List<ITentRequestPost[]>())).Add(mentionedPosts);
            return this;
        }

        public ITentFeedRequest AddNotMentions(params string[] mentionedEntities)
        {
            Ensure.Argument.IsNotNull(mentionedEntities, nameof(mentionedEntities));
            if (mentionedEntities.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(mentionedEntities), "The list of mentioned entities is empty.");

            (this.notMentions ?? (this.notMentions = new List<ITentRequestPost[]>())).Add(mentionedEntities.Select(this.requestPostFactory.FromString).ToArray());
            return this;
        }

        public ITentFeedRequest AddNotMentions(params ITentRequestPost[] mentionedPosts)
        {
            Ensure.Argument.IsNotNull(mentionedPosts, nameof(mentionedPosts));
            if (mentionedPosts.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(mentionedPosts), "The list of mentioned posts is empty.");

            (this.notMentions ?? (this.notMentions = new List<ITentRequestPost[]>())).Add(mentionedPosts);
            return this;
        }

        public ITentFeedRequest AddProfiles(TentFeedRequestProfiles newProfiles)
        {
            this.profiles |= newProfiles;
            return this;
        }

        public ITentFeedRequest AddLimit(uint newLimit)
        {
            if (newLimit == 0)
                throw new ArgumentOutOfRangeException(nameof(newLimit), "The limit can't be 0.");

            this.limit = newLimit;
            return this;
        }

        public ITentFeedRequest AddSkip(uint newSkip)
        {
            this.skip = newSkip;
            return this;
        }

        public ITentFeedRequest AddMaxRefs(uint newMaxRefs)
        { 
            this.maxRefs = newMaxRefs;
            return this;
        }

        public ITentFeedRequest AddPostBoundary(TentPost boundaryPost, TentFeedRequestBoundaryType boundaryType)
        {
            Ensure.Argument.IsNotNull(boundaryPost, nameof(boundaryPost));
            var requestPost = this.requestPostFactory.FromPost(boundaryPost);
            return this.AddPostBoundary(requestPost, boundaryType);
        }

        public ITentFeedRequest AddPostBoundary(ITentRequestPost boundaryPost, TentFeedRequestBoundaryType boundaryType)
        {
            Ensure.Argument.IsNotNull(boundaryPost, nameof(boundaryPost));
            var requestDate = this.requestDateFactory.FromPost(boundaryPost);
            return this.AddBoundary(requestDate, boundaryType);
        }

        public ITentFeedRequest AddBoundary(ITentRequestDate boundaryDate, TentFeedRequestBoundaryType boundaryType)
        {
            Ensure.Argument.IsNotNull(boundaryDate, nameof(boundaryDate));
            (this.boundaries ?? (this.boundaries = new Dictionary<TentFeedRequestBoundaryType, ITentRequestDate>()))[boundaryType] = boundaryDate;
            return this;
        }

        public ITentFeedRequest SortBy(TentFeedRequestSort newSortBy)
        {
            this.sortBy = newSortBy;
            return this;
        }

        public ITentHawkSignature AsCredentials()
        {
            throw new NotImplementedException();
        }

        public uint? AsLimit()
        {
            return this.limit;
        }

        public Uri AsUri(string parameter = null)
        {
            throw new NotImplementedException();
        }

        public ReqlExpr AsTableQuery(RethinkDB rdb, Table table, string ownerId)
        {
            // Find the name of the index to use.
            var index = this.TableIndex();

            // Find the date boundary values.
            var lowerDateBound = this.boundaries.TryGetValue(TentFeedRequestBoundaryType.Since)?.Date?.ToUnixTime()
                ?? this.boundaries.TryGetValue(TentFeedRequestBoundaryType.Until)?.Date?.ToUnixTime();
            var upperDateBound = this.boundaries.TryGetValue(TentFeedRequestBoundaryType.Before)?.Date?.ToUnixTime();

            // Filter by owner and date.
            var query = (ReqlExpr)table.Between(
                new object[] { ownerId, (object)lowerDateBound ?? rdb.Minval() },
                new object[] { ownerId, (object)upperDateBound ?? rdb.Maxval() })[new { index }];

            var filters = new List<Func<ReqlExpr, ReqlExpr>>();

            // Entities.


            // Post type filter.
            if (this.types != null && this.types.Any())
            {
                // Condition on a single type.
                var typeCondition = new Func<ReqlExpr, ITentPostType, ReqlExpr>((r, type) => type.WildCard
                    ? (ReqlExpr)r.Match("^" + type.Type)
                    : r.Eq(type.ToString()));

                // Combine the type conditions as part of an OR expression.
                filters.Add(r => r.BetterOr(this.types.Select(type => typeCondition(r, type)).Cast<object>().ToArray()));
            }

            // Condition on a single mention.
            var mentionCondition = new Func<ReqlExpr, ITentRequestPost, ReqlExpr>((r, mention) => r.And(
                r.G("user").Eq(mention.User.Id),
                string.IsNullOrWhiteSpace(mention.PostId) ? (object)true : r.G("post").Eq(mention.PostId)
            ));

            // Mentions.
            if (this.mentions != null && this.mentions.Any())
            {
                // Combine the mention conditions, first by AND, then by OR.
                filters.Add(r => r.And(this.mentions.Select(andMentions => 
                    r.BetterOr(andMentions.Select(mention => 
                        mentionCondition(r, mention)).Cast<object>().ToArray()))));
            }

            // Not mentions.
            if (this.notMentions != null && this.notMentions.Any())
            {
                // Combine the not mention conditions, first by AND, then by OR.
                filters.Add(r => r.And(this.notMentions.Select(andNotMentions =>
                    r.BetterOr(andNotMentions.Select(notMention =>
                        mentionCondition(r, notMention).Not()).Cast<object>().ToArray()))));
            }

            // Permissions.
            filters.Add(r => r.Or(
                r.G("user").Eq(ownerId),
                r.G("permissions").G("public").Eq(true),  
                r.G("permissions").G("users").Contains(ownerId)));

            // Apply all the filters as part of an AND expression.
            query = query.Filter(r => r.And(filters.Select(f => f(r)).Cast<object>().ToArray()));

            // Set the order-by depending on the boundary type.
            query = query.OrderBy(new { index = this.boundaries.ContainsKey(TentFeedRequestBoundaryType.Since) ? rdb.Desc(index) : (object)index });
            
            // Apply the skip.
            if (this.skip.HasValue)
                query = query.Skip(this.skip.Value);

            // Apply the limit.
            if (this.limit.HasValue)
                query = query.Limit(this.limit.Value);

            return query;
        }

        #endregion

        #region Private methods.

        private string TableIndex()
        {
            switch (this.sortBy)
            {
                case TentFeedRequestSort.PublishedAt:
                    return "owner_publishedat";

                case TentFeedRequestSort.ReceivedAt:
                    return "owner_receivedat";

                case TentFeedRequestSort.VersionPublishedAt:
                    return "owner_versionpublishedat";

                default:
                    return "owner_versionreceivedat";
            }
        }

        private IEnumerable<KeyValuePair<string, object>> ToDictionary()
        {
            var result = new Dictionary<string, object>();

            // Types.
            if (this.types != null && this.types.Any())
                result.Add("types", new List<IEnumerable<string>> { this.types.Select(t => t.ToString()) });

            // Entities.
            if (this.entities != null && this.entities.Any())
                result.Add("entities", this.entities);
            else if (this.specialEntities.HasFlag(TentFeedRequestSpecialEntities.Followings))
                result.Add("entities", "followings");
            else if (this.specialEntities.HasFlag(TentFeedRequestSpecialEntities.Followers))
                result.Add("entities", "followers");
            else if (this.specialEntities.HasFlag(TentFeedRequestSpecialEntities.Friends))
                result.Add("entities", "friends");

            // Mentions.
            if (this.mentions != null && this.mentions.Any())
                result.Add("mentions", new [] { this.mentions });

            // Not mentions.
            if (this.notMentions == null && this.notMentions.Any())
                result.Add("-mentions", new [] { this.mentions });

            // Limit.
            if (this.limit.HasValue)
                result.Add("limit", this.limit.Value);

            // Skip.
            if (this.skip.HasValue)
                result.Add("skip", this.skip.Value);

            // Max refs.
            if (this.maxRefs.HasValue)
                result.Add("max_refs", this.maxRefs.Value);

            // Profiles.
            if (this.profiles != TentFeedRequestProfiles.None)
            {
                var profileValues = new List<string>();

                if (this.profiles.HasFlag(TentFeedRequestProfiles.Entity))
                    profileValues.Add("entity");

                if (this.profiles.HasFlag(TentFeedRequestProfiles.Mentions))
                    profileValues.Add("mentions");

                if (this.profiles.HasFlag(TentFeedRequestProfiles.Refs))
                    profileValues.Add("refs");

                if (this.profiles.HasFlag(TentFeedRequestProfiles.Parents))
                    profileValues.Add("parents");

                if (this.profiles.HasFlag(TentFeedRequestProfiles.Permissions))
                    profileValues.Add("permissions");

                result.Add("profiles", new [] { profileValues });
            }

            // Boundaries.
            if (this.boundaries != null && this.boundaries.Any())
                foreach (var boundary in this.boundaries)
                {
                    // Get the query string key for this boundary.
                    string boundaryKey;
                    switch (boundary.Key)
                    {
                        case TentFeedRequestBoundaryType.Since:
                            boundaryKey = "since";
                            break;
                        case TentFeedRequestBoundaryType.Until:
                            boundaryKey = "until";
                            break;
                        default:
                            boundaryKey = "before";
                            break;
                    };

                    // Add it to the dictionary.
                    result.Add(boundaryKey, boundary.Value.ToString());
                }

            // Sort.
            string sortByValue;
            switch (this.sortBy)
            {
                case TentFeedRequestSort.VersionReceivedAt:
                    sortByValue = "version.received_at";
                    break;
                case TentFeedRequestSort.PublishedAt:
                    sortByValue = "published_at";
                    break;
                case TentFeedRequestSort.VersionPublishedAt:
                    sortByValue = "version.published_at";
                    break;
                default:
                    sortByValue = "received_at";
                    break;
            }
            result.Add("sort_by", sortByValue);

            return result;
        }

        #endregion
    }
}