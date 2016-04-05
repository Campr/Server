using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Tent;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;

namespace Campr.Server.Lib.Models.Other
{
    public interface ITentFeedRequest
    {
        ITentFeedRequest AddTypes(params ITentPostType[] newTypes);
        ITentFeedRequest AddUsers(params User[] newUsers);
        ITentFeedRequest ReplaceUsers(params User[] newUsers);

        ITentFeedRequest AddEntities(params string[] newEntities);
        ITentFeedRequest AddSpecialEntities(TentFeedRequestSpecialEntities specialEntities); 
        ITentFeedRequest AddMentions(params string[] mentionedEntities);
        ITentFeedRequest AddMentions(params ITentRequestPost[] mentionedPosts);
        ITentFeedRequest AddNotMentions(params string[] notMentionedEntities);
        ITentFeedRequest AddNotMentions(params ITentRequestPost[] notMentionedPosts);
        ITentFeedRequest AddProfiles(TentFeedRequestProfiles newProfiles);
        ITentFeedRequest AddLimit(uint newLimit);
        ITentFeedRequest AddSkip(uint newSkip);
        ITentFeedRequest AddMaxRefs(uint newMaxRefs);
        ITentFeedRequest AddPostBoundary(TentPost boundaryPost, TentFeedRequestBoundaryType boundaryType);
        ITentFeedRequest AddPostBoundary(ITentRequestPost boundaryPost, TentFeedRequestBoundaryType boundaryType);
        ITentFeedRequest AddBoundary(ITentRequestDate boundaryDate, TentFeedRequestBoundaryType boundaryType);
        ITentFeedRequest SortBy(TentFeedRequestSort newSortBy);

        Task<Uri> AsUriAsync(string parameter = null, CancellationToken cancellationToken = default(CancellationToken));
        Task<ReqlExpr> AsTableQueryAsync(RethinkDB rdb, Table table, string requesterId, string feedOwnerId, CancellationToken cancellationToken = default(CancellationToken));
        Task<ReqlExpr> AsCountTableQueryAsync(RethinkDB rdb, Table table, string requesterId, string feedOwnerId, CancellationToken cancellationToken = default(CancellationToken));
        ITentHawkSignature AsCredentials();
        uint? AsLimit();
    }
}