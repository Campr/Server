//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Campr.Server.Lib.Enums;
//using Campr.Server.Lib.Models.Db;
//using Campr.Server.Lib.Models.Tent;

//namespace Campr.Server.Lib.Models.Other
//{
//    public interface ITentRequestParameters
//    {
//        #region Properties.

//        string VersionId { get; }

//        ITentRequestDate Since { get; }
//        ITentRequestDate Until { get; }
//        ITentRequestDate Before { get; }

//        int? RequestLimit { get; }
//        int? Limit { get; }
//        int? Skip { get; }
//        int? MaxRefs { get; }
//        RequestProfilesEnum Profiles { get; }
//        CacheControlValue CacheControl { get; }

//        IList<string> Entities { get; }
//        IList<User> Users { get; }

//        bool OnlyFromFollowings { get; }
//        bool OnlyFromFollowers { get; }

//        string Bewit { get; }

//        string ExternalQuery { get; }

//        IList<IList<ITentRequestPost>> Mentioning { get; }
//        IList<IList<ITentRequestPost>> NotMentioning { get; }

//        IList<ITentPostType> PostTypes { get; }
//        RequestSortByEnum SortBy { get; }

//        #endregion

//        #region Methods.

//        Task ResolveEntities();
//        string ToQueryString(ITentRequestDate sinceDate = null, ITentRequestDate beforeDate = null, bool noDate = false);
//        TentPostPages GeneratePages(TentPost firstPost, TentPost lastPost, Func<TentPost, DateTime?> dateProperty, bool hasMore);
//        IDictionary<string, object> ToDictionary(ITentRequestDate sinceDate = null, ITentRequestDate beforeDate = null, bool noDate = false, bool useRequestLimit = false);
//        Func<TentPost, DateTime?> GetPostSortProperty();

//        #endregion
//    }
//}