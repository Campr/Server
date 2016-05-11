using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;

namespace Campr.Server.Lib.Logic
{
    public interface IPostLogic
    {
        Task<TentPost<T>> GetPostAsync<T>(User requester, User feedOwner, User user, string postId, string versionId = null, TentPost<TentContentCredentials> credentials = null, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task<TentPost<T>> GetLastPostOfTypeAsync<T>(User requester, User feedOwner, User user, ITentPostType type, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task<TentPost<T>> GetLastPostOfTypeMentioningAsync<T>(User requester, User feedOwner, User user, ITentPostType type, ITentRequestPost mention, CancellationToken cancellationToken = default(CancellationToken)) where T : class;

        Task<TentPost<TentContentCredentials>> CreateNewCredentialsPostAsync(User user, User targetUser, TentPost<object> targetPost);
        Task<TentPost<T>> CreatePostAsync<T>(User user, TentPost<T> post, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task CreateUserPostAsync<T>(User owner, TentPost<T> post, bool? isFromFollowing = null) where T : class;
         //Task<TentPost<T>> ImportPostFromLinkAsync<T>(User user, User targetUser, Uri uri) where T : class;

        Task<IList<TentPost<T>>> GetPostsAsync<T>(User requester, User feedOwner, ITentFeedRequest feedRequest, CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        Task<long> CountPostsAsync(User requester, User feedOwner, ITentFeedRequest feedRequest, CancellationToken cancellationToken = default(CancellationToken));

        //Task<IList<TentMention>> GetMentionsAsync(User user, User targetUser, string postId, ITentRequestParameters parameters, bool proxy);
        //Task<long> GetMentionsCountAsync(User user, User targetUser, string postId, ITentRequestParameters parameters, bool proxy);
        //Task<IList<TentMention>> GetVersionsAsync(User user, User targetUser, string postId, ITentRequestParameters parameters, bool proxy);
        //Task<long> GetVersionsCountAsync(User user, User targetUser, string postId, ITentRequestParameters parameters, bool proxy);
        //Task<IEnumerable<TentVersion>> GetVersionsChildrenAsync(User user, User targetUser, string postId, string versionId, ITentRequestParameters parameters, bool proxy);
        //Task<long> GetVersionsChildrenCountAsync(User user, User targetUser, string postId, string versionId, ITentRequestParameters parameters, bool proxy);
        //Task<IEnumerable<TentPost<object>>> GetPostsFromReplyChainAsync(User user, User targetUser, string postId, string versionId = null);
        //Task<long> GetPostsCountFromReplyChainAsync(User user, User targetUser, string postId, string versionId = null);

        Task<TentPost<TentContentMeta>> GetMetaPostAsync(User user, CancellationToken cancellationToken = default(CancellationToken));

        //Task<IDictionary<string, TentMetaProfile>> GetMetaProfileForUserAsync(string userId);
        //Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForMentionsAsync(IEnumerable<TentMention> mentions);
        //Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForVersionsAsync(IEnumerable<TentVersion> versions, RequestProfilesEnum requestedProfiles);
        //Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForPostAsync<T>(TentPost<T> post, IEnumerable<TentPost<T>> refs, RequestProfilesEnum requestedProfiles) where T : class ;
        //Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForPostsAsync<T>(IEnumerable<TentPost<T>> posts, IEnumerable<TentPost<T>> refs, RequestProfilesEnum requestedProfiles) where T : class;

        //IEnumerable<TentPost<object>> GetPostRefsForPost<T>(TentPost<T> post, int maxRefs) where T : class;
        //IEnumerable<TentPost<object>> GetPostRefsForPosts<T>(IEnumerable<TentPost<T>> posts, int maxRefs) where T : class;

        //Task<TentPost<object>> GetPostWithAttachmentAsync(string userId, string digest);
        //Task<TentPost<T>> GetLastPostByTypeWithMentionAsync<T>(string userId, ITentPostType postType, TentPostIdentifier mention) where T : class;

        //Task<TentPost<object>> GetSubscribingPostForTypeAsync(string userId, string targetUserId, params ITentPostType[] postTypes);
        //Task<IEnumerable<TentPost<object>>> GetSubscriberPostsForTypeAsync(string userId, int skip, int take, params ITentPostType[] postTypes);
        //Task<int> GetSubscriberPostsCountForTypeAsync(string userId, params ITentPostType[] postTypes);

        //Task<TentPost<object>> DeletePostAsync(User user, TentPost<object> post, bool specificVersion, bool createDeletePost = true);
    }
}