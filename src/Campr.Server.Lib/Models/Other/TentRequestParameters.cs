//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Campr.Server.Lib.Enums;
//using Campr.Server.Lib.Helpers;
//using Campr.Server.Lib.Infrastructure;
//using Campr.Server.Lib.Logic;
//using Campr.Server.Lib.Models.Db;
//using Campr.Server.Lib.Models.Other.Factories;
//using Campr.Server.Lib.Models.Tent;

//namespace Campr.Server.Lib.Models.Other
//{
//    public class TentRequestParameters : ITentRequestParameters
//    {
//        #region Constructor & Dependencies.

//        public TentRequestParameters(IUserLogic userLogic, 
//            IQueryStringHelpers queryStringHelpers, 
//            ITentRequestDateFactory requestDateFactory)
//        {
//            Ensure.Argument.IsNotNull(userLogic, nameof(userLogic));
//            Ensure.Argument.IsNotNull(queryStringHelpers, nameof(queryStringHelpers));
//            Ensure.Argument.IsNotNull(requestDateFactory, nameof(requestDateFactory));

//            this.userLogic = userLogic;
//            this.queryStringHelpers = queryStringHelpers;
//            this.requestDateFactory = requestDateFactory;
//        }

//        private readonly IUserLogic userLogic;
//        private readonly IQueryStringHelpers queryStringHelpers;
//        private readonly ITentRequestDateFactory requestDateFactory;

//        #endregion

//        #region Public properties.

//        public string VersionId { get; set; }

//        public ITentRequestDate Since { get; set; }
//        public ITentRequestDate Until { get; set; }
//        public ITentRequestDate Before { get; set; }

//        public int? RequestLimit { get; set; }
//        public int? Limit { get; set; }
//        public int? Skip { get; set; }
//        public int? MaxRefs { get; set; }
//        public RequestProfilesEnum Profiles { get; set; }
//        public CacheControlValue CacheControl { get; set; }

//        public IList<string> Entities { get; set; }
//        public IList<User> Users { get; private set; }

//        public bool OnlyFromFollowings { get; set; }
//        public bool OnlyFromFollowers { get; set; }

//        public string Bewit { get; set; }

//        public string ExternalQuery { get; set; }
        
//        public IList<IList<ITentRequestPost>> Mentioning { get; set; }
//        public IList<IList<ITentRequestPost>> NotMentioning { get; set; }

//        public IList<ITentPostType> PostTypes { get; set; }
//        public RequestSortByEnum SortBy { get; set; }

//        #endregion

//        #region Public methods.

//        public async Task ResolveEntities()
//        {
//            // Users.
//            if (this.Entities != null && this.Entities.Any())
//            {
//                var userTasks = this.Entities.Select(this.userLogic.GetUserAsync).ToList();
//                await Task.WhenAll(userTasks);
//                this.Users = userTasks
//                    .Where(t => t.Result != null)
//                    .Select(t => t.Result)
//                    .ToList();
//            }

//            // Mentions.
//            var mentionTasks = new List<Task>();
//            if (this.Mentioning != null)
//            {
//                mentionTasks.AddRange(this.Mentioning.SelectMany(m => m.Select(m2 => m2.ResolveEntity())));
//            }

//            if (this.NotMentioning != null)
//            {
//                mentionTasks.AddRange(this.NotMentioning.SelectMany(m => m.Select(m2 => m2.ResolveEntity())));
//            }

//            await Task.WhenAll(mentionTasks);

//            // Dates.
//            var sortProperty = this.GetPostSortProperty();
//            if (this.Since != null)
//            {
//                await this.Since.ResolvePost(sortProperty);
//            }

//            if (this.Until != null)
//            {
//                await this.Until.ResolvePost(sortProperty);
//            }
            
//            if (this.Before != null)
//            {
//                await this.Before.ResolvePost(sortProperty);
//            }
//        }

//        public string ToQueryString(ITentRequestDate sinceDate, ITentRequestDate beforeDate, bool noDate)
//        {
//            return this.queryStringHelpers.BuildQueryString(this.ToDictionary(sinceDate, beforeDate, noDate, true));
//        }

//        public TentPostPages GeneratePages(TentPost firstPost, TentPost lastPost, Func<TentPost, DateTime?> dateProperty, bool hasMore)
//        {
//            var result = new TentPostPages();

//            if (hasMore && this.Since == null)
//            {
//                result.Last = '?' + this.ToQueryString(this.requestDateFactory.MinValue(), null, false);
//                result.Next = '?' + this.ToQueryString(null, this.requestDateFactory.FromPost(lastPost, dateProperty), false);
//            }

