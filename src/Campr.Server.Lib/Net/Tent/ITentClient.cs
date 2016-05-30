using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Net.Tent
{
    public interface ITentClient : ISimpleTentClient
    {
        Task<TentPost<T>> GetAsync<T>(string postId, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task<TentPost<T>> GetAsync<T>(string postId, string versionId, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task<IList<TentPost<T>>> GetAsync<T>(ITentFeedRequest feedRequest, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task<long> CountAsync(ITentFeedRequest feedRequest, CancellationToken cancellationToken = default(CancellationToken));
        Task<Uri> PostRelationshipAsync(User requester, TentPost<object> relationship, CancellationToken cancellationToken = default(CancellationToken));

        //Task<TentPost<T>> RetrievePostForUserAsync<T>(TentPost<TentContentMeta> metaPost, ITentHawkSignature credentials, string postId, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        //Task<TentPost<T>> RetrievePostForUserAsync<T>(TentPost<TentContentMeta> metaPost, ITentHawkSignature credentials, string postId, string versionId, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        //Task<IList<TentPost<T>>> RetrievePublicationsForUserAsync<T>(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        //Task<long> RetrievePublicationsCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, CancellationToken cancellationToken = default(CancellationToken));
        //Task<IList<TentMention>> RetrieveMentionsForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, CancellationToken cancellationToken = default(CancellationToken));
        //Task<IList<TentMention>> RetrieveMentionsForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, string versionId, CancellationToken cancellationToken = default(CancellationToken));
        //Task<long> RetrieveMentionsCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, CancellationToken cancellationToken = default(CancellationToken));
        //Task<long> RetrieveMentionsCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, string versionId, CancellationToken cancellationToken = default(CancellationToken));
        //Task<IList<TentVersion>> RetrieveVersionsForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, CancellationToken cancellationToken = default(CancellationToken));
        //Task<long> RetrieveVersionsCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, CancellationToken cancellationToken = default(CancellationToken));
        //Task<IList<TentVersion>> RetrieveVersionChildrenForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, CancellationToken cancellationToken = default(CancellationToken));
        //Task<long> RetrieveVersionChildrenCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, CancellationToken cancellationToken = default(CancellationToken));
        //Task<IHttpResponseMessage> GetAttachmentAsync(TentPost<TentContentMeta> metaPost, ITentHawkSignature credentials, string digest, CancellationToken cancellationToken = default(CancellationToken));
        //Task<Uri> PostRelationshipAsync(string userHandle, TentPost<TentContentMeta> metaPost, TentPost<object> relationshipPost, CancellationToken cancellationToken = default(CancellationToken));
        //Task<bool> PostNotificationAsync(TentPost<TentContentMeta> metaPost, TentPost<object> post, ITentHawkSignature credentials, CancellationToken cancellationToken = default(CancellationToken));
        //Task<TentPost<T>> RetrievePostAtUriAsync<T>(Uri postUri, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        //Task<TentPost<T>> RetrievePostAtUriAsync<T>(Uri postUri, ITentHawkSignature credentials, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        //Task<bool> PostNotificationAtUriAsync(Uri uri, TentPost<object> post, ITentHawkSignature credentials, CancellationToken cancellationToken = default(CancellationToken));
    }
}