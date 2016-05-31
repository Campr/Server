using System;
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
        Task<TentPost<object>> GetRelationship(User user, User targetUser, CancellationToken cancellationToken = default(CancellationToken));
        Task<TentPost<object>> GetRelationship(User user, User targetUser, bool createIfNotFound, CancellationToken cancellationToken = default(CancellationToken));
        Task<TentPost<object>> GetRelationship(User user, User targetUser, bool createIfNotFound, bool propagate, CancellationToken cancellationToken = default(CancellationToken));
        Task<TentPost<object>> GetRelationship(User user, User targetUser, bool createIfNotFound, bool propagate, bool alwaysIncludeCredentials, CancellationToken cancellationToken = default(CancellationToken));

        Task<TentPost<object>> AcceptRelationship(User user, User targetUser, Uri credentialsLinkUri, CancellationToken cancellationToken = default(CancellationToken));

        Task<ITentHawkSignature> GetCredentialsForUserAsync(User user, User targetUser, CancellationToken cancellationToken = default(CancellationToken));
        Task<ITentHawkSignature> GetCredentialsForUserAsync(User user, User targetUser, bool createIfNotFound, CancellationToken cancellationToken = default(CancellationToken));
        Task<ITentHawkSignature> GetCredentialsForUserAsync(User user, User targetUser, bool createIfNotFound, TentPost<TentContentCredentials> credentials, CancellationToken cancellationToken = default(CancellationToken));
    }
}