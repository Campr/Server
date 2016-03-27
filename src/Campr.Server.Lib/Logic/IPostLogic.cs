//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Campr.Server.Lib.Enums;
//using Campr.Server.Lib.Models.Db;
//using Campr.Server.Lib.Models.Other;
//using Campr.Server.Lib.Models.Tent;
//using Campr.Server.Lib.Models.Tent.PostContent;

//namespace Campr.Server.Lib.Logic
//{
//    public interface IPostLogic
//    {
//        Task<TentPost<object>> GetPostAsync(User user, User targetUser, string postId, string versionId = null, CacheControlValue cacheControl = CacheControlValue.ProxyIfMiss, TentPost<TentContentCredentials> credentialsPost = null, CancellationToken cancellationToken = default(CancellationToken));





//        Task<TentPost<T>> CreateNewPostAsync<T>(User user, string postType, T postContent, bool isPublic = true, IEnumerable<TentMention> mentions = null, IEnumerable<TentPostRef> postRefs = null, IEnumerable<TentPostAttachment> attachments = null, bool propagate = true) where T : class;
//        Task<TentPost<TentContentCredentials>> CreateNewCredentialsPostAsync(User user, User targetUser, TentPost<object> targetPost);
//        Task<TentPost<T>> CreatePostAsync<T>(User user, TentPost<T> post, bool newVersion = false, bool propagate = true, bool import = false) where T : class;
//        Task CreateFeedItemAsync<T>(string userId, TentPost<T> post, bool? isSubscriber = null) where T : class;
//        Task<TentPost<T>> ImportPostFromLinkAsync<T>(User user, User targetUser, Uri uri) where T : class;

//        Task<IList<TentPost<object>>> GetPostsFromFeedAsync(string userId, ITentRequestParameters parameters);
//        Task<long> GetPostsCountFromFeedAsync(string userId, ITentRequestParameters parameters);
//        Task<IList<TentPost<object>>> GetPostsFromPublicationsAsync(User user, User targetUser, ITentRequestParameters parameters, bool proxy);
//        Task<long> GetPostsCountFromPublicationsAsync(User user, User targetUser, ITentRequestParameters parameters, bool proxy);
//        Task<IList<TentMention>> GetMentionsAsync(User user, User targetUser, string postId, ITentRequestParameters parameters, bool proxy);
//        Task<long> GetMentionsCountAsync(User user, User targetUser, string postId, ITentRequestParameters parameters, bool proxy);
//        Task<IList<TentMention>> GetVersionsAsync(User user, User targetUser, string postId, ITentRequestParameters parameters, bool proxy);
//        Task<long> GetVersionsCountAsync(User user, User targetUser, string postId, ITentRequestParameters parameters, bool proxy);
//        Task<IEnumerable<TentVersion>> GetVersionsChildrenAsync(User user, User targetUser, string postId, string versionId, ITentRequestParameters parameters, bool proxy);
//        Task<long> GetVersionsChildrenCountAsync(User user, User targetUser, string postId, string versionId, ITentRequestParameters parameters, bool proxy);
//        Task<IEnumerable<TentPost<object>>> GetPostsFromReplyChainAsync(User user, User targetUser, string postId, string versionId = null);
//        Task<long> GetPostsCountFromReplyChainAsync(User user, User targetUser, string postId, string versionId = null);
        
//        Task<TentPost<TentContentMeta>> GetMetaPostForUserAsync(User user);

//        Task<IDictionary<string, TentMetaProfile>> GetMetaProfileForUserAsync(string userId);
//        Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForMentionsAsync(IEnumerable<TentMention> mentions);
//        //Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForVersionsAsync(IEnumerable<TentVersion> versions, RequestProfilesEnum requestedProfiles);
//        //Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForPostAsync<T>(TentPost<T> post, IEnumerable<TentPost<T>> refs, RequestProfilesEnum requestedProfiles) where T : class ;
//        //Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForPostsAsync<T>(IEnumerable<TentPost<T>> posts, IEnumerable<TentPost<T>> refs, RequestProfilesEnum requestedProfiles) where T : class;

//        IEnumerable<TentPost<object>> GetPostRefsForPost<T>(TentPost<T> post, int maxRefs) where T : class;
//        IEnumerable<TentPost<object>> GetPostRefsForPosts<T>(IEnumerable<TentPost<T>> posts, int maxRefs) where T : class;
        
//        Task<TentPost<object>> GetPostWithAttachmentAsync(string userId, string digest); 
//        Task<TentPost<T>> GetLastPostByTypeWithMentionAsync<T>(string userId, ITentPostType postType, TentPostIdentifier mention) where T : class;
        
//        Task<TentPost<object>> GetSubscribingPostForTypeAsync(string userId, string targetUserId, params ITentPostType[] postTypes);
//        Task<IEnumerable<TentPost<object>>> GetSubscriberPostsForTypeAsync(string userId, int skip, int take, params ITentPostType[] postTypes);
//        Task<int> GetSubscriberPostsCountForTypeAsync(string userId, params ITentPostType[] postTypes);

//        Task<TentPost<object>> DeletePostAsync(User user, TentPost<object> post, bool specificVersion, bool createDeletePost = true);
//    }
//}