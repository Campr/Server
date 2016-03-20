using System;
using System.Collections.Generic;
using System.Linq;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other.Factories;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Other
{
    class TentFeedRequest<T> : ITentFeedRequest<T>
    {
        #region Constructors & Private fields.

        public TentFeedRequest(
            ITentRequestPostFactory requestPostFactory,
            ITentRequestDateFactory requestDateFactory)
        {
            Ensure.Argument.IsNotNull(requestPostFactory, "requestPostFactory");
            Ensure.Argument.IsNotNull(requestDateFactory, "requestDateFactory");
            
            this.requestPostFactory = requestPostFactory;
            this.requestDateFactory = requestDateFactory;
        }
        
        private readonly ITentRequestPostFactory requestPostFactory;
        private readonly ITentRequestDateFactory requestDateFactory;
        private readonly IGeneralConfiguration configuration;

        private List<ITentPostType> types;
        private List<string> entities; 
        private List<ITentRequestPost[]> mentions;
        private List<ITentRequestPost[]> notMentions;
        private TentFeedRequestSpecialEntities specialEntities;
        private TentFeedRequestProfiles profiles;
        private IDictionary<TentFeedRequestBoundaryType, ITentRequestDate> dateBoundaries;
        private IDictionary<TentFeedRequestBoundaryType, ITentRequestPost> postBoundaries; 
        private TentFeedRequestSort sortBy;
        private uint? limit;
        private uint? skip;
        private uint? maxRefs;

        #endregion

        #region Interface implementation.
        
        public ITentFeedRequest<T> AddTypes(params ITentPostType[] newTypes)
        {
            (this.types ?? (this.types = new List<ITentPostType>())).AddRange(newTypes);
            return this;
        }

        public ITentFeedRequest<T> AddEntities(params string[] newEntities)
        {
            Ensure.Argument.IsNotNull(newEntities, nameof(newEntities));
            (this.entities ?? (this.entities = new List<string>())).AddRange(newEntities);
            return this;
        }

        public ITentFeedRequest<T> AddSpecialEntities(TentFeedRequestSpecialEntities newSpecialEntities)
        {
            this.specialEntities |= newSpecialEntities;
            return this;
        }

        public ITentFeedRequest<T> AddMentions(params string[] mentionedEntities)
        {
            Ensure.Argument.IsNotNull(mentionedEntities, nameof(mentionedEntities));
            if (mentionedEntities.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(mentionedEntities), "The list of mentioned entities is empty.");

            (this.mentions ?? (this.mentions = new List<ITentRequestPost[]>())).Add(mentionedEntities.Select(this.requestPostFactory.FromString).ToArray());
            return this;
        }

        public ITentFeedRequest<T> AddMentions(params ITentRequestPost[] mentionedPosts)
        {
            Ensure.Argument.IsNotNull(mentionedPosts, nameof(mentionedPosts));
            if (mentionedPosts.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(mentionedPosts), "The list of mentioned posts is empty.");

            (this.mentions ?? (this.mentions = new List<ITentRequestPost[]>())).Add(mentionedPosts);
            return this;
        }

        public ITentFeedRequest<T> AddNotMentions(params string[] mentionedEntities)
        {
            Ensure.Argument.IsNotNull(mentionedEntities, nameof(mentionedEntities));
            if (mentionedEntities.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(mentionedEntities), "The list of mentioned entities is empty.");

            (this.notMentions ?? (this.notMentions = new List<ITentRequestPost[]>())).Add(mentionedEntities.Select(this.requestPostFactory.FromString).ToArray());
            return this;
        }

        public ITentFeedRequest<T> AddNotMentions(params ITentRequestPost[] mentionedPosts)
        {
            Ensure.Argument.IsNotNull(mentionedPosts, nameof(mentionedPosts));
            if (mentionedPosts.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(mentionedPosts), "The list of mentioned posts is empty.");

            (this.notMentions ?? (this.notMentions = new List<ITentRequestPost[]>())).Add(mentionedPosts);
            return this;
        }

        public ITentFeedRequest<T> AddProfiles(TentFeedRequestProfiles newProfiles)
        {
            this.profiles |= newProfiles;
            return this;
        }

        public ITentFeedRequest<T> AddLimit(uint newLimit)
        {
            if (newLimit == 0)
                throw new ArgumentOutOfRangeException(nameof(newLimit), "The limit can't be 0.");

            this.limit = newLimit;
            return this;
        }

        public ITentFeedRequest<T> AddSkip(uint newSkip)
        {
            this.skip = newSkip;
            return this;
        }

        public ITentFeedRequest<T> AddMaxRefs(uint newMaxRefs)
        { 
            this.maxRefs = newMaxRefs;
            return this;
        }

        public ITentFeedRequest<T> AddBoundary(ITentRequestDate boundaryDate, TentFeedRequestBoundaryType boundaryType)
        {
            Ensure.Argument.IsNotNull(boundaryDate, nameof(boundaryDate));
            (this.dateBoundaries ?? (this.dateBoundaries = new Dictionary<TentFeedRequestBoundaryType, ITentRequestDate>()))[boundaryType] = boundaryDate;
            return this;
        }

        public ITentFeedRequest<T> AddPostBoundary(ITentRequestPost boundaryPost, TentFeedRequestBoundaryType boundaryType)
        {
            Ensure.Argument.IsNotNull(boundaryPost, nameof(boundaryPost));
            (this.postBoundaries ?? (this.postBoundaries = new Dictionary<TentFeedRequestBoundaryType, ITentRequestPost>()))[boundaryType] = boundaryPost;
            return this;
        }

        public ITentFeedRequest<T> AddPostBoundary(TentPost boundaryPost, TentFeedRequestBoundaryType boundaryType)
        {
            Ensure.Argument.IsNotNull(boundaryPost, nameof(boundaryPost));
            (this.postBoundaries ?? (this.postBoundaries = new Dictionary<TentFeedRequestBoundaryType, ITentRequestPost>()))[boundaryType] = this.requestPostFactory.FromPost(boundaryPost);
            return this;
        }

        public ITentFeedRequest<T> SortBy(TentFeedRequestSort newSortBy)
        {
            this.sortBy = newSortBy;
            return this;
        }

        public Uri AsUri(string parameter = null)
        {
            throw new NotImplementedException();
        }

        public ITentHawkSignature AsCredentials()
        {
            throw new NotImplementedException();
        }

        public uint? AsLimit()
        {
            return this.limit;
        }

        #endregion

        #region Private methods.

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
            if (this.dateBoundaries != null && this.dateBoundaries.Any())
                foreach (var boundary in this.dateBoundaries)
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
            else if (this.postBoundaries != null && this.postBoundaries.Any())
                foreach (var boundary in this.postBoundaries)
                {
                    // Get the query string key for this boundary.
                    string boundaryKey;
                    switch (boundary.Key)
                    {
                        case TentFeedRequestBoundaryType.Since:
                            boundaryKey = "since_post";
                            break;

                        case TentFeedRequestBoundaryType.Until:
                            boundaryKey = "until_post";
                            break;

                        default:
                            boundaryKey = "before_post";
                            break;
                    }

                    // Create the RequestDate object.
                    var requestDate = this.requestDateFactory.FromPost(boundary.Value, this.sortBy);

                    // Add it to the dictionary.
                    result.Add(boundaryKey, requestDate.ToString());
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