using System;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Other
{
    public interface ITentFeedRequest<TPost>
    {
        ITentFeedRequest<TPost> AddTypes(params ITentPostType[] newTypes);
        ITentFeedRequest<TPost> AddEntities(params string[] newEntities);
        ITentFeedRequest<TPost> AddSpecialEntities(TentFeedRequestSpecialEntities specialEntities); 
        ITentFeedRequest<TPost> AddMentions(params string[] mentionedEntities);
        ITentFeedRequest<TPost> AddMentions(params ITentRequestPost[] mentionedPosts);
        ITentFeedRequest<TPost> AddNotMentions(params string[] mentionedEntities);
        ITentFeedRequest<TPost> AddNotMentions(params ITentRequestPost[] mentionedPosts);
        ITentFeedRequest<TPost> AddProfiles(TentFeedRequestProfiles newProfiles);
        ITentFeedRequest<TPost> AddLimit(uint newLimit);
        ITentFeedRequest<TPost> AddSkip(uint newSkip);
        ITentFeedRequest<TPost> AddMaxRefs(uint newMaxRefs);
        ITentFeedRequest<TPost> AddBoundary(ITentRequestDate boundaryPost, TentFeedRequestBoundaryType boundaryType);
        ITentFeedRequest<TPost> SortBy(TentFeedRequestSort newSortBy);

        Uri AsUri(string parameter = null);
        ITentHawkSignature AsCredentials();
        uint? AsLimit();
    }
}