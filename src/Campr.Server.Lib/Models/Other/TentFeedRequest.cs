using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Logic;
using Campr.Server.Lib.Models.Db;
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
            this.resolveDependenciesRunner = new TaskRunner(this.ResolveDependenciesAsync);
        }

        private readonly IUserLogic userLogic;
        private readonly ITentRequestPostFactory requestPostFactory;
        private readonly ITentRequestDateFactory requestDateFactory;
        private readonly TaskRunner resolveDependenciesRunner;

        private List<ITentPostType> types;
        private List<User> users; 
        private List<ITentRequestPost[]> mentions;
        private List<ITentRequestPost[]> notMentions;
        private TentFeedRequestSpecialEntities specialEntities;
        private TentFeedRequestProfiles profiles;
        private IDictionary<TentFeedRequestBoundaryType, ITentRequestDate> boundaries;
        private TentFeedRequestSort sortBy;
        private uint? limit;
        private uint? skip;
        private uint? maxRefs;

        private List<string> temporaryEntities;
        private IDictionary<TentFeedRequestBoundaryType, ITentRequestPost> temporaryBoundaryPosts;

        #endregion

        #region Interface implementation.
        
        public ITentFeedRequest AddTypes(params ITentPostType[] newTypes)
        {
            Ensure.Argument.IsNotNull(newTypes, nameof(newTypes));
            (this.types ?? (this.types = new List<ITentPostType>())).AddRange(newTypes);
            return this;
        }

        public ITentFeedRequest AddUsers(params User[] newUsers)
        {
            Ensure.Argument.IsNotNull(newUsers, nameof(newUsers));
            (this.users ?? (this.users = new List<User>())).AddRange(newUsers);
            return this;
        }

        public ITentFeedRequest AddEntities(params string[] newEntities)
        {
            Ensure.Argument.IsNotNull(newEntities, nameof(newEntities));
            (this.temporaryEntities ?? (this.temporaryEntities = new List<string>())).AddRange(newEntities);
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
            (this.mentions ?? (this.mentions = new List<ITentRequestPost[]>())).Add(mentionedEntities.Select(this.requestPostFactory.FromString).ToArray());
            return this;
        }

        public ITentFeedRequest AddMentions(params ITentRequestPost[] mentionedPosts)
        {
            Ensure.Argument.IsNotNull(mentionedPosts, nameof(mentionedPosts));
            (this.mentions ?? (this.mentions = new List<ITentRequestPost[]>())).Add(mentionedPosts);
            return this;
        }

        public ITentFeedRequest AddNotMentions(params string[] notMentionedEntities)
        {
            Ensure.Argument.IsNotNull(notMentionedEntities, nameof(notMentionedEntities));
            (this.notMentions ?? (this.notMentions = new List<ITentRequestPost[]>())).Add(notMentionedEntities.Select(this.requestPostFactory.FromString).ToArray());
            return this;
        }

        public ITentFeedRequest AddNotMentions(params ITentRequestPost[] notMentionedPosts)
        {
            Ensure.Argument.IsNotNull(notMentionedPosts, nameof(notMentionedPosts));
            (this.notMentions ?? (this.notMentions = new List<ITentRequestPost[]>())).Add(notMentionedPosts);
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
            (this.temporaryBoundaryPosts ?? (this.temporaryBoundaryPosts = new Dictionary<TentFeedRequestBoundaryType, ITentRequestPost>()))[boundaryType] = boundaryPost;
            return this;
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

        public async Task<Uri> AsUriAsync(string parameter = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Make sure we have all the data we need.
            await this.resolveDependenciesRunner.RunOnce(cancellationToken);

            // TODO.
            throw new NotImplementedException();
        }

        public async Task<ReqlExpr> AsCountTableQueryAsync(RethinkDB rdb, Table table, string ownerId, CancellationToken cancellationToken = new CancellationToken())
        {
            // Make sure we have all the data we need.
            await this.resolveDependenciesRunner.RunOnce(cancellationToken);

            // Find the name of the index to use.
            var index = this.TableIndex();

            // Find the date boundary values.
            var lowerDateBound = this.boundaries?.TryGetValue(TentFeedRequestBoundaryType.Since)?.Date
                ?? this.boundaries?.TryGetValue(TentFeedRequestBoundaryType.Until)?.Date;
            var upperDateBound = this.boundaries?.TryGetValue(TentFeedRequestBoundaryType.Before)?.Date;

            // Filter by owner and date.
            var query = (ReqlExpr)table.Between(
                new object[] { ownerId, lowerDateBound != null ? rdb.Expr(lowerDateBound) : rdb.Minval() },
                new object[] { ownerId, upperDateBound != null ? rdb.Expr(upperDateBound) : rdb.Maxval() })[new
                {
                    index,
                    left_bound = "open",
                    right_bound = "open"
                }];

            // Set the order-by depending on the boundary type.
            query = query.OrderBy()[new { index = this.boundaries != null && this.boundaries.ContainsKey(TentFeedRequestBoundaryType.Since) ? (object)rdb.Asc(index) : (object)rdb.Desc(index) }];

            var filters = new List<Func<ReqlExpr, ReqlExpr>>();

            // Entities.
            if (this.users != null && this.users.Any())
                filters.Add(r => r.BetterOr(this.users.Select(u => r.G("user").Eq(u.Id)).Cast<object>().ToArray()));

            // Post type filter.
            if (this.types != null && this.types.Any())
            {
                // Condition on a single type.
                var typeCondition = new Func<ReqlExpr, ITentPostType, ReqlExpr>((r, type) => type.WildCard
                    ? (ReqlExpr)r.G("type").Match($"^{Regex.Escape(type.Type)}#")
                    : r.G("type").Eq(type.ToString()));

                // Combine the type conditions as part of an OR expression.
                filters.Add(r => r.BetterOr(this.types.Select(type => typeCondition(r, type)).Cast<object>().ToArray()));
            }

            // Condition on a single mention.
            var mentionCondition = new Func<ReqlExpr, ITentRequestPost, ReqlExpr>((r, mention) => r.G("mentions")
                // Map each mention for the current row to a boolean.
                .Map(m => mention.Post == null
                    ? m.G("user").Eq(mention.User.Id)
                    : m.BetterAnd(m.G("user").Eq(mention.User.Id), m.G("post").Eq(mention.Post.Id)))
                // Reduce the resulting booleans to just one (any).
                .Reduce((b1, b2) => r.BetterOr(b1, b2))
                .Default_(false));

            // Mentions.
            if (this.mentions != null && this.mentions.Any())
            {
                // Combine the mention conditions, first by AND, then by OR.
                filters.Add(r => r.BetterAnd(this.mentions
                    .Select(andMentions =>
                        r.BetterOr(andMentions.Select(mention =>
                            mentionCondition(r, mention)).Cast<object>().ToArray()))
                    .Cast<object>().ToArray()));
            }

            // Not mentions.
            if (this.notMentions != null && this.notMentions.Any())
            {
                // Combine the not mention conditions, first by AND, then by OR.
                filters.Add(r => r.BetterAnd(this.notMentions
                    .Select(andNotMentions =>
                        r.BetterOr(andNotMentions.Select(notMention =>
                            mentionCondition(r, notMention).Not()).Cast<object>().ToArray()))
                    .Cast<object>().ToArray()));
            }

            // Apply all the filters as part of an AND expression.
            if (filters.Any())
                query = query.Filter(r => r.BetterAnd(filters.Select(f => f(r)).Cast<object>().ToArray()));

            return query;
        }

        public async Task<ReqlExpr> AsTableQueryAsync(RethinkDB rdb, Table table, string ownerId, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Build the base query.
            var query = await this.AsCountTableQueryAsync(rdb, table, ownerId, cancellationToken);

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

        private async Task ResolveDependenciesAsync(CancellationToken cancellationToken)
        {
            // Entities -> Users.
            var resolveEntitiesTasks = this.temporaryEntities?.Select(e => this.userLogic.GetUserAsync(e, cancellationToken)).ToList();

            // Resolve mentions and not mentions posts and users.
            var resolveMentions = this.mentions?.SelectMany(ms => ms.Select(m => m.Resolve(cancellationToken))).ToList();
            var resolveNotMentions = this.notMentions?.SelectMany(ms => ms.Select(m => m.Resolve(cancellationToken))).ToList();

            // Resolve boundary posts.
            var resolveBoundaryPosts = this.temporaryBoundaryPosts?.Values.Select(bp => bp.Resolve(cancellationToken)).ToList();

            // Wait for tasks to complete.
            var resolveTasks = new List<IEnumerable<Task>>
            {
                resolveEntitiesTasks,
                resolveMentions,
                resolveNotMentions,
                resolveBoundaryPosts
            };

            await Task.WhenAll(resolveTasks.Compact().SelectMany(l => l));

            // Assign new users.
            if (resolveEntitiesTasks != null)
                (this.users ?? (this.users = new List<User>())).AddRange(resolveEntitiesTasks.Where(t => t.Result != null).Select(t => t.Result));

            // Assign new boundary dates.
            this.temporaryBoundaryPosts?
                .ToDictionary(kv => kv.Key, kv => this.requestDateFactory.FromPost(kv.Value, this.sortBy))
                .Where(kv => kv.Value != null)
                .ForEach(kv => (this.boundaries ?? (this.boundaries = new Dictionary<TentFeedRequestBoundaryType, ITentRequestDate>()))[kv.Key] = kv.Value);
        }

        private string TableIndex()
        {
            switch (this.sortBy)
            {
                case TentFeedRequestSort.VersionPublishedAt:
                    return "owner_versionpublishedat";

                case TentFeedRequestSort.VersionReceivedAt:
                    return "owner_versionreceivedat";

                case TentFeedRequestSort.PublishedAt:
                    return "owner_publishedat";

                default:
                    return "owner_receivedat";
            }
        }

        private IEnumerable<KeyValuePair<string, object>> ToDictionary()
        {
            var result = new Dictionary<string, object>();

            // Types.
            if (this.types != null && this.types.Any())
                result.Add("types", new List<IEnumerable<string>> { this.types.Select(t => t.ToString()) });

            // Entities.
            if (this.users != null && this.users.Any())
                result.Add("entities", this.users.Select(u => u.Entity));
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