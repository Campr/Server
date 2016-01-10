using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;

namespace Campr.Server.Lib.Net.Tent
{
    public interface ITentClient
    {
        Task<TentPost<T>> RetrievePostForUserAsync<T>(TentPost<TentContentMeta> metaPost, ITentHawkSignature credentials, string postId, string versionId = null) where T : class;
        Task<IList<TentPost<T>>> RetrievePublicationsForUserAsync<T>(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials) where T : class;
        Task<long> RetrievePublicationsCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials);
        Task<IList<TentMention>> RetrieveMentionsForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, string versionId = null);
        Task<long> RetrieveMentionsCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, string versionId = null);
        Task<IList<TentVersion>> RetrieveVersionsForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId);
        Task<long> RetrieveVersionsCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId);
        Task<IList<TentVersion>> RetrieveVersionChildrenForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId);
        Task<long> RetrieveVersionChildrenCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId);
        Task<IHttpResponseMessage> GetAttachmentAsync(TentPost<TentContentMeta> metaPost, ITentHawkSignature credentials, string digest);
        Task<Uri> PostRelationshipAsync(string userHandle, TentPost<TentContentMeta> metaPost, TentPost<object> relationshipPost);
        Task<bool> PostNotificationAsync(TentPost<TentContentMeta> metaPost, TentPost<object> post, ITentHawkSignature credentials);
        Task<TentPost<T>> RetrievePostAtUriAsync<T>(Uri postUri, ITentHawkSignature credentials = null) where T : class;
        Task<bool> PostNotificationAtUriAsync(Uri uri, TentPost<object> post, ITentHawkSignature credentials);
    }
}