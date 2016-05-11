using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;

namespace Campr.Server.Lib.Logic
{
    public interface IFollowLogic
    {
        //Task<IDbPost<object>> GetRelationship(DbUser user, DbUser targetUser, bool createIfNotFound = true, bool propagate = true, bool alwaysIncludeCredentials = false);
        //Task<IDbPost<object>> AcceptRelationship(DbUser user, DbUser targetUser, Uri credentialsLinkUri, string entity, string postId);
        Task<ITentHawkSignature> GetCredentialsForUser(User user, User targetUser, CancellationToken cancellationToken = default(CancellationToken));
        Task<ITentHawkSignature> GetCredentialsForUser(User user, User targetUser, bool createIfNotFound, CancellationToken cancellationToken = default(CancellationToken));
        Task<ITentHawkSignature> GetCredentialsForUser(User user, User targetUser, bool createIfNotFound, TentPost<TentContentCredentials> credentials, CancellationToken cancellationToken = default(CancellationToken));
    }
}