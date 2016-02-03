using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Connectors.Queues;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Exceptions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Other.Factories;
using Campr.Server.Lib.Models.Queues;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;
using Campr.Server.Lib.Net.Tent;
using Campr.Server.Lib.Repositories;
using Campr.Server.Lib.Services;

namespace Campr.Server.Lib.Logic
{
    class PostLogic : IPostLogic
    {
        #region Constructor & Dependencies.

        public PostLogic(IAttachmentLogic attachmentLogic,
            IPostRepository postRepository,
            IUserRepository userRepository,
            ITentServDbClient dbClient,
            ITentQueues tentQueues,
            ITentClient tentClient,
            IDiscoveryService discoveryService,
            IDbPostFactory dbPostFactory,
            ITentPostTypeFactory postTypeFactory,
            ITextHelpers textHelpers,
            IModelHelpers modelHelpers,
            ICryptoHelpers cryptoHelpers,
            ITentConstants tentConstants,
            ITentServConfiguration configuration,
            IUnityContainer container)
        {
            Ensure.Argument.IsNotNull(attachmentLogic, "attachmentLogic");
            Ensure.Argument.IsNotNull(postRepository, "postRepository");
            Ensure.Argument.IsNotNull(userRepository, "userRepository");
            Ensure.Argument.IsNotNull(dbClient, "dbClient");
            Ensure.Argument.IsNotNull(tentQueues, "tentQueues");
            Ensure.Argument.IsNotNull(discoveryService, "discoveryService");
            Ensure.Argument.IsNotNull(dbPostFactory, "dbPostFactory");
            Ensure.Argument.IsNotNull(postTypeFactory, "postTypeFactory");
            Ensure.Argument.IsNotNull(textHelpers, "textHelpers");
            Ensure.Argument.IsNotNull(modelHelpers, "modelHelpers");
            Ensure.Argument.IsNotNull(cryptoHelpers, "cryptoHelpers");
            Ensure.Argument.IsNotNull(tentConstants, "tentConstants");
            Ensure.Argument.IsNotNull(configuration, "configuration");
            Ensure.Argument.IsNotNull(container, "container");

            this.attachmentLogic = attachmentLogic;
            this.postRepository = postRepository;
            this.userRepository = userRepository;
            this.dbClient = dbClient;
            this.tentQueues = tentQueues;
            this.tentClient = tentClient;
            this.discoveryService = discoveryService;
            this.dbPostFactory = dbPostFactory;
            this.postTypeFactory = postTypeFactory;
            this.textHelpers = textHelpers;
            this.modelHelpers = modelHelpers;
            this.cryptoHelpers = cryptoHelpers;
            this.tentConstants = tentConstants;
            this.configuration = configuration;

            // We have to use this *bad* strategy for UserLogic because of the circular dependency.
            this.userLogic = new Lazy<IUserLogic>(() => container.Resolve<IUserLogic>());
            this.followLogic = new Lazy<IFollowLogic>(() => container.Resolve<IFollowLogic>());
            this.typeSpecificLogic = new Lazy<ITypeSpecificLogic>(() => container.Resolve<ITypeSpecificLogic>());
            this.appPostLogic = new Lazy<IAppPostLogic>(() => container.Resolve<IAppPostLogic>());
        }

        private readonly IAttachmentLogic attachmentLogic;
        private readonly IPostRepository postRepository;
        private readonly IUserRepository userRepository;
        private readonly ITentServDbClient dbClient;
        private readonly ITentQueues tentQueues;
        private readonly ITentClient tentClient;
        private readonly IDiscoveryService discoveryService;
        private readonly IDbPostFactory dbPostFactory;
        private readonly ITentPostTypeFactory postTypeFactory;
        private readonly ITextHelpers textHelpers;
        private readonly IModelHelpers modelHelpers;
        private readonly ICryptoHelpers cryptoHelpers;
        private readonly ITentConstants tentConstants;
        private readonly ITentServConfiguration configuration;

        private readonly Lazy<IUserLogic> userLogic;
        private readonly Lazy<IFollowLogic> followLogic; 
        private readonly Lazy<ITypeSpecificLogic> typeSpecificLogic;
        private readonly Lazy<IAppPostLogic> appPostLogic; 

        #endregion

        #region Interface implementation.

        public Task<TentPost<T>> CreateNewPostAsync<T>(
            User user, 
            string postType, 
            T postContent, 
            bool isPublic = true, 
            IEnumerable<TentMention> mentions = null, 
            IEnumerable<TentPostRef> postRefs = null, 
            IEnumerable<TentPostAttachment> attachments = null, 
            bool propagate = true) where T : class
        {
            // Create the post object.
            var apiPost = new TentPost<T>
            {
                Entity = this.modelHelpers.GetUserEntity(user),
                Type = postType,
                Content = postContent,
                PublishedAt = DateTime.UtcNow,
                Mentions = mentions?.ToList(),
                Refs = postRefs?.ToList(),
                Attachments = attachments?.ToList(),
                Permissions = new TentPermissions
                {
                    Public = isPublic
                }
            };

            // Save and propagate.
            return this.CreatePostAsync(user, apiPost, false, propagate);
        }

        public async Task<TentPost<TentContentCredentials>> CreateNewCredentialsPostAsync(User user, User targetUser, TentPost<object> targetPost)
        {
            Ensure.Argument.IsNotNull(user, nameof(user));
            Ensure.Argument.IsNotNull(targetUser, nameof(targetUser));
            Ensure.Argument.IsNotNull(targetPost, nameof(targetPost));

            // Create the post's content.
            var credentialsContent = new TentContentCredentials
            {
                HawkAlgorithm = this.tentConstants.HawkAlgorithm,
                HawkKey = this.cryptoHelpers.GenerateNewSecret()
            };

            // Create the mention to the target post.
            var postMention = new TentMention
            {
                User = targetUser,
                Post = targetPost
            };

            // Create the credentials post.
            var credentialsPost = await this.CreateNewPostAsync(user, this.tentConstants.CredentialsPostType, credentialsContent, false, new[] { postMention }, null, null, false);

            // Set the credentials post as passenger on the target post.
            targetPost.Post.PassengerCredentials = credentialsPost.Post;

            return credentialsPost;
        }