//            if (this.Since != null || this.Before != null)
//            {
//                result.First = '?' + this.ToQueryString(null, null, true);
//                result.Previous = '?' + this.ToQueryString(this.requestDateFactory.FromPost(firstPost, dateProperty), null, false);
//            }

//            return result;
//        }

//        public IDictionary<string, object> ToDictionary(ITentRequestDate sinceDate, ITentRequestDate beforeDate, bool noDate, bool useRequestLimit)
//        {
//            var result = new Dictionary<string, object>();

//            // Entities.
//            if (this.Entities != null && this.Entities.Any())
//            {
//                result.Add("entities", this.Entities);
//            }
//            else if (this.OnlyFromFollowings)
//            {
//                result.Add("entities", "followings");
//            }

//            // Version.
//            if (!string.IsNullOrEmpty(this.VersionId))
//            {
//                result.Add("version", this.VersionId);
//            }

//            // Mentions.
//            if (this.Mentioning != null && this.Mentioning.Any())
//            {
//                result.Add("mentions", this.Mentioning);
//            }

//            // Negative mentions.
//            if (this.NotMentioning != null && this.NotMentioning.Any())
//            {
//                result.Add("-mentions", this.NotMentioning);
//            }

//            // Limit.
//            if (useRequestLimit)
//            {
//                if (this.RequestLimit.HasValue)
//                {
//                    result.Add("limit", this.RequestLimit.Value);
//                }
//            }
//            else if (this.Limit.HasValue)
//            {
//                result.Add("limit", this.Limit.Value);
//            }

//            // Bewit.
//            if (!string.IsNullOrEmpty(this.Bewit))
//            {
//                result.Add("bewit", this.Bewit);
//            }

//            // Profiles.
//            if (this.Profiles != RequestProfilesEnum.None)
//            {
//                var profileValues = new List<string>();
//                if (this.Profiles.HasFlag(RequestProfilesEnum.Entity))
//                {
//                    profileValues.Add("entity");
//                }

//                if (this.Profiles.HasFlag(RequestProfilesEnum.Refs))
//                {
//                    profileValues.Add("refs");
//                }

//                if (this.Profiles.HasFlag(RequestProfilesEnum.Mentions))
//                {
//                    profileValues.Add("mentions");
//                }

//                if (this.Profiles.HasFlag(RequestProfilesEnum.Permissions))
//                {
//                    profileValues.Add("permissions");
//                }

//                if (this.Profiles.HasFlag(RequestProfilesEnum.Parents))
//                {
//                    profileValues.Add("parents");
//                }

//                result.Add("profiles", new[] { profileValues });
//            }

//            // Time range parameters.
//            if (sinceDate != null || beforeDate != null || noDate)
//            {
//                if (sinceDate != null && sinceDate.IsValid)
//                {
//                    result.Add("since", sinceDate);
//                }

//                if (beforeDate != null && beforeDate.IsValid)
//                {
//                    result.Add("before", beforeDate);
//                }
//            }
//            else
//            {
//                if (this.Since != null && this.Since.IsValid)
//                {
//                    result.Add("since", this.Since);
//                }
//                else if (this.Until != null && this.Until.IsValid)
//                {
//                    result.Add("until", this.Until);
//                }

//                if (this.Before != null && this.Before.IsValid)
//                {
//                    result.Add("before", this.Before);
//                }
//            }

//            // Sort by.
//            if (this.SortBy != RequestSortByEnum.Default)
//            {
//                string sortBy;
//                switch (this.SortBy)
//                {
//                    case RequestSortByEnum.PublishedDate:
//                        sortBy = "published_at";
//                        break;
//                    case RequestSortByEnum.VersionPublishedDate:
//                        sortBy = "version.published_at";
//                        break;
//                    case RequestSortByEnum.VersionReceivedDate:
//                        sortBy = "version.received_at";
//                        break;
//                    default:
//                        sortBy = "received_at";
//                        break;
//                }
//                result.Add("sort_by", sortBy);
//            }

//            // Post types.
//            if (this.PostTypes != null && this.PostTypes.Any())
//            {
//                result.Add("types", new List<IEnumerable<string>> { this.PostTypes.Select(t => t.ToString()) });
//            }

//            return result;
//        }
        
//        public Func<TentPost, DateTime?> GetPostSortProperty()
//        {
//            switch (this.SortBy)
//            {
//                case RequestSortByEnum.PublishedDate:
//                    return p => p.PublishedAt;
//                case RequestSortByEnum.VersionPublishedDate:
//                    return p => p.Version.PublishedAt;
//                case RequestSortByEnum.VersionReceivedDate:
//                    return p => p.Version.ReceivedAt;
//                default:
//                    return p => p.ReceivedAt;
//            }
//        }

//        #endregion
//    }
//}