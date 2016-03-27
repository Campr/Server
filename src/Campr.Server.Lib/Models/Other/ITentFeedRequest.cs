using System;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Models.Tent;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;

namespace Campr.Server.Lib.Models.Other
{
    public interface ITentFeedRequest
    {
        ITentFeedRequest AddTypes(params ITentPostType[] newTypes);
        ITentFeedRequest AddEntities(params string[] newEntities);
        ITentFeedRequest AddSpecialEntities(TentFeedRequestSpecialEntities specialEntities); 
        ITentFeedRequest AddMentions(params string[] mentionedEntities);
        ITentFeedRequest AddMentions(params ITentRequestPost[] mentionedPosts);
        ITentFeedRequest AddNotMentions(params string[] mentionedEntities);
        ITentFeedRequest AddNotMentions(params ITentRequestPost[] mentionedPosts);
        ITentFeedRequest AddProfiles(TentFeedRequestProfiles newProfiles);
        ITentFeedRequest AddLimit(uint newLimit);
        ITentFeedRequest AddSkip(uint newSkip);
        ITentFeedRequest AddMaxRefs(uint newMaxRefs);
        ITentFeedRequest AddPostBoundary(TentPost boundaryPost, TentFeedRequestBoundaryType boundaryType);
        ITentFeedRequest AddPostBoundary(ITentRequestPost boundaryPost, TentFeedRequestBoundaryType boundaryType);
        ITentFeedRequest AddBoundary(ITentRequestDate boundaryDate, TentFeedRequestBoundaryType boundaryType);
        ITentFeedRequest SortBy(TentFeedRequestSort newSortBy);

        Uri AsUri(string parameter = null);
        ReqlExpr AsTableQuery(RethinkDB rdb, Table table, string ownerId);
        ITentHawkSignature AsCredentials();
        uint? AsLimit();
    }
}