        public async Task<TentPost<T>> CreatePostAsync<T>(
            User user, 
            TentPost<T> post, 
            bool newVersion = false, 
            bool propagate = true, 
            bool import = false) where T: class 
        {
            // If the post already has an Id, try to retrieve exisiting versions of this post.
            IDbPost<T> lastPostVersion = null;
            if (!string.IsNullOrEmpty(post.Id))
            {
                // If a VersionId was specified, try to retrieve this exact post version.
                if (post.Version != null && !string.IsNullOrEmpty(post.Version.Id) && !newVersion)
                {
                    var existingPost = this.postRepository.GetPostVersion<T>(user.Id, post.Id, post.Version.Id);
                    if (existingPost != null)
                    {
                        return existingPost;
                    }
                }
                // Otherwise, just retrieve the last published version.
                else
                {
                    lastPostVersion = this.postRepository.GetPostLastVersion<T>(user.Id, post.Id);
                }
            }
            // Otherwise, generate a new Id for this post.
            else
            {
                post.Id = this.textHelpers.GenerateUniqueId();
            }

            // If a previous was required and wasn't found, return.
            if (newVersion && lastPostVersion == null)
            {
                return null;
            }
            
            // If needed, set the entity on the post.
            if (string.IsNullOrEmpty(post.Entity))
            {
                post.Entity = this.modelHelpers.GetUserEntity(user);
            }

            // If this post doesn't have a version, create it.
            if (newVersion || post.Version == null)
            {
                post.Version = new ApiVersion();
            }

            // If needed, set the version published date.
            if (!post.Version.PublishedAt.HasValue)
            {
                post.Version.PublishedAt = post.PublishedAt.HasValue
                    ? post.PublishedAt
                    : DateTime.UtcNow;
            }

            // If needed, set the published date.
            if (!post.PublishedAt.HasValue)
            {
                post.PublishedAt = lastPostVersion != null
                    ? lastPostVersion.Post.PublishedAt
                    : DateTime.UtcNow;
            }

            // Update the ReceivedAt properties.
            if (import)
            {
                post.Version.ReceivedAt = post.Version.ReceivedAt ?? DateTime.UtcNow;
                post.ReceivedAt = post.ReceivedAt ?? post.Version.ReceivedAt;
            }
            else
            {
                post.Version.ReceivedAt = DateTime.UtcNow;
                post.ReceivedAt = lastPostVersion != null
                    ? lastPostVersion.Post.ReceivedAt
                    : post.Version.ReceivedAt;
            }

            // Resolve the mentions, post refs and permissions.
            if (!(post is ITentPost<TentContentMeta>))
            {
                await this.ResolveMentionsAsync(user, post);
                await this.ResolvePostRefsAsync(user, post);
                await this.ResolveParentsAsync(user, post);
                await this.ResolvePermissionsAsync(user, post);
            }

            // Process attachments.
            if (user.IsInternal())
            {
                // Only keep ported attachments that existed on the last Version.
                if (post.Attachments != null && post.Attachments.Any())
                {
                    post.Attachments = post.Attachments
                        .Select(a =>
                        {
                            if (a.Data != null)
                            {
                                return a;
                            }

                            if (lastPostVersion != null && lastPostVersion.Post.Attachments != null)
                            {
                                return lastPostVersion.Post.Attachments.FirstOrDefault(la => 
                                    la.Digest == a.Digest
                                    && la.Name == a.Name
                                    && la.Category == a.Category
                                    && la.ContentType == a.ContentType);
                            }

                            return null;
                        })
                        .ToList();

                    // Upload new attachments.
                    await Task.WhenAll(post.Attachments
                        .Where(a => a.Data != null)
                        .Select(a => this.attachmentLogic.SaveAttachment(a.Data, a.Digest)));
                }
            }

            // Specific actions.
            if (!await this.typeSpecificLogic.Value.SpecificActionCreatePostAsync(user, post, import))
            {
                return null;
            }

            // Compute the VersionId for this post.
            var versionId = this.modelHelpers.GetVersionIdFromPost(post);
            if (!string.IsNullOrEmpty(post.Version.Id) && post.Version.Id != versionId)
            {
                throw new Exception("The provided VersionId is not valid.");
            }

            // Update the VersionId.
            post.Version.Id = versionId;

            // Save the post to the Db.
            var dbPost = this.dbPostFactory.CreatePost(user.Id, user.Id, post);
            this.postRepository.CreatePost(dbPost);

            // If this post was from an external user, we're done here.
            // If we were instructed not to propagate the post, we're done too.
            if (!user.IsInternal() || !propagate || import)
            {
                return dbPost;
            }

            // If needed, propagate to apps.
            var postType = this.postTypeFactory.FromString(dbPost.Post.Type);
            var appCountForType = await this.appPostLogic.Value.GetAppPostsCountForTypeAsync(dbPost.Post.UserId, postType, dbPost.Post.Permissions.Public);
            if (appCountForType > 0)
            {
                await this.tentQueues.AppNotifications.AddMessageAsync(new QueueAppNotificationMessage
                {
                    OwnerId = dbPost.Post.UserId,
                    UserId = dbPost.Post.UserId,
                    PostId = dbPost.Post.Id,
                    VersionId = dbPost.Post.Version.Id
                });
            }

            // If this post mentions other users, queue it for propagation.
            if (dbPost.Post.Mentions != null && dbPost.Post.Mentions.Any(m => 
                    m.UserId != dbPost.Post.UserId && m.UserId != default(ObjectId)))
            {
                await this.tentQueues.Mentions.AddMessageAsync(new QueueMentionMessage
                {
                    UserId = dbPost.Post.UserId,
                    PostId = dbPost.Post.Id,
                    VersionId = dbPost.Post.Version.Id
                });
            }

            // Propagate to subscriptions.
            ITentPostType[] subscriptionPostTypes;
            if (dbPost.Post.Type.StartsWith(this.tentConstants.DeletePostType()))
            {
                subscriptionPostTypes = dbPost.Post.PostRefs
                    .Where(p => p.Post != null)
                    .Select(p => this.postTypeFactory.FromString(p.Post.Post.Type))
                    .ToArray();
            }
            else
            {
                subscriptionPostTypes = new[] { this.postTypeFactory.FromString(dbPost.Post.Type) };
            }

            var subscriptionsCount = await this.GetSubscriberPostsCountForTypeAsync(user.Id, subscriptionPostTypes);
            for (var subscriptionIndex = 0; subscriptionIndex < subscriptionsCount; subscriptionIndex += this.configuration.SubscriptionsBatchSize())
            {
                await this.tentQueues.Subscriptions.AddMessageAsync(new QueueSubscriptionMessage
                {
                    UserId = user.Id,
                    PostId = dbPost.Post.Id,
                    VersionId = dbPost.Post.Version.Id,
                    Skip = subscriptionIndex * this.configuration.SubscriptionsBatchSize(),
                    Take = this.configuration.SubscriptionsBatchSize()
                });

                // TODO: This could be converted to Fire & Forget to speed up response time (will only be an issue when people start having very large subscriber bases.
            }

            return dbPost;
        }

        public async Task CreateFeedItemAsync<T>(ObjectId userId, IDbPost<T> post, bool? isSubscriber = null) where T : class
        {
            var postType = this.postTypeFactory.FromString(post.Post.Type);

            // If needed, check if our user is a subscriber.
            if (!isSubscriber.HasValue)
            {
                var subscription = await this.GetSubscribingPostForTypeAsync(userId, post.Post.UserId, postType);
                isSubscriber = subscription != null;
            }
            
            // TODO: Decide if any sort of validation should be performed here.
            //// If our user isn't a subscriber, check for mentions.
            //if (!isSubscriber.Value
            //    && (post.Post.Mentions == null
            //        || post.Post.Mentions.All(m => m.UserId != userId)))
            //{
            //    return;
            //}

            // Create the feed item and save it to the db.
            var feedItem = this.dbPostFactory.CreateFeedItem(userId, post, isSubscriber.Value);
            this.postRepository.CreatePost(feedItem);
            
            // If needed, propagate to apps.
            var appCountForType = await this.appPostLogic.Value.GetAppPostsCountForTypeAsync(userId, postType, post.Post.Permissions.Public);
            if (appCountForType > 0)
            {
                await this.tentQueues.AppNotifications.AddMessageAsync(new QueueAppNotificationMessage
                {
                    OwnerId = userId,
                    UserId = post.Post.UserId,
                    PostId = post.Post.Id,
                    VersionId = post.Post.Version.Id
                });
            }
        }

        public async Task<TentPost<T>> ImportPostFromLinkAsync<T>(User user, User targetUser, Uri uri) where T : class 
        {
            // Retrieve the post.
            var externalPost = await this.tentClient.RetrievePostAtUriAsync<T>(uri);
            if (externalPost == null)
            {
                return null;
            }

            // Save this post to the Db.
            var externalDbPost = await this.CreatePostAsync(targetUser, externalPost);

            // Add a feed item to the post for our user.
            var feedItem = this.dbPostFactory.CreateFeedItem(user.Id, externalDbPost, false);
            this.postRepository.CreatePost(feedItem);

            return externalDbPost;
        }

        public async Task<IDbPost<object>> GetPostAsync(DbUser user, DbUser targetUser, string postId, string versionId = null, CacheControlValue cacheControl = CacheControlValue.ProxyIfMiss, ITentPost<TentContentCredentials> credentialsPost = null)
        {
            // Decide of the OwnerId depending on the proxy value.
            var ownerId = cacheControl == CacheControlValue.NoProxy
                ? user.Id
                : targetUser.Id;

            // First, try to retrieve this post internally.
            var query = this.dbClient.GetPostCollection<object>()
                .AsQueryable()
                .Where(p => p.OwnerId == ownerId
                    && p.DeletedAt == null
                    && p.BPost.UserId == targetUser.Id
                    && p.BPost.Id == postId);

            // If needed, add condition for version.
            if (!string.IsNullOrWhiteSpace(versionId))
            {
                query = query.Where(p => p.BPost.Version.Id == versionId);
            }

            // If needed, add conditions for permissions.
            if (user == null || user.Id != targetUser.Id)
            {
                if (user != null)
                {
                    query = query.Where(p =>
                        p.BPost.Permissions.Public
                        || p.BPost.Permissions.UserIds.Contains(user.Id));
                }
                else
                {
                    query = query.Where(p => p.BPost.Permissions.Public);
                }
            }

            // Sort and retrieve the first element.
            var post = query
                .OrderByDescending(p => p.BPost.Version.ReceivedAt)
                .FirstOrDefault();
            if (post != null)
            {
                return post;
            }

            // Otherwise, if this is an internal user, return now.
            if (targetUser.IsInternal() || user == null || cacheControl == CacheControlValue.NoProxy)
            {
                return null;
            }

            // Try to retrieve this post externally.
            var metaPost = (await this.GetMetaPostForUserAsync(targetUser)).Post;
            var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser, false, credentialsPost);

            var externalPost = await this.tentClient.RetrievePostForUserAsync<object>(metaPost, credentials, postId, versionId);
            var externalDbPost = await this.CreatePostAsync(targetUser, externalPost);

            return externalDbPost;
        }

        public async Task<int> GetPostCountFromFeedAsync(User userId, ITentRequestParameters parameters)
        {
            return this.BuildFeedRequest(userId, parameters).Count();
        }

        public async Task<IEnumerable<ITentPost<object>>> GetPostsFromFeedAsync(ObjectId userId, ITentRequestParameters parameters)
        {
            return this.BuildFeedRequest(userId, parameters)
                .Take(parameters.Limit.GetValueOrDefault())
                .Select(p => p.Post)
                .ToList();

            //// TEMP: Get the JSON for this query.
            //var imq = (query as MongoQueryable<DbPost<object>>).GetMongoQuery();
            //var queryString = imq.ToString();

            //// you could also just do;
            //var cursor = this.dbClient.GetFeedPostCollection<object>().FindAs(typeof(DbPost<object>), imq);
            //var explainDoc = cursor.Explain();
        }

        public async Task<long> GetPostsCountFromFeedAsync(ObjectId userId, ITentRequestParameters parameters)
        {
            return this.BuildFeedRequest(userId, parameters).Count();
        }

        public async Task<IEnumerable<ITentPost<object>>> GetPostsFromPublicationsAsync(DbUser user, DbUser targetUser, ITentRequestParameters parameters, bool proxy)
        {
            // If the user is internal, return from the Db.
            if (targetUser.IsInternal() || !proxy)
            {
                return this.BuildPublicationsRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, parameters, proxy)
                    .Take(parameters.Limit.GetValueOrDefault())
                    .Select(p => p.Post)
                    .ToList();
            }
            
            // If we don't have an authenticated user, return now.
            if (user == null)
            {
                return null;
            }

            // Otherwise, proxy.
            var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
            var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

            var externalPosts = await this.tentClient.RetrievePublicationsForUserAsync<object>(targetUserMetaPost.Post, parameters, credentials);
            if (externalPosts == null)
            {
                return null;
            }

            var injestedPosts = await this.InjestPosts(externalPosts.ToList());
            externalPosts = injestedPosts.Select(p => p.Post);

            return externalPosts;
        }

        public async Task<long> GetPostsCountFromPublicationsAsync(DbUser user, DbUser targetUser, ITentRequestParameters parameters, bool proxy)
        {
            // If the user is internal, return from the Db.
            if (targetUser.IsInternal() || !proxy)
            {
                return this.BuildPublicationsRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, parameters, proxy).Count();
            }
            
            // If we don't have an authenticated user, return now.
            if (user == null)
            {
                return 0;
            }

            // Otherwise, proxy.
            var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
            var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

            return await this.tentClient.RetrievePublicationsCountForUserAsync(targetUserMetaPost.Post, parameters, credentials);
        }

        public async Task<IEnumerable<ApiMention>> GetMentionsAsync(DbUser user, DbUser targetUser, string postId, ITentRequestParameters parameters, bool proxy)
        {
            // If the user is internal, return from the Db.
            if (targetUser.IsInternal() || !proxy)
            {
                var query = this.BuildMentionsRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, parameters, proxy);
                
                //// TEMP: Get the JSON for this query.
                //var imq = (query as MongoQueryable<DbPost<object>>).GetMongoQuery();
                //var queryString = imq.ToString();

                ////// you could also just do;
                ////var cursor = this.dbClient.GetPostCollection<object>().FindAs(typeof(DbPost<object>), imq);
                ////var explainDoc = cursor.Explain();

                return query
                    .Take(parameters.Limit.GetValueOrDefault())
                    .ToList()
                    .Select(p =>
                    {
                        // Find the actual mention in the post.
                        var mention = p.BPost.Mentions.FirstOrDefault(m => 
                            m.UserId == targetUser.Id
                            && m.PostId == postId);

                        // Create a new mention based on the post itself.
                        return new ApiMention
                        {
                            Entity = p.Post.Entity,
                            PostId = p.Post.Id,
                            VersionId = p.Post.Version.Id,
                            Type = p.Post.Type,
                            Public = !p.Post.Permissions.Public || (mention != null && !mention.Public.GetValueOrDefault(true))
                                ? (bool?) false
                                : null
                        };
                    });
            }
            
            // If we don't have an authenticated user, return now.
            if (user == null)
            {
                return null;
            }

            // Otherwise, proxy.
            var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
            var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

            return await this.tentClient.RetrieveMentionsForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        }

        public async Task<long> GetMentionsCountAsync(DbUser user, DbUser targetUser, string postId, ITentRequestParameters parameters, bool proxy)
        {
            // If the user is internal, return from the Db.
            if (targetUser.IsInternal() || !proxy)
            {
                return this.BuildMentionsRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, parameters, proxy).Count();
            }
            
            // If we don't have an authenticated user, return now.
            if (user == null)
            {
                return 0;
            }

            // Otherwise, proxy.
            var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
            var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

            return await this.tentClient.RetrieveMentionsCountForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        }

        public async Task<IEnumerable<ApiVersion>> GetVersionsAsync(DbUser user, DbUser targetUser, string postId, ITentRequestParameters parameters, bool proxy)
        {
            // If the user is internal, or if we can't proxy, return from the Db.
            if (targetUser.IsInternal() || !proxy)
            {
                return this.BuildVersionsRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, parameters, proxy)
                    .Skip(parameters.Skip.GetValueOrDefault())
                    .Take(parameters.Limit.GetValueOrDefault())
                    .ToList()
                    .Select(p =>
                    {
                        // Set the UserId for profile retrieval.
                        p.BPost.Version.UserId = p.BPost.UserId;

                        // Set the type.
                        p.BPost.Version.Type = p.BPost.Type;

                        // If the post is not from our target user, add the user info to the version.
                        if (p.BPost.UserId != targetUser.Id)
                        {
                            p.BPost.Version.Entity = p.BPost.Entity;
                        }

                        // If the post isn't the requested post, add the post info to the version.
                        if (p.BPost.Id != postId)
                        {
                            p.BPost.Version.PostId = p.BPost.Id;
                        }

                        return p.BPost.Version;
                    });
            }

            // If we don't have an authenticated user, return now.
            if (user == null)
            {
                return null;
            }

            // Otherwise, proxy.
            var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
            var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

            return await this.tentClient.RetrieveVersionsForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        }

        public async Task<long> GetVersionsCountAsync(DbUser user, DbUser targetUser, string postId, ITentRequestParameters parameters, bool proxy)
        {
            // If the user is internal, or if we can't proxy, return from the Db.
            if (targetUser.IsInternal() || !proxy)
            {
                return this.BuildVersionsRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, parameters, proxy).Count();
            }

            // If we don't have an authenticated user, return now.
            if (user == null)
            {
                return 0;
            }

            // Otherwise, proxy.
            var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
            var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

            return await this.tentClient.RetrieveVersionsCountForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        }

        public async Task<IEnumerable<ApiVersion>> GetVersionsChildrenAsync(DbUser user, DbUser targetUser, string postId, string versionId, ITentRequestParameters parameters, bool proxy)
        {
            // If the user is internal, or if we can't proxy, return from the Db.
            if (targetUser.IsInternal() || !proxy)
            {
                return this.BuildVersionChildrenRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, versionId, parameters, proxy)
                    .Skip(parameters.Skip.GetValueOrDefault())
                    .Take(parameters.Limit.GetValueOrDefault())
                    .ToList()
                    .Select(p =>
                    {
                        // Set the UserId for profile retrieval.
                        p.BPost.Version.UserId = p.BPost.UserId;
                            
                        // Set the type.
                        p.BPost.Version.Type = p.BPost.Type;

                        // If the post is not from our target user, add the user info to the version.
                        if (p.BPost.UserId != targetUser.Id)
                        {
                            p.BPost.Version.Entity = p.BPost.Entity;
                        }

                        // If the post isn't the requested post, add the post info to the version.
                        if (p.BPost.Id != postId)
                        {
                            p.BPost.Version.PostId = p.BPost.Id;
                        }

                        return p.BPost.Version;
                    });
            }

            // If we don't have an authenticated user, return now.
            if (user == null)
            {
                return null;
            }

            // Otherwise, proxy.
            var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
            var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

            return await this.tentClient.RetrieveVersionChildrenForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        }

        public async Task<long> GetVersionsChildrenCountAsync(DbUser user, DbUser targetUser, string postId, string versionId, ITentRequestParameters parameters, bool proxy)
        {
            // If the user is internal, or if we can't proxy, return from the Db.
            if (targetUser.IsInternal() || !proxy)
            {
                return this.BuildVersionChildrenRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, versionId, parameters, proxy).Count();
            }

            // If we don't have an authenticated user, return now.
            if (user == null)
            {
                return 0;
            }

            // Otherwise, proxy.
            var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
            var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

            return await this.tentClient.RetrieveVersionChildrenCountForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        }

        public async Task<IEnumerable<ITentPost<object>>> GetPostsFromReplyChainAsync(DbUser user, DbUser targetUser, string postId, string versionId = null)
        {
            // Retrieve the Reply Chain for this post.
            var replyChain = await this.GetPostReplyChainAsync(user, targetUser, postId, versionId);

            // Retrieve the corresponding posts.
            return this.postRepository.GetBulkPosts<object>(replyChain).Select(p => p.Post).ToList();
        }

        public async Task<long> GetPostsCountFromReplyChainAsync(DbUser user, DbUser targetUser, string postId, string versionId = null)
        {
            // Retrieve the Reply Chain for this post.
            var replyChain = await this.GetPostReplyChainAsync(user, targetUser, postId, versionId);

            // Return the number of items in the Reply Chain.
            return replyChain.Count();
        }

        public async Task<IDbPost<TentContentMeta>> GetMetaPostForUserAsync(DbUser user)
        {
            // Start by trying to retrieve the Meta Post internally.
            var metaPost = this.GetMetaPostForUser(user.Id);
            if (metaPost != null || user.IsInternal())
            {
                return metaPost;
            }

            // If no Meta Post could be found, decide whether we should perform discovery or not.
            if (user.LastDiscoveryAttempt.HasValue && (DateTime.UtcNow - user.LastDiscoveryAttempt.Value) < this.configuration.DiscoveryAttemptTimeout())
            {
                return null;
            }

            // If no Meta Post could be found, try to perform discovery.
            var remoteMetaPost = await this.discoveryService.DiscoverUriAsync<TentContentMeta>(new Uri(user.LastEntity, UriKind.Absolute));
            if (remoteMetaPost != null)
            {
                // TODO: Handle the case where the entity is different from the one we have.

                // Save the post and return.
                return await this.CreatePostAsync(user, remoteMetaPost);
            }

            // If no Meta Post was found through discovery, update the User and return.
            user.LastDiscoveryAttempt = DateTime.UtcNow;
            this.userRepository.UpdateUser(user);

            return null;
        }

        public IDbPost<TentContentMeta> GetMetaPostForUser(ObjectId userId)
        {
            var metaPostType = this.postTypeFactory.FromString(this.tentConstants.MetaPostType());
            return this.postRepository.GetPostLastVersionByType<TentContentMeta>(userId, metaPostType);
        }

        private IEnumerable<IDbPost<TentContentMeta>> GetMetaPostForUsers(IEnumerable<ObjectId> userIds)
        {
            return userIds.Select(this.GetMetaPostForUser);
        }

        public Task<IDictionary<string, ApiMetaProfile>> GetMetaProfileForUserAsync(ObjectId userId)
        {
            return this.GetMetaProfilesForUsersAsync(new[] { userId });
        }

        public Task<IDictionary<string, ApiMetaProfile>> GetMetaProfilesForMentionsAsync(IEnumerable<ApiMention> mentions)
        {
            var userIds = mentions.Select(m => m.UserId);

            // Resolve user ids for mentions who don't have any.
            var entities = mentions.Where(m => m.UserId == default(ObjectId)).Select(m => m.Entity);
            userIds = userIds.Concat(entities
                .Distinct()
                .Select(e => this.userLogic.Value.GetUserId(e).GetValueOrDefault()));

            return this.GetMetaProfilesForUsersAsync(userIds);
        }

        public async Task<IDictionary<string, ApiMetaProfile>> GetMetaProfilesForVersionsAsync(IEnumerable<ApiVersion> versions, RequestProfilesEnum requestedProfiles)
        {
            var userIds = new List<ObjectId>();

            if (requestedProfiles.HasFlag(RequestProfilesEnum.Entity))
            {
                userIds.AddRange(versions.Select(v => v.UserId));

                // Resolve user ids for versions who don't have any.
                var entities = versions.Where(m => m.UserId == default(ObjectId)).Select(m => m.Entity);
                userIds.AddRange(entities.Distinct().Select(e => this.userLogic.Value.GetUserId(e).GetValueOrDefault()));
            }

            if (requestedProfiles.HasFlag(RequestProfilesEnum.Parents))
            {
                var versionParents = versions
                    .Where(v => v.Parents != null)
                    .SelectMany(v => v.Parents)
                    .ToList();

                userIds.AddRange(versionParents.Select(vp => vp.UserId));

                // Resolve user ids for parents who don't have any.
                var entities = versionParents.Where(m => m.UserId == default(ObjectId)).Select(m => m.Entity);
                userIds.AddRange(entities.Distinct().Select(e => this.userLogic.Value.GetUserId(e).GetValueOrDefault()));
            }

            return userIds.Any()
                ? await this.GetMetaProfilesForUsersAsync(userIds)
                : new Dictionary<string, ApiMetaProfile>();
        }

        public Task<IDictionary<string, ApiMetaProfile>> GetMetaProfilesForPostAsync<T>(ITentPost<T> post, IEnumerable<ITentPost<T>> refs, RequestProfilesEnum requestedProfiles) where T : class
        {
            return this.GetMetaProfilesForPostsAsync(new[] {post}, refs, requestedProfiles);
        }

        public async Task<IDictionary<string, ApiMetaProfile>> GetMetaProfilesForPostsAsync<T>(IEnumerable<ITentPost<T>> posts, IEnumerable<ITentPost<T>> refs, RequestProfilesEnum requestedProfiles) where T : class 
        {
            var userIds = new List<ObjectId>();

            if (requestedProfiles.HasFlag(RequestProfilesEnum.Entity))
            {
                userIds.AddRange(posts.Select(p => p.UserId));
            }

            if (requestedProfiles.HasFlag(RequestProfilesEnum.Refs))
            {
                // Add the refs authors.
                userIds.AddRange(posts.Where(p => p.PostRefs != null)
                    .SelectMany(p => p.PostRefs)
                    .Select(pr => pr.UserId));

                // Add the refs mentioned users.
                if (refs != null && requestedProfiles.HasFlag(RequestProfilesEnum.Mentions))
                {
                    userIds.AddRange(refs.Where(p => p.Mentions != null)
                        .SelectMany(p => p.Mentions)
                        .Select(m => m.UserId));
                }
            }

            if (requestedProfiles.HasFlag(RequestProfilesEnum.Mentions))
            {
                userIds.AddRange(posts.Where(p => p.Mentions != null)
                    .SelectMany(p => p.Mentions)
                    .Select(m => m.UserId));
            }

            if (requestedProfiles.HasFlag(RequestProfilesEnum.Permissions))
            {
                // TODO: Add LinkPostVersionPUsers to the PostVersion.
            }

            if (requestedProfiles.HasFlag(RequestProfilesEnum.Parents))
            {
                userIds.AddRange(posts.Where(p => p.Version.Parents != null)
                    .SelectMany(p => p.Version.Parents)
                    .Select(vp => vp.UserId));
            }

            return userIds.Any() 
                ? await this.GetMetaProfilesForUsersAsync(userIds)
                : new Dictionary<string, ApiMetaProfile>();
        }

        public IEnumerable<ITentPost<object>> GetPostRefsForPost<T>(ITentPost<T> post, int maxRefs) where T : class
        {
            Ensure.Argument.IsNotNull(post, "post");
            return this.GetPostRefsForPosts(new[] { post }, maxRefs);
        }

        public IEnumerable<ITentPost<object>> GetPostRefsForPosts<T>(IEnumerable<ITentPost<T>> posts, int maxRefs) where T : class
        {
            Ensure.Argument.IsNotNull(posts, "posts");

            // Select all the PostRefs to retrieve.
            var postRefs = posts
                .Where(p => p.PostRefs != null)
                .Select(p => p.PostRefs.Take(maxRefs))
                .SelectMany(e => e)
                .Where(pr => pr.FoundPost)
                .Select(pr => new TentPostReference
                {
                    UserId = pr.UserId,
                    PostId = pr.PostId,
                    VersionId = pr.VersionId
                })
                .Distinct();

            return this.postRepository.GetBulkPosts<object>(postRefs)
                .Where(p => p != null)
                .Select(p => p.Post);
        }

        public async Task<IDbPost<object>> GetPostWithAttachmentAsync(ObjectId userId, string digest)
        {
            return this.dbClient.GetPostCollection<object>()
                .AsQueryable()
                .FirstOrDefault(p => p.OwnerId == userId
                    && p.LastVersion
                    && p.BPost.UserId == userId
                    && p.BPost.Attachments.Any(a =>
                        a.Digest == digest));
        }

        public async Task<IDbPost<T>> GetLastPostByTypeWithMentionAsync<T>(ObjectId userId, ITentPostType postType, TentPostReference mention) where T : class 
        {
            return this.dbClient.GetPostCollection<T>()
                .AsQueryable()
                .FirstOrDefault(p => p.OwnerId == userId
                    && p.LastVersion
                    && p.BPost.UserId == userId
                    && ((postType.WildCard && p.BPost.Type.StartsWith(postType.ToString()))
                        || (!postType.WildCard && p.BPost.Type == postType.ToString()))
                    && p.BPost.Mentions.Any(m => m.UserId == mention.UserId
                        && (mention.PostId == null || m.PostId == mention.PostId)));
        }

        public async Task<IDbPost<object>> GetSubscribingPostForTypeAsync(ObjectId userId, ObjectId targetUserId, params ITentPostType[] postTypes)
        {
            return this.BuildGetSubscriptionPostsForType(userId, postTypes)
                .FirstOrDefault(p => p.BPost.UserId == userId
                    && p.BPost.Mentions.Any(m =>
                        m.UserId == targetUserId));
        }

        public async Task<IEnumerable<IDbPost<object>>> GetSubscriberPostsForTypeAsync(ObjectId userId, int skip, int take, params ITentPostType[] postTypes)
        {
            return this.BuildGetSubscriptionPostsForType(userId, postTypes)
                .Where(p => p.BPost.UserId != userId)
                .Skip(skip)
                .Take(take)
                .ToList();
        }

        public async Task<int> GetSubscriberPostsCountForTypeAsync(ObjectId userId, params ITentPostType[] postTypes)
        {
            return this.BuildGetSubscriptionPostsForType(userId, postTypes)
                .Count(p => p.BPost.UserId != userId);
        }

        public async Task<IDbPost<object>>  DeletePostAsync(DbUser user, IDbPost<object> post, bool specificVersion, bool createDeletePost = true)
        {
            IDbPost<object> deletedPost = null;

            // If needed, create the corresponding Delete post.
            if (createDeletePost)
            {
                var deletePostMentions = post.Post.Mentions == null
                    ? null
                    : post.Post.Mentions
                        .Where(m => 
                            m.UserId != post.Post.UserId
                            && m.UserId != default(ObjectId))
                        .Select(m => new ApiMention
                        {
                            Entity = m.Entity
                        });

                deletedPost = await this.CreateNewPostAsync<object>(
                    user,
                    this.tentConstants.DeletePostType(),
                    null,
                    post.Post.Permissions.Public,
                    deletePostMentions,
                    new[]
                    {
                        new ApiPostRef
                        {
                            User = user,
                            PostId = post.Post.Id,
                            Type = post.Post.Type,
                            VersionId = specificVersion
                                ? post.Post.Version.Id
                                : null
                        }
                    });
            }

            // Delete the post.
            this.postRepository.DeletePost(post, specificVersion);

            // Return the newly created delete post.
            return deletedPost;
        }

        #endregion

        #region Private methods.

        private IQueryable<DbPost<object>> BuildFeedRequest(ObjectId userId, ITentRequestParameters parameters)
        {
            // Build the base query.
            var query = this.dbClient.GetPostCollection<object>()
                .AsQueryable()
                .Where(p => p.OwnerId == userId && p.LastVersion);

            // Users.
            if (parameters.Users != null)
            {
                var userIds = parameters.Users.Select(u => u == null ? default(ObjectId) : u.Id).ToList();
                query = query.Where(p => userIds.Contains(p.BPost.UserId));
            }
            else if (parameters.OnlyFromFollowings)
            {
                query = query.Where(p => p.FromFollowing);
            }

            return this.BuildCommonRequest(userId, userId, query, parameters);
        }

        private IQueryable<DbPost<object>> BuildPublicationsRequest(ObjectId? userId, ObjectId targetUserId, ITentRequestParameters parameters, bool proxy)
        {
            // Build the base query.
            var query = this.dbClient.GetPostCollection<object>()
                .AsQueryable()
                .Where(p => p.BPost.UserId == targetUserId && p.LastVersion);

            query = !proxy && userId.HasValue
                ? query.Where(p => p.OwnerId == userId.Value)
                : query.Where(p => p.OwnerId == targetUserId);

            return this.BuildCommonRequest(userId, targetUserId, query, parameters);
        }
        
        private IQueryable<DbPost<object>> BuildCommonRequest(ObjectId? userId, ObjectId targetUserId, IQueryable<DbPost<object>> query, ITentRequestParameters parameters)
        {
            // Only non-deleted posts.
            query = query.Where(p => p.DeletedAt == null);

            // Mentions.
            if (parameters.Mentioning != null)
            {
                foreach (var andMentioning in parameters.Mentioning)
                {
                    var userOnlyMentionIds = andMentioning.Where(m => string.IsNullOrEmpty(m.PostId) && m.User != null).Select(m => m.User.Id.ToString()).ToList();
                    var userOnlyMentionEntities = andMentioning.Where(m => string.IsNullOrEmpty(m.PostId) && m.User == null).Select(m => m.Entity).ToList();
                    var postMentions = andMentioning.Where(m => !string.IsNullOrEmpty(m.PostId) && m.User != null).Select(m => m.User.Id.ToString() + m.PostId).ToList();

                    var userOnlyIdRegex = new Regex(string.Format("^({0})", userOnlyMentionIds.Any() 
                        ? string.Join("|", userOnlyMentionIds.Select(Regex.Escape))
                        : "nothing"));
                    var userOnlyEntityRegex = new Regex(string.Format("^({0})$", userOnlyMentionEntities.Any()
                        ? string.Join("|", userOnlyMentionEntities.Select(Regex.Escape))
                        : "nothing"));

                    query = query.Where(p =>
                        p.BPost.Mentions.Any(m =>
                            userOnlyIdRegex.IsMatch(m.MentionId)
                            || userOnlyEntityRegex.IsMatch(m.Entity)
                            || postMentions.Contains(m.MentionId)));
                }
            }

            // Negative mentions.
            if (parameters.NotMentioning != null)
            {
                // TODO: Implement negative mentions.
            }

            // Post types.
            if (parameters.PostTypes != null && parameters.PostTypes.Any())
            {
                var wildcardPostTypes = parameters.PostTypes.Where(t => t.WildCard).Select(t => t.Type).ToList();
                var normalPostTypes = parameters.PostTypes.Where(t => !t.WildCard).Select(t => t.ToString()).ToList();

                if (wildcardPostTypes.Any())
                {
                    var wildcardRegex = new Regex(string.Format("^({0})", string.Join("|", wildcardPostTypes.Select(Regex.Escape))));

                    query = query.Where(p =>
                        wildcardRegex.IsMatch(p.BPost.Type)
                        || normalPostTypes.Contains(p.BPost.Type));
                }
                else
                {
                    query = query.Where(p =>
                        normalPostTypes.Contains(p.BPost.Type));
                }
            }

            // Allowed post types.
            if (parameters.AllowedTypes != null && parameters.AllowedTypes.Any())
            {
                var wildcardPostTypes = parameters.AllowedTypes.Where(t => t.WildCard).Select(t => t.Type).ToList();
                var normalPostTypes = parameters.AllowedTypes.Where(t => !t.WildCard).Select(t => t.ToString()).ToList();

                if (wildcardPostTypes.Any())
                {
                    var wildcardRegex = new Regex(string.Format("^({0})", string.Join("|", wildcardPostTypes.Select(Regex.Escape))));

                    query = query.Where(p =>
                        p.BPost.Permissions.Public
                        || wildcardRegex.IsMatch(p.BPost.Type)
                        || normalPostTypes.Contains(p.BPost.Type));
                }
                else
                {
                    query = query.Where(p =>
                        p.BPost.Permissions.Public
                        || normalPostTypes.Contains(p.BPost.Type));
                }
            }
            
            // Permissions.
            if (targetUserId != userId)
            {
                if (userId.HasValue)
                {
                    query = query.Where(p =>
                        p.BPost.Permissions.Public
                        || p.BPost.Permissions.UserIds.Contains(userId.Value));
                }
                else
                {
                    query = query.Where(p => p.BPost.Permissions.Public);
                }
            }
            
            // TODO: Add version comparison.
            // Condition on date.
            var sortPropertyName = this.GetPostSortPropertyName(parameters.SortBy);
            if (parameters.Since != null && parameters.Since.IsValid)
            {
                if (string.IsNullOrWhiteSpace(parameters.Since.Version))
                {
                    query = query.Where(sortPropertyName + " > @0", parameters.Since.Date.GetValueOrDefault());
                }
                else
                {
                    query = query.Where(sortPropertyName + " >= @0", parameters.Since.Date.GetValueOrDefault());
                    query = query.Where(p => p.BPost.Version.Id != parameters.Since.Version);
                }
            }
            else if (parameters.Until != null && parameters.Until.IsValid)
            {
                if (string.IsNullOrWhiteSpace(parameters.Until.Version))
                {
                    query = query.Where(sortPropertyName + " > @0", parameters.Until.Date.GetValueOrDefault());
                }
                else
                {
                    query = query.Where(sortPropertyName + " >= @0", parameters.Until.Date.GetValueOrDefault());
                    query = query.Where(p => p.BPost.Version.Id != parameters.Until.Version);
                }
            }

            if (parameters.Before != null && parameters.Before.IsValid)
            {
                if (string.IsNullOrWhiteSpace(parameters.Before.Version))
                {
                    query = query.Where(sortPropertyName + " < @0", parameters.Before.Date.GetValueOrDefault());
                }
                else
                {
                    query = query.Where(sortPropertyName + " <= @0", parameters.Before.Date.GetValueOrDefault());
                    query = query.Where(p => p.BPost.Version.Id != parameters.Before.Version);
                }
            }

            // Ordering
            return parameters.Since != null
                ? query.OrderBy(sortPropertyName)
                : query.OrderBy(sortPropertyName + " descending");
        }

        private IQueryable<DbPost<object>> BuildMentionsRequest(ObjectId? userId, ObjectId targetUserId, string postId, ITentRequestParameters parameters, bool proxy)
        {
            // Build the base query.
            var query = this.dbClient.GetPostCollection<object>()
                .AsQueryable()
                .Where(p => p.LastVersion 
                        && p.BPost.Mentions.Any(m =>
                            m.UserId == targetUserId
                            && m.PostId == postId
                            && (m.Public != false
                                || p.BPost.UserId == userId.GetValueOrDefault()
                                || m.UserId == userId.GetValueOrDefault())));

            // Add common conditions and return.
            return this.BuildPostFeedCommonRequest(userId, targetUserId, query, parameters, proxy);
        }

        private IQueryable<DbPost<object>> BuildVersionsRequest(ObjectId? userId, ObjectId targetUserId, string postId, ITentRequestParameters parameters, bool proxy)
        {
            // Build the base query.
            var query = this.dbClient.GetPostCollection<object>()
                .AsQueryable()
                .Where(p => p.BPost.UserId == targetUserId
                    && p.BPost.Id == postId);

            // Add common conditions and return.
            return this.BuildPostFeedCommonRequest(userId, targetUserId, query, parameters, proxy);
        }

        private IQueryable<DbPost<object>> BuildVersionChildrenRequest(ObjectId? userId, ObjectId targetUserId, string postId, string versionId, ITentRequestParameters parameters, bool proxy)
        {
            // Build the base query.
            var query = this.dbClient.GetPostCollection<object>()
                .AsQueryable()
                .Where(p => (p.BPost.Version.Parents.Any(vp =>
                        vp.UserId == targetUserId
                        && vp.PostId == postId
                        && vp.VersionId == versionId))
                    || (p.BPost.Id == postId
                        && p.BPost.Version.Parents.Any(vp =>
                            vp.UserId == targetUserId
                            && vp.PostId == null
                            && vp.VersionId == versionId)));

            // Add common conditions and return.
            return this.BuildPostFeedCommonRequest(userId, targetUserId, query, parameters, proxy);
        }

        private IQueryable<DbPost<object>> BuildPostFeedCommonRequest(ObjectId? userId, ObjectId targetUserId, IQueryable<DbPost<object>> query, ITentRequestParameters parameters, bool proxy)
        {
            // Owner.
            query = !proxy && userId.HasValue
                ? query.Where(p => p.OwnerId == userId.Value)
                : query.Where(p => p.OwnerId == targetUserId);

            // Only non-deleted posts.
            query = query.Where(p => p.DeletedAt == null);

            // Post types.
            if (parameters.PostTypes != null && parameters.PostTypes.Any())
            {
                var wildcardPostTypes = parameters.PostTypes.Where(t => t.WildCard).Select(t => t.Type).ToList();
                var normalPostTypes = parameters.PostTypes.Where(t => !t.WildCard).Select(t => t.ToString()).ToList();

                if (wildcardPostTypes.Any())
                {
                    var wildcardRegex = new Regex(string.Format("^({0})", string.Join("|", wildcardPostTypes.Select(Regex.Escape))));

                    query = query.Where(p =>
                        wildcardRegex.IsMatch(p.BPost.Type)
                        || normalPostTypes.Contains(p.BPost.Type));
                }
                else
                {
                    query = query.Where(p =>
                        normalPostTypes.Contains(p.BPost.Type));
                }
            }

            // Allowed post types.
            if (parameters.AllowedTypes != null && parameters.AllowedTypes.Any())
            {
                var wildcardPostTypes = parameters.AllowedTypes.Where(t => t.WildCard).Select(t => t.Type).ToList();
                var normalPostTypes = parameters.AllowedTypes.Where(t => !t.WildCard).Select(t => t.ToString()).ToList();

                if (wildcardPostTypes.Any())
                {
                    var wildcardRegex = new Regex(string.Format("^({0})", string.Join("|", wildcardPostTypes.Select(Regex.Escape))));

                    query = query.Where(p =>
                        p.BPost.Permissions.Public
                        || wildcardRegex.IsMatch(p.BPost.Type)
                        || normalPostTypes.Contains(p.BPost.Type));
                }
                else
                {
                    query = query.Where(p =>
                        p.BPost.Permissions.Public
                        || normalPostTypes.Contains(p.BPost.Type));
                }
            }

            // Permissions.
            if (targetUserId != userId)
            {
                if (userId.HasValue)
                {
                    query = query.Where(p =>
                        p.BPost.Permissions.Public
                        || p.BPost.Permissions.UserIds.Contains(userId.Value));
                }
                else
                {
                    query = query.Where(p => p.BPost.Permissions.Public);
                }
            }

            // Sort and return.
            return query.OrderByDescending(p => p.BPost.Version.ReceivedAt);
        }

        private string GetPostSortPropertyName(RequestSortByEnum sortBy)
        {
            switch (sortBy)
            {
                case RequestSortByEnum.PublishedDate:
                    return "BPost.PublishedAt";
                case RequestSortByEnum.VersionPublishedDate:
                    return "BPost.Version.PublishedAt";
                case RequestSortByEnum.VersionReceivedDate:
                    return "BPost.Version.ReceivedAt";
                default:
                    return "BPost.ReceivedAt";
            }
        }

        private IQueryable<DbPost<object>> BuildGetSubscriptionPostsForType(ObjectId userId, params ITentPostType[] postTypes)
        {
            var query = this.dbClient.GetPostCollection<object>()
                .AsQueryable()
                .Where(p => p.OwnerId == userId 
                    && p.LastVersion
                    && p.DeletedAt == null);

            // If we don't have a specific post type, retrieve all subscriptions.
            if (postTypes == null || postTypes.Length == 0)
            {
                query = query.Where(p => p.BPost.Type.StartsWith(this.tentConstants.SubscriptionPostType()));
            }
            // Otherwise, narrow our search to subscriptions for the target post type.
            else
            {
                var subscriptionPostTypes = postTypes.Select(t => this.tentConstants.SubscriptionPostType() + t.Type);
                query = query.Where(p => subscriptionPostTypes.Contains(p.BPost.Type));
            }
            
            return query;
        }

        private async Task<IEnumerable<IDbPost<object>>> InjestPosts(IList<ITentPost<object>> posts)
        {
            // Find the users for posts to inject.
            var retrieveUsersTasks = posts
                .Select(p => p.Entity)
                .Distinct()
                .Select(this.userLogic.Value.GetUserAsync)
                .ToList();

            await Task.WhenAll(retrieveUsersTasks);

            var userDictionary = retrieveUsersTasks
                .Where(t => t.Result != null)
                .ToDictionary(t => t.Result.LastEntity, t => t.Result);

            // Save the posts to the db.
            var createPostsTasks = posts
                .Where(p => userDictionary.ContainsKey(p.Entity))
                .Select(p => this.CreatePostAsync(userDictionary[p.Entity], p))
                .ToList();

            await Task.WhenAll(createPostsTasks);

            return createPostsTasks
                .Where(t => t.Result != null)
                .Select(t => t.Result);
        }

        private async Task<IEnumerable<TentPostReference>> GetPostReplyChainAsync(DbUser user, DbUser targetUser, string postId, string versionId = null)
        {
            // Retrieve the specified post.
            var post = await this.GetPostAsync(user, targetUser, postId, versionId);
            if (post == null)
            {
                throw new PostNotFoundException();
            }

            var result = new List<TentPostReference>();

            // If this post doesn't have any mentions, return an empty list.
            ApiMention firstMention;
            if (post.Post.Mentions == null ||
                (firstMention = post.Post.Mentions.FirstOrDefault(m => m.FoundPost)) == null)
            {
                return result;
            }

            // Create the Reply Chain item for this mention.
            result.Add(new TentPostReference
            {
                UserId = firstMention.UserId,
                PostId = firstMention.PostId,
                VersionId = firstMention.VersionId
            });

            // If this post's first mention doesn't have a Reply Chain, return only the first mention.
            if (firstMention.ReplyChain == null || !firstMention.ReplyChain.Any())
            {
                return result;
            }

            // Otherwise, return the whole Reply Chain.
            result.AddRange(firstMention.ReplyChain);

            return result;
        }

        private async Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForUsersAsync(IEnumerable<string> userIds)
        {
            // Retrieve the corresponding Meta Posts for those UserIds.
            var metaPosts = this.GetMetaPostForUsers(userIds
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct());
            var metaProfiles = new Dictionary<string, ApiMetaProfile>();

            foreach (var metaPost in metaPosts)
            {
                // Skip null meta posts.
                if (metaPost == null)
                {
                    continue;
                }

                // Extract the ApiProfile from the meta post.
                if (metaPost.Post.Content == null
                    || metaPost.Post.Content.Profile == null)
                {
                    metaProfiles[metaPost.Post.Entity] = new ApiMetaProfile();
                    continue;
                }

                var metaProfile = metaPost.Post.Content.Profile;

                // Try to find the avatar in the meta post's attachments.
                if (metaPost.Post.Attachments != null)
                {
                    var firstAttachment = metaPost.Post.Attachments.FirstOrDefault(a => a.Category == "avatar");
                    if (firstAttachment != null)
                    {
                        metaProfile.AvatarDigest = firstAttachment.Digest;
                    }
                }

                // Return a dictionary with the entities as key.
                metaProfiles[metaPost.Post.Entity] = metaProfile;
            }

            return metaProfiles;
        }

        private async Task ResolveMentionsAsync<T>(User user, TentPost<T> post) where T : class 
        {
            if (post.Mentions == null || !post.Mentions.Any())
            {
                post.Mentions = null;
                return;
            }

            // Create a task for each mention.
            var resolveMentionsTasks = post.Mentions.Select(async mention =>
            {
                try
                {
                    // If this user already has a UserId, return directly.
                    if (mention.User != null)
                    {
                        mention.UserId = mention.User.Id;
                        if (mention.UserId != user.Id)
                        {
                            mention.Entity = this.modelHelpers.GetUserEntity(mention.User);
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(mention.Entity))
                    {
                        mention.UserId = user.Id;
                        mention.User = user;
                    }
                    else
                    {
                        // Retrieve the user for this mention.
                        mention.User = await this.userLogic.Value.GetUserAsync(mention.Entity);
                        if (mention.User == null)
                        {
                            return;
                        }

                        mention.UserId = mention.User.Id;
                    }

                    // Remove entity if duplicate.
                    if (mention.UserId == user.Id)
                    {
                        mention.Entity = null;
                    }

                    // Retrieve the post for this mention, if any.
                    if (!string.IsNullOrEmpty(mention.PostId))
                    {
                        var credentialsPost = post as ITentPost<TentContentCredentials>;
                        mention.Post = await this.GetPostAsync(user, mention.User, mention.PostId, mention.VersionId, CacheControlValue.ProxyIfMiss, credentialsPost);
                    }
                    else if (mention.Post != null)
                    {
                        mention.PostId = mention.Post.Post.Id;
                        mention.Type = mention.Post.Post.Type;
                    }
                    
                    // If we don't have a post at this point, no need to continue.
                    if (mention.Post == null)
                    {
                        return;
                    }
                    
                    mention.FoundPost = true;

                    // Reply chain.
                    if (mention.Post.Post.Mentions != null)
                    {
                        var firstMention = mention.Post.Post.Mentions.FirstOrDefault(m => m.FoundPost);
                        if (firstMention != null)
                        {
                            mention.ReplyChain = firstMention.ReplyChain == null
                                ? new List<TentPostReference>()
                                : new List<TentPostReference>(firstMention.ReplyChain);

                            mention.ReplyChain.Insert(0, new TentPostReference
                            {
                                UserId = firstMention.UserId,
                                PostId = firstMention.PostId,
                                VersionId = firstMention.VersionId
                            });
                        }
                    }
                }
                catch
                {
                }
            }).ToList();

            // Wait for all of these tasks to complete, and return their resulting Mentions.
            await Task.WhenAll(resolveMentionsTasks);
        }

        private async Task ResolvePostRefsAsync<T>(User user, TentPost<T> post) where T : class 
        {
            if (post.Refs == null || !post.Refs.Any())
            {
                post.Refs = null;
                return;
            }

            // Create a task for each PostRef.
            var resolvePostRefsTasks = post.PostRefs.Select(async postRef =>
            {
                try
                {
                    // Retrieve the user for this PostRef.
                    if (postRef.User != null)
                    {
                        postRef.UserId = postRef.User.Id;
                        if (postRef.UserId != user.Id)
                        {
                            postRef.Entity = this.modelHelpers.GetUserEntity(postRef.User);
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(postRef.Entity))
                    {
                        postRef.UserId = user.Id;
                        postRef.User = user;
                    }
                    else
                    {
                        postRef.User = await this.userLogic.Value.GetUserAsync(postRef.Entity);
                        if (postRef.User == null)
                        {
                            return;
                        }

                        postRef.UserId = postRef.User.Id;
                    }

                    // Remove entity if duplicate.
                    if (postRef.UserId == user.Id)
                    {
                        postRef.Entity = null;
                    }

                    // Retrieve the post for this PostRef.
                    if (postRef.Post == null)
                    {
                        postRef.Post = await this.GetPostAsync(user, postRef.User, postRef.PostId, postRef.VersionId);
                    }
                    else
                    {
                        postRef.Type = postRef.Post.Post.Type;
                    }

                    if (postRef.Post == null)
                    {
                        return;
                    }
                    
                    postRef.FoundPost = true;
                    if (postRef.PostId == post.Id)
                    {
                        postRef.PostId = null;
                    }
                }
                catch
                {
                }
            }).ToList();

            // Wait for all of these tasks to complete, and return their resulting PostRefs.
            await Task.WhenAll(resolvePostRefsTasks);
        }

        private async Task ResolveParentsAsync<T>(User user, TentPost<T> post) where T : class
        {
            if (post.Version.Parents == null || !post.Version.Parents.Any())
            {
                post.Version.Parents = null;
                return;
            }

            // Create a task for each Parent.
            var resolveParentsTasks = post.Version.Parents.Select(async parent =>
            {
                try
                {
                    // Retrieve the user for this Parent.
                    if (parent.User != null)
                    {
                        parent.UserId = parent.User.Id;
                        if (parent.UserId != user.Id)
                        {
                            parent.Entity = this.modelHelpers.GetUserEntity(parent.User);
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(parent.Entity))
                    {
                        parent.UserId = user.Id;
                        parent.User = user;
                    }
                    else
                    {
                        parent.User = await this.userLogic.Value.GetUserAsync(parent.Entity);
                        if (parent.User == null)
                        {
                            return;
                        }

                        parent.UserId = parent.User.Id;
                    }

                    // Remove the entity if duplicate.
                    if (parent.UserId == user.Id)
                    {
                        parent.Entity = null;
                    }

                    // Retrieve the post for this Parent.
                    if (parent.Post == null)
                    {
                        var parentPostId = string.IsNullOrWhiteSpace(parent.PostId)
                            ? post.Id
                            : parent.PostId;
                        parent.Post = await this.GetPostAsync(user, parent.User, parentPostId, parent.VersionId);
                    }

                    if (parent.Post == null)
                    {
                        return;
                    }

                    parent.FoundPost = true;
                    parent.VersionId = parent.Post.Post.Version.Id;

                    parent.PostId = parent.Post.Post.Id == post.Id
                        ? null
                        : parent.Post.Post.Id;
                }
                catch
                {
                }
            }).ToList();

            // Wait for all of these tasks to complete, and return their resulting PostRefs.
            await Task.WhenAll(resolveParentsTasks);
        }

        private async Task ResolvePermissionsAsync(User user, TentPost<object> post)
        {
            if (post.Permissions == null)
            {
                post.Permissions = new TentPermissions
                {
                    Public = true
                };
                return;
            }

            // If this post is public, no need to go farther.
            if (post.Permissions.Public.GetValueOrDefault())
            {
                post.Permissions.Entities = null;
                post.Permissions.Groups = null;
                return;
            }

            // Resolve User Ids.
            if (post.Permissions.Entities != null)
            {
                var retrieveUsersTasks = post.Permissions.Entities
                    .Distinct()
                    .Select(u => this.userLogic.Value.GetUserAsync(u))
                    .ToList();

                await Task.WhenAll(retrieveUsersTasks);

                post.Permissions.UserIds = retrieveUsersTasks
                    .Where(t => t.Result != null)
                    .Select(t => t.Result.Id)
                    .ToList();
            }

            // Add mentioned users to the list of entities.
            if (post.Mentions != null && post.Mentions.Any())
            {
                var entities = post.Permissions.Entities ?? new List<string>();
                var userIds = post.Permissions.UserIds ?? new List<string>();

                post.Permissions.Entities = entities
                    .Concat(post.Mentions.Where(m => m.User != null && m.User.Id != user.Id).Select(m => this.modelHelpers.GetUserEntity(m.User)))
                    .Distinct()
                    .ToList();

                post.Permissions.UserIds = userIds
                    .Concat(post.Mentions.Where(m => m.User != null && m.User.Id != user.Id).Select(m => m.User.Id))
                    .Distinct()
                    .ToList();
            }

            // TODO: Process permissions groups.
        }

        #endregion
    }
}