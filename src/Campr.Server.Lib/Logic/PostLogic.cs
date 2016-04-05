using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Connectors.Queues;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Exceptions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Other.Factories;
using Campr.Server.Lib.Models.Queues;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;
using Campr.Server.Lib.Net.Tent;
using Campr.Server.Lib.Repositories;
using Campr.Server.Lib.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Campr.Server.Lib.Logic
{
    class PostLogic : IPostLogic
    {
        #region Constructor & Dependencies.

        public PostLogic(IAttachmentLogic attachmentLogic,
            IPostRepository postRepository,
            IUserRepository userRepository,
            ITentQueues tentQueues,
            ITentClient tentClient,
            IDiscoveryService discoveryService,
            ITentPostFactory postFactory,
            ITentPostTypeFactory postTypeFactory,
            ITextHelpers textHelpers,
            IModelHelpers modelHelpers,
            ICryptoHelpers cryptoHelpers,
            ITentConstants tentConstants,
            IGeneralConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            Ensure.Argument.IsNotNull(attachmentLogic, "attachmentLogic");
            Ensure.Argument.IsNotNull(postRepository, "postRepository");
            Ensure.Argument.IsNotNull(userRepository, "userRepository");
            Ensure.Argument.IsNotNull(tentQueues, "tentQueues");
            Ensure.Argument.IsNotNull(discoveryService, "discoveryService");
            Ensure.Argument.IsNotNull(postFactory, nameof(postFactory));
            Ensure.Argument.IsNotNull(postTypeFactory, "postTypeFactory");
            Ensure.Argument.IsNotNull(textHelpers, "textHelpers");
            Ensure.Argument.IsNotNull(modelHelpers, "modelHelpers");
            Ensure.Argument.IsNotNull(cryptoHelpers, "cryptoHelpers");
            Ensure.Argument.IsNotNull(tentConstants, "tentConstants");
            Ensure.Argument.IsNotNull(configuration, "configuration");
            Ensure.Argument.IsNotNull(serviceProvider, nameof(serviceProvider));

            this.attachmentLogic = attachmentLogic;
            this.postRepository = postRepository;
            this.userRepository = userRepository;
            this.tentQueues = tentQueues;
            this.tentClient = tentClient;
            this.discoveryService = discoveryService;
            this.postFactory = postFactory;
            this.postTypeFactory = postTypeFactory;
            this.textHelpers = textHelpers;
            this.modelHelpers = modelHelpers;
            this.cryptoHelpers = cryptoHelpers;
            this.tentConstants = tentConstants;
            this.configuration = configuration;

            // We have to use this *bad* strategy for UserLogic because of the circular dependency.
            this.userLogic = new Lazy<IUserLogic>(serviceProvider.GetService<IUserLogic>);
            //this.followLogic = new Lazy<IFollowLogic>(() => container.Resolve<IFollowLogic>());
            //this.typeSpecificLogic = new Lazy<ITypeSpecificLogic>(() => container.Resolve<ITypeSpecificLogic>());
            //this.appPostLogic = new Lazy<IAppPostLogic>(() => container.Resolve<IAppPostLogic>());
        }

        private readonly IAttachmentLogic attachmentLogic;
        private readonly IPostRepository postRepository;
        private readonly IUserRepository userRepository;
        private readonly IUserPostRepository userPostRepository;
        private readonly ITentQueues tentQueues;
        private readonly ITentClient tentClient;
        private readonly IDiscoveryService discoveryService;
        private readonly ILoggingService loggingService;
        private readonly ITentPostFactory postFactory;
        private readonly ITentPostTypeFactory postTypeFactory;
        private readonly IUserPostFactory userPostFactory;
        private readonly ITextHelpers textHelpers;
        private readonly IModelHelpers modelHelpers;
        private readonly ICryptoHelpers cryptoHelpers;
        private readonly ITentConstants tentConstants;
        private readonly IGeneralConfiguration configuration;

        private readonly Lazy<IUserLogic> userLogic;
        //private readonly Lazy<IFollowLogic> followLogic;
        //private readonly Lazy<ITypeSpecificLogic> typeSpecificLogic;
        //private readonly Lazy<IAppPostLogic> appPostLogic;

        #endregion

        #region Interface implementation.

        public async Task<TentPost<T>> GetPostAsync<T>(User requester, User feedOwner, User user, string postId, string versionId = null, TentPost<TentContentCredentials> credentials = null, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            // If the feed owner is the requester, don't proxy.
            if (requester.Id == feedOwner.Id)
            {
                // Make sure we have a User Post for the requested Post.
                var userPost = await this.userPostRepository.GetAsync(feedOwner.Id, user.Id, postId, versionId, cancellationToken);
                if (userPost == null)
                    return null;
            }
            // The feed owner needs to be either the requester or the target user.
            else if (feedOwner.Id != user.Id)
            {
                return null;
            }

            // Try and retrieve this post internally.
            var internalPost = await this.postRepository.GetAsync<T>(user.Id, postId, cancellationToken);

            // If a post was found, check if our user can access it.
            if (internalPost != null 
                && requester.Id != user.Id 
                && !internalPost.Permissions.Public.GetValueOrDefault() 
                && !internalPost.Permissions.UserIds.Contains(requester.Id))
                return null;

            // If a post was found at this point, or if the user is internal, return.
            if (internalPost != null || user.IsInternal())
                return internalPost;

            // Otherwise, try and retrieve it externally.
            var metaPost = await this.GetMetaPostAsync(user, cancellationToken);
            //            var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser, false, credentialsPost);
            //
            //            var externalPost = await this.tentClient.RetrievePostForUserAsync<object>(metaPost, credentials, postId, versionId);
            //            var externalDbPost = await this.CreatePostAsync(targetUser, externalPost);
            //
            //            return externalDbPost;
            return null;
        }

        public async Task<TentPost<TentContentCredentials>> CreateNewCredentialsPostAsync(
            User user,
            User targetUser,
            TentPost<object> targetPost)
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
            var credentialsPost = this.postFactory.FromContent(user, credentialsContent, this.tentConstants.CredentialsPostType)
                .WithMentions(new[] { postMention })
                .Post();

            credentialsPost = await this.CreatePostAsync(user, credentialsPost);

            // Set the credentials post as passenger on the target post.
            targetPost.PassengerCredentials = credentialsPost;

            return credentialsPost;
        }

        public async Task<TentPost<T>> CreatePostAsync<T>(
            User user,
            TentPost<T> post,
            CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            // Validate parameters.
            Ensure.Argument.IsNotNull(user, nameof(user));
            Ensure.Argument.IsNotNull(post, nameof(post));

            if (post.Version == null)
                throw new VersionMissingException();

            TentPost<T> lastPostVersion = null;

            // If the post already has an Id, try to retrieve existing versions of this post.
            if (!string.IsNullOrEmpty(post.Id))
                // If a VersionId was specified, try to retrieve this exact post version.
                if (!string.IsNullOrWhiteSpace(post.Version?.Id))
                {
                    var existingPost = await this.postRepository.GetAsync<T>(user.Id, post.Id, post.Version.Id, cancellationToken);

                    // If one was found, no need to create a new post.
                    if (existingPost != null)
                        return existingPost;
                }
                // Otherwise, just retrieve the last published version.
                else
                {
                    lastPostVersion = await this.postRepository.GetAsync<T>(user.Id, post.Id, cancellationToken);
                }
            // Otherwise, generate a new Id for this post.
            else
                post.Id = this.textHelpers.GenerateUniqueId();

            // Conform date properties.
            if (lastPostVersion != null)
            {
                post.ReceivedAt = lastPostVersion.ReceivedAt;
                post.PublishedAt = lastPostVersion.PublishedAt;
            }

            // Resolve the mentions, post refs and permissions.
            await this.ResolveMentionsAsync(user, post, cancellationToken);
            await Task.WhenAll(
                this.ResolvePostRefsAsync(user, post, cancellationToken),
                this.ResolveParentsAsync(user, post, cancellationToken),
                this.ResolvePermissionsAsync(user, post, cancellationToken));

            // Process attachments.
            if (user.IsInternal())
            {
                // Only keep attachments that existed on the last Version.
                if (post.Attachments != null && post.Attachments.Any())
                {
                    // TODO: Handle attachments.
                    //post.Attachments = post.Attachments
                    //    .Select(a =>
                    //    {
                    //        if (a.Data != null)
                    //            return a;

                    //        if (lastPostVersion != null && lastPostVersion.Post.Attachments != null)
                    //        {
                    //            return lastPostVersion.Post.Attachments.FirstOrDefault(la =>
                    //                la.Digest == a.Digest
                    //                && la.Name == a.Name
                    //                && la.Category == a.Category
                    //                && la.ContentType == a.ContentType);
                    //        }

                    //        return null;
                    //    })
                    //    .ToList();

                    //// Upload new attachments.
                    //await Task.WhenAll(post.Attachments
                    //    .Where(a => a.Data != null)
                    //    .Select(a => this.attachmentLogic.SaveAttachment(a.Data, a.Digest)));
                }
            }

            // TODO.
            //// Specific actions.
            //if (!await this.typeSpecificLogic.Value.SpecificActionCreatePostAsync(user, post, import))
            //{
            //    return null;
            //}

            // Compute the version Id for our post.
            post.Version.Id = this.modelHelpers.GetVersionIdFromPost(post);

            // Save the post to the Db.
            await this.postRepository.UpdateAsync(post, cancellationToken);

            // If this post was from an external user, we're done here.
            // If we were instructed not to propagate the post, we're done too.
            if (!user.IsInternal())
                return post;

            // TODO: Implement app notifications.
            //// If needed, propagate to apps.
            //var postType = this.postTypeFactory.FromString(dbPost.Post.Type);
            //var appCountForType = await this.appPostLogic.Value.GetAppPostsCountForTypeAsync(dbPost.Post.UserId, postType, dbPost.Post.Permissions.Public);
            //if (appCountForType > 0)
            //{
            //    await this.tentQueues.AppNotifications.AddMessageAsync(new QueueAppNotificationMessage
            //    {
            //        OwnerId = dbPost.Post.UserId,
            //        UserId = dbPost.Post.UserId,
            //        PostId = dbPost.Post.Id,
            //        VersionId = dbPost.Post.Version.Id
            //    });
            //}

            // If this post mentions other users, queue it for propagation.
            if (post.Mentions != null && post.Mentions.Any(m =>
                !string.IsNullOrWhiteSpace(m.UserId) && m.UserId != post.UserId))
            {
                await this.tentQueues.Mentions.AddMessageAsync(new QueueMentionMessage
                {
                    UserId = post.UserId,
                    PostId = post.Id,
                    VersionId = post.Version.Id
                }, cancellationToken);
            }

            // TODO: Do that in a worker.
            //// Propagate to subscriptions.
            //ITentPostType[] subscriptionPostTypes;
            //if (dbPost.Post.Type.StartsWith(this.tentConstants.DeletePostType()))
            //{
            //    subscriptionPostTypes = dbPost.Post.PostRefs
            //        .Where(p => p.Post != null)
            //        .Select(p => this.postTypeFactory.FromString(p.Post.Post.Type))
            //        .ToArray();
            //}
            //else
            //{
            //    subscriptionPostTypes = new[] { this.postTypeFactory.FromString(dbPost.Post.Type) };
            //}

            //var subscriptionsCount = await this.GetSubscriberPostsCountForTypeAsync(user.Id, subscriptionPostTypes);
            //for (var subscriptionIndex = 0; subscriptionIndex < subscriptionsCount; subscriptionIndex += this.configuration.SubscriptionsBatchSize())
            //{
            //    await this.tentQueues.Subscriptions.AddMessageAsync(new QueueSubscriptionMessage
            //    {
            //        UserId = user.Id,
            //        PostId = dbPost.Post.Id,
            //        VersionId = dbPost.Post.Version.Id,
            //        Skip = subscriptionIndex * this.configuration.SubscriptionsBatchSize(),
            //        Take = this.configuration.SubscriptionsBatchSize()
            //    });

            //    // TODO: This could be converted to Fire & Forget to speed up response time (will only be an issue when people start having very large subscriber bases.
            //}

            return post;
        }

        public async Task CreateUserPostAsync<T>(User owner, TentPost<T> post, bool? isFromFollowing = null) where T : class
        {
            //// If needed, check if our user is a subscriber.
            //// TODO
            //if (!isFromFollowing.Value.HasValue)
            //{
            //    var subscription = await this.GetSubscribingPostForTypeAsync(userId, post.Post.UserId, postType);
            //    isSubscriber = subscription != null;
            //}

            // TODO: Decide if any sort of validation should be performed here.
            //// If our user isn't a subscriber, check for mentions.
            //if (!isSubscriber.Value
            //    && (post.Post.Mentions == null
            //        || post.Post.Mentions.All(m => m.UserId != userId)))
            //{
            //    return;
            //}

            // Create the feed item and save it to the db.
            await this.userPostRepository.UpdateAsync(owner.Id, post, isFromFollowing.GetValueOrDefault());

            // TODO.
            //// If needed, propagate to apps.
            //var appCountForType = await this.appPostLogic.Value.GetAppPostsCountForTypeAsync(userId, postType, post.Post.Permissions.Public);
            //if (appCountForType > 0)
            //{
            //    await this.tentQueues.AppNotifications.AddMessageAsync(new QueueAppNotificationMessage
            //    {
            //        OwnerId = userId,
            //        UserId = post.Post.UserId,
            //        PostId = post.Post.Id,
            //        VersionId = post.Post.Version.Id
            //    });
            //}
        }

        //public async Task<TentPost<T>> ImportPostFromLinkAsync<T>(User user, User targetUser, Uri uri) where T : class
        //{
        //    // Retrieve the post.
        //    var externalPost = await this.tentClient.RetrievePostAtUriAsync<T>(uri);
        //    if (externalPost == null)
        //        return null;

        //    // Save this post to the Db.
        //    externalPost = await this.CreatePostAsync(targetUser, externalPost);

        //    // Add a feed item to the post for our user.
        //    await this.CreateUserPostAsync(user, externalPost);

        //    return externalPost;
        //}

        public async Task<IList<TentPost<T>>> GetPostsAsync<T>(User requester, User feedOwner, ITentFeedRequest feedRequest, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            // If the target user isn't the feed owner, update the request.
            if (requester.Id != feedOwner.Id)
                feedRequest.ReplaceUsers(feedOwner);

            // If the user is external, proxy the request.
            if (!feedOwner.IsInternal())
            {
                // TODO.
            }

            // Otherwise, perform the request internally.
            var userPosts = await this.userPostRepository.GetAsync(requester.Id, feedOwner.Id, feedRequest, cancellationToken);

            // Fetch the corresponding posts and return.
            return await this.postRepository.GetAsync<T>(userPosts.Cast<ITentPostIdentifier>().ToList(), cancellationToken);
        }

        public Task<long> GetCountAsync(User requester, User feedOwner, ITentFeedRequest feedRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            // If the target user isn't the feed owner, update the request.
            if (requester.Id != feedOwner.Id)
                feedRequest.ReplaceUsers(feedOwner);

            // If the user is external, proxy the request.
            if (!feedOwner.IsInternal())
            {
                // TODO.
            }

            // Otherwise, perform the request internally.
            return this.userPostRepository.CountAsync(requester.Id, feedOwner.Id, feedRequest, cancellationToken);
        }

        //public async Task<IEnumerable<ApiMention>> GetMentionsAsync(DbUser user, DbUser targetUser, string postId, ITentRequestParameters parameters, bool proxy)
        //{
        //    // If the user is internal, return from the Db.
        //    if (targetUser.IsInternal() || !proxy)
        //    {
        //        var query = this.BuildMentionsRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, parameters, proxy);

        //        //// TEMP: Get the JSON for this query.
        //        //var imq = (query as MongoQueryable<DbPost<object>>).GetMongoQuery();
        //        //var queryString = imq.ToString();

        //        ////// you could also just do;
        //        ////var cursor = this.dbClient.GetPostCollection<object>().FindAs(typeof(DbPost<object>), imq);
        //        ////var explainDoc = cursor.Explain();

        //        return query
        //            .Take(parameters.Limit.GetValueOrDefault())
        //            .ToList()
        //            .Select(p =>
        //            {
        //                // Find the actual mention in the post.
        //                var mention = p.BPost.Mentions.FirstOrDefault(m =>
        //                    m.UserId == targetUser.Id
        //                    && m.PostId == postId);

        //                // Create a new mention based on the post itself.
        //                return new ApiMention
        //                {
        //                    Entity = p.Post.Entity,
        //                    PostId = p.Post.Id,
        //                    VersionId = p.Post.Version.Id,
        //                    Type = p.Post.Type,
        //                    Public = !p.Post.Permissions.Public || (mention != null && !mention.Public.GetValueOrDefault(true))
        //                        ? (bool?)false
        //                        : null
        //                };
        //            });
        //    }

        //    // If we don't have an authenticated user, return now.
        //    if (user == null)
        //    {
        //        return null;
        //    }

        //    // Otherwise, proxy.
        //    var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
        //    var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

        //    return await this.tentClient.RetrieveMentionsForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        //}

        //public async Task<long> GetMentionsCountAsync(DbUser user, DbUser targetUser, string postId, ITentRequestParameters parameters, bool proxy)
        //{
        //    // If the user is internal, return from the Db.
        //    if (targetUser.IsInternal() || !proxy)
        //    {
        //        return this.BuildMentionsRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, parameters, proxy).Count();
        //    }

        //    // If we don't have an authenticated user, return now.
        //    if (user == null)
        //    {
        //        return 0;
        //    }

        //    // Otherwise, proxy.
        //    var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
        //    var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

        //    return await this.tentClient.RetrieveMentionsCountForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        //}

        //public async Task<IEnumerable<ApiVersion>> GetVersionsAsync(DbUser user, DbUser targetUser, string postId, ITentRequestParameters parameters, bool proxy)
        //{
        //    // If the user is internal, or if we can't proxy, return from the Db.
        //    if (targetUser.IsInternal() || !proxy)
        //    {
        //        return this.BuildVersionsRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, parameters, proxy)
        //            .Skip(parameters.Skip.GetValueOrDefault())
        //            .Take(parameters.Limit.GetValueOrDefault())
        //            .ToList()
        //            .Select(p =>
        //            {
        //                // Set the UserId for profile retrieval.
        //                p.BPost.Version.UserId = p.BPost.UserId;

        //                // Set the type.
        //                p.BPost.Version.Type = p.BPost.Type;

        //                // If the post is not from our target user, add the user info to the version.
        //                if (p.BPost.UserId != targetUser.Id)
        //                {
        //                    p.BPost.Version.Entity = p.BPost.Entity;
        //                }

        //                // If the post isn't the requested post, add the post info to the version.
        //                if (p.BPost.Id != postId)
        //                {
        //                    p.BPost.Version.PostId = p.BPost.Id;
        //                }

        //                return p.BPost.Version;
        //            });
        //    }

        //    // If we don't have an authenticated user, return now.
        //    if (user == null)
        //    {
        //        return null;
        //    }

        //    // Otherwise, proxy.
        //    var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
        //    var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

        //    return await this.tentClient.RetrieveVersionsForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        //}

        //public async Task<long> GetVersionsCountAsync(DbUser user, DbUser targetUser, string postId, ITentRequestParameters parameters, bool proxy)
        //{
        //    // If the user is internal, or if we can't proxy, return from the Db.
        //    if (targetUser.IsInternal() || !proxy)
        //    {
        //        return this.BuildVersionsRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, parameters, proxy).Count();
        //    }

        //    // If we don't have an authenticated user, return now.
        //    if (user == null)
        //    {
        //        return 0;
        //    }

        //    // Otherwise, proxy.
        //    var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
        //    var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

        //    return await this.tentClient.RetrieveVersionsCountForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        //}

        //public async Task<IEnumerable<ApiVersion>> GetVersionsChildrenAsync(DbUser user, DbUser targetUser, string postId, string versionId, ITentRequestParameters parameters, bool proxy)
        //{
        //    // If the user is internal, or if we can't proxy, return from the Db.
        //    if (targetUser.IsInternal() || !proxy)
        //    {
        //        return this.BuildVersionChildrenRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, versionId, parameters, proxy)
        //            .Skip(parameters.Skip.GetValueOrDefault())
        //            .Take(parameters.Limit.GetValueOrDefault())
        //            .ToList()
        //            .Select(p =>
        //            {
        //                // Set the UserId for profile retrieval.
        //                p.BPost.Version.UserId = p.BPost.UserId;

        //                // Set the type.
        //                p.BPost.Version.Type = p.BPost.Type;

        //                // If the post is not from our target user, add the user info to the version.
        //                if (p.BPost.UserId != targetUser.Id)
        //                {
        //                    p.BPost.Version.Entity = p.BPost.Entity;
        //                }

        //                // If the post isn't the requested post, add the post info to the version.
        //                if (p.BPost.Id != postId)
        //                {
        //                    p.BPost.Version.PostId = p.BPost.Id;
        //                }

        //                return p.BPost.Version;
        //            });
        //    }

        //    // If we don't have an authenticated user, return now.
        //    if (user == null)
        //    {
        //        return null;
        //    }

        //    // Otherwise, proxy.
        //    var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
        //    var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

        //    return await this.tentClient.RetrieveVersionChildrenForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        //}

        //public async Task<long> GetVersionsChildrenCountAsync(DbUser user, DbUser targetUser, string postId, string versionId, ITentRequestParameters parameters, bool proxy)
        //{
        //    // If the user is internal, or if we can't proxy, return from the Db.
        //    if (targetUser.IsInternal() || !proxy)
        //    {
        //        return this.BuildVersionChildrenRequest(user == null ? (ObjectId?)null : user.Id, targetUser.Id, postId, versionId, parameters, proxy).Count();
        //    }

        //    // If we don't have an authenticated user, return now.
        //    if (user == null)
        //    {
        //        return 0;
        //    }

        //    // Otherwise, proxy.
        //    var targetUserMetaPost = await this.GetMetaPostForUserAsync(targetUser);
        //    var credentials = await this.followLogic.Value.GetCredentialsForUser(user, targetUser);

        //    return await this.tentClient.RetrieveVersionChildrenCountForUserAsync(targetUserMetaPost.Post, parameters, credentials, postId);
        //}

        //public async Task<IList<TentPost<object>>> GetPostsFromReplyChainAsync(User user, User targetUser, string postId, string versionId, CancellationToken cancellationToken)
        //{
        //    // Retrieve the Reply Chain for this post.
        //    var replyChain = await this.GetPostReplyChainAsync(user, targetUser, postId, versionId, cancellationToken);

        //    // Retrieve the corresponding posts.
        //    return await this.postRepository.GetBulkAsync<object>(replyChain, cancellationToken);
        //}

        //public async Task<long> GetPostsCountFromReplyChainAsync(DbUser user, DbUser targetUser, string postId, string versionId = null)
        //{
        //    // Retrieve the Reply Chain for this post.
        //    var replyChain = await this.GetPostReplyChainAsync(user, targetUser, postId, versionId);

        //    // Return the number of items in the Reply Chain.
        //    return replyChain.Count();
        //}

        public async Task<TentPost<TentContentMeta>> GetMetaPostAsync(User user, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Start by trying to retrieve the Meta Post internally.
            var metaPost = await this.GetMetaPostNoProxyAsync(user.Id, cancellationToken);
            if (metaPost != null || user.IsInternal())
                return metaPost;

            // If no Meta Post could be found, decide whether we should perform discovery or not.
            if (user.LastDiscoveryAttempt.HasValue
                && DateTime.UtcNow - user.LastDiscoveryAttempt.Value < this.configuration.DiscoveryAttemptTimeout)
                return null;

            // If no Meta Post could be found, try to perform discovery.
            var remoteMetaPost = await this.discoveryService.DiscoverUriAsync<TentContentMeta>(new Uri(user.Entity, UriKind.Absolute));
            if (remoteMetaPost != null)
            {
                // TODO: Handle the case where the entity is different from the one we have.

                // Save the post and return.
                return await this.CreatePostAsync(user, remoteMetaPost, cancellationToken);
            }

            // If no Meta Post was found through discovery, update the User and return.
            user.LastDiscoveryAttempt = DateTime.UtcNow;
            await this.userRepository.UpdateAsync(user, cancellationToken);

            return null;
        }

        private Task<TentPost<TentContentMeta>> GetMetaPostNoProxyAsync(string userId, CancellationToken cancellationToken)
        {
            return this.postRepository.GetLastVersionOfTypeAsync<TentContentMeta>(userId, this.tentConstants.MetaPostType, cancellationToken);
        }

        private async Task<IList<TentPost<TentContentMeta>>> GetMetaPostsNoProxyAsync(IEnumerable<string> userIds, CancellationToken cancellationToken)
        {
            var getMetaPostTasks = userIds.Select(u => this.GetMetaPostNoProxyAsync(u, cancellationToken)).ToList();
            await Task.WhenAll(getMetaPostTasks);
            return getMetaPostTasks.Where(t => t.Result != null).Select(t => t.Result).ToList();
        }

        //public Task<IDictionary<string, TentMetaProfile>> GetMetaProfileForUserAsync(string userId, CancellationToken cancellationToken)
        //{
        //    return this.GetMetaProfilesForUsersAsync(new List<string> { userId }, cancellationToken);
        //}

        //public Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForMentionsAsync(IList<TentMention> mentions, CancellationToken cancellationToken)
        //{
        //    // Extact the user ids from the mentions.
        //    var userIds = mentions.Select(m => m.UserId).ToList();

        //    // Fetch the meta profiles for the specified users.
        //    return this.GetMetaProfilesForUsersAsync(userIds, cancellationToken);
        //}

        //public async Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForVersionsAsync(IList<TentVersion> versions, RequestProfilesEnum requestedProfiles, CancellationToken cancellationToken)
        //{
        //    var userIds = new List<string>();

        //    if (requestedProfiles.HasFlag(RequestProfilesEnum.Entity))
        //        userIds.AddRange(versions.Select(v => v.UserId));

        //    if (requestedProfiles.HasFlag(RequestProfilesEnum.Parents))
        //        userIds.AddRange(versions
        //            .Where(v => v.Parents != null)
        //            .SelectMany(v => v.Parents)
        //            .Select(vp => vp.UserId));

        //    return userIds.Any()
        //        ? await this.GetMetaProfilesForUsersAsync(userIds, cancellationToken)
        //        : new Dictionary<string, TentMetaProfile>();
        //}

        //public Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForPostAsync<T>(TentPost<T> post, IList<TentPost> refs, RequestProfilesEnum requestedProfiles, CancellationToken cancellationToken) where T : class
        //{
        //    return this.GetMetaProfilesForPostsAsync(new List<TentPost<T>> { post }, refs, requestedProfiles, cancellationToken);
        //}

        //public async Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForPostsAsync<T>(IList<TentPost<T>> posts, IList<TentPost> refs, RequestProfilesEnum requestedProfiles, CancellationToken cancellationToken) where T : class
        //{
        //    var userIds = new List<string>();

        //    if (requestedProfiles.HasFlag(RequestProfilesEnum.Entity))
        //        userIds.AddRange(posts.Select(p => p.UserId));

        //    if (requestedProfiles.HasFlag(RequestProfilesEnum.Refs))
        //    {
        //        // Add the refs authors.
        //        userIds.AddRange(posts.Where(p => p.Refs != null)
        //            .SelectMany(p => p.Refs)
        //            .Select(pr => pr.UserId));

        //        // Add the refs mentioned users.
        //        if (refs != null && requestedProfiles.HasFlag(RequestProfilesEnum.Mentions))
        //            userIds.AddRange(refs.Where(p => p.Mentions != null)
        //                .SelectMany(p => p.Mentions)
        //                .Select(m => m.UserId));
        //    }

        //    if (requestedProfiles.HasFlag(RequestProfilesEnum.Mentions))
        //        userIds.AddRange(posts.Where(p => p.Mentions != null)
        //            .SelectMany(p => p.Mentions)
        //            .Select(m => m.UserId));

        //    if (requestedProfiles.HasFlag(RequestProfilesEnum.Permissions))
        //    {
        //        userIds.AddRange(posts.Where(p => p.Permissions != null)
        //            .SelectMany(p => p.Permissions.UserIds));
        //    }

        //    if (requestedProfiles.HasFlag(RequestProfilesEnum.Parents))
        //        userIds.AddRange(posts.Where(p => p.Version.Parents != null)
        //            .SelectMany(p => p.Version.Parents)
        //            .Select(vp => vp.UserId));

        //    return userIds.Any()
        //        ? await this.GetMetaProfilesForUsersAsync(userIds, cancellationToken)
        //        : new Dictionary<string, TentMetaProfile>();
        //}

        //public Task<IList<TentPost<object>>> GetPostRefsForPostAsync<T>(TentPost<T> post, int maxRefs, CancellationToken cancellationToken) where T : class
        //{
        //    Ensure.Argument.IsNotNull(post, "post");
        //    return this.GetPostRefsForPostsAsync(new List<TentPost<T>> { post }, maxRefs, cancellationToken);
        //}

        //public Task<IList<TentPost<object>>> GetPostRefsForPostsAsync<T>(IList<TentPost<T>> posts, int maxRefs, CancellationToken cancellationToken) where T : class
        //{
        //    Ensure.Argument.IsNotNull(posts, "posts");

        //    // Select all the PostRefs to retrieve.
        //    var postRefs = posts
        //        .Where(p => p.Refs != null)
        //        .SelectMany(p => p.Refs.Take(maxRefs))
        //        .Where(r => r.FoundPost)
        //        .Select(pr => new TentPostIdentifier
        //        {
        //            UserId = pr.UserId,
        //            PostId = pr.PostId,
        //            VersionId = pr.VersionId
        //        })
        //        .Distinct()
        //        .ToList();

        //    return this.postRepository.GetBulkAsync<object>(postRefs, cancellationToken);
        //}

        //public async Task<IDbPost<object>> GetPostWithAttachmentAsync(ObjectId userId, string digest)
        //{
        //    return this.dbClient.GetPostCollection<object>()
        //        .AsQueryable()
        //        .FirstOrDefault(p => p.OwnerId == userId
        //            && p.LastVersion
        //            && p.BPost.UserId == userId
        //            && p.BPost.Attachments.Any(a =>
        //                a.Digest == digest));
        //}

        //public async Task<IDbPost<T>> GetLastPostByTypeWithMentionAsync<T>(ObjectId userId, ITentPostType postType, TentPostReference mention) where T : class
        //{
        //    return this.dbClient.GetPostCollection<T>()
        //        .AsQueryable()
        //        .FirstOrDefault(p => p.OwnerId == userId
        //            && p.LastVersion
        //            && p.BPost.UserId == userId
        //            && ((postType.WildCard && p.BPost.Type.StartsWith(postType.ToString()))
        //                || (!postType.WildCard && p.BPost.Type == postType.ToString()))
        //            && p.BPost.Mentions.Any(m => m.UserId == mention.UserId
        //                && (mention.PostId == null || m.PostId == mention.PostId)));
        //}

        //public async Task<IDbPost<object>> GetSubscribingPostForTypeAsync(ObjectId userId, ObjectId targetUserId, params ITentPostType[] postTypes)
        //{
        //    return this.BuildGetSubscriptionPostsForType(userId, postTypes)
        //        .FirstOrDefault(p => p.BPost.UserId == userId
        //            && p.BPost.Mentions.Any(m =>
        //                m.UserId == targetUserId));
        //}

        //public async Task<IEnumerable<IDbPost<object>>> GetSubscriberPostsForTypeAsync(ObjectId userId, int skip, int take, params ITentPostType[] postTypes)
        //{
        //    return this.BuildGetSubscriptionPostsForType(userId, postTypes)
        //        .Where(p => p.BPost.UserId != userId)
        //        .Skip(skip)
        //        .Take(take)
        //        .ToList();
        //}

        //public async Task<int> GetSubscriberPostsCountForTypeAsync(ObjectId userId, params ITentPostType[] postTypes)
        //{
        //    return this.BuildGetSubscriptionPostsForType(userId, postTypes)
        //        .Count(p => p.BPost.UserId != userId);
        //}

        //public async Task<IDbPost<object>> DeletePostAsync(DbUser user, IDbPost<object> post, bool specificVersion, bool createDeletePost = true)
        //{
        //    IDbPost<object> deletedPost = null;

        //    // If needed, create the corresponding Delete post.
        //    if (createDeletePost)
        //    {
        //        var deletePostMentions = post.Post.Mentions == null
        //            ? null
        //            : post.Post.Mentions
        //                .Where(m =>
        //                    m.UserId != post.Post.UserId
        //                    && m.UserId != default(ObjectId))
        //                .Select(m => new ApiMention
        //                {
        //                    Entity = m.Entity
        //                });

        //        deletedPost = await this.CreateNewPostAsync<object>(
        //            user,
        //            this.tentConstants.DeletePostType(),
        //            null,
        //            post.Post.Permissions.Public,
        //            deletePostMentions,
        //            new[]
        //            {
        //                new ApiPostRef
        //                {
        //                    User = user,
        //                    PostId = post.Post.Id,
        //                    Type = post.Post.Type,
        //                    VersionId = specificVersion
        //                        ? post.Post.Version.Id
        //                        : null
        //                }
        //            });
        //    }

        //    // Delete the post.
        //    this.postRepository.DeletePost(post, specificVersion);

        //    // Return the newly created delete post.
        //    return deletedPost;
        //}

        #endregion

        #region Private methods.

        //private async Task<IEnumerable<IDbPost<object>>> InjestPosts(IList<ITentPost<object>> posts)
        //{
        //    // Find the users for posts to inject.
        //    var retrieveUsersTasks = posts
        //        .Select(p => p.Entity)
        //        .Distinct()
        //        .Select(this.userLogic.Value.GetUserAsync)
        //        .ToList();

        //    await Task.WhenAll(retrieveUsersTasks);

        //    var userDictionary = retrieveUsersTasks
        //        .Where(t => t.Result != null)
        //        .ToDictionary(t => t.Result.LastEntity, t => t.Result);

        //    // Save the posts to the db.
        //    var createPostsTasks = posts
        //        .Where(p => userDictionary.ContainsKey(p.Entity))
        //        .Select(p => this.CreatePostAsync(userDictionary[p.Entity], p))
        //        .ToList();

        //    await Task.WhenAll(createPostsTasks);

        //    return createPostsTasks
        //        .Where(t => t.Result != null)
        //        .Select(t => t.Result);
        //}

        private async Task<IList<TentPostIdentifier>> GetPostReplyChainAsync(User user, User targetUser, string postId, string versionId, CancellationToken cancellationToken)
        {
            // Retrieve the specified post.
            var post = await this.GetPostAsync<object>(user, targetUser, targetUser, postId, versionId, null, cancellationToken);
            if (post == null)
                throw new PostNotFoundException();

            var result = new List<TentPostIdentifier>();

            // If this post doesn't have any mentions, return an empty list.
            var firstMention = post.Mentions?.FirstOrDefault(m => m.FoundPost);
            if (firstMention == null)
                return result;

            // Create the Reply Chain item for this mention.
            result.Add(new TentPostIdentifier(firstMention.UserId, firstMention.PostId, firstMention.VersionId));

            // If this post's first mention doesn't have a Reply Chain, return only the first mention.
            if (firstMention.ReplyChain == null || !firstMention.ReplyChain.Any())
                return result;

            // Otherwise, return the whole Reply Chain.
            result.AddRange(firstMention.ReplyChain);

            return result;
        }

        //private async Task<IDictionary<string, TentMetaProfile>> GetMetaProfilesForUsersAsync(IList<string> userIds, CancellationToken cancellationToken)
        //{
        //    // Retrieve the corresponding Meta Posts for those UserIds.
        //    userIds = userIds.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        //    var metaPosts = await this.GetMetaPostForUsersNoProxyAsync(userIds, cancellationToken);

        //    // Build the resulting dictionary.
        //    return metaPosts.ToDictionary(p => p.Entity, p =>
        //    {
        //        // If no profile could be found, return a blank one.
        //        if (p.Content?.Profile == null)
        //            return new TentMetaProfile();

        //        // Otherwise, copy the one found on the meta post.
        //        var result = p.Content.Profile;

        //        // If this post has attachments, look for the avatar.
        //        var avatarAttachment = p.Attachments?.FirstOrDefault(a => a.Category == "avatar");
        //        if (avatarAttachment != null)
        //            result.AvatarDigest = avatarAttachment.Digest;

        //        return result;
        //    });
        //}

        private async Task ResolveMentionsAsync<T>(User user, TentPost<T> post, CancellationToken cancellationToken) where T : class
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
                        mention.Entity = this.modelHelpers.GetUserEntity(mention.User);
                    }
                    else if (string.IsNullOrWhiteSpace(mention.Entity))
                    {
                        mention.UserId = user.Id;
                        mention.User = user;
                    }
                    else
                    {
                        // Retrieve the user for this mention.
                        mention.User = await this.userLogic.Value.GetUserAsync(mention.Entity, cancellationToken);

                        // If no user was found, no need to continue.
                        if (mention.User == null)
                            return;

                        mention.UserId = mention.User.Id;
                    }

                    // Remove entity if it's the same as the post's.
                    if (mention.UserId == user.Id)
                        mention.Entity = null;

                    // Retrieve the post for this mention, if any.
                    if (!string.IsNullOrEmpty(mention.PostId))
                    {
                        var credentialsPost = post as TentPost<TentContentCredentials>;
                        mention.Post = await this.GetPostAsync<object>(user, mention.User, mention.User, mention.PostId, mention.VersionId, credentialsPost, cancellationToken);
                    }
                    else if (mention.Post != null)
                    {
                        mention.PostId = mention.Post.Id;
                        mention.Type = mention.Post.Type;
                    }

                    // If we don't have a post at this point, no need to continue.
                    if (mention.Post == null)
                        return;

                    mention.FoundPost = true;

                    // Reply chain.
                    var firstMention = mention.Post.Mentions?.FirstOrDefault(m => m.FoundPost);
                    if (firstMention == null)
                        return;

                    mention.ReplyChain = firstMention.ReplyChain == null
                        ? new List<TentPostIdentifier>()
                        : new List<TentPostIdentifier>(firstMention.ReplyChain);

                    mention.ReplyChain.Insert(0, new TentPostIdentifier(firstMention.UserId, firstMention.PostId, firstMention.VersionId));
                }
                catch (Exception ex)
                {
                    this.loggingService.Exception(ex, "Error while resolving post mentions.");
                }
            }).ToList();

            // Wait for all of these tasks to complete, and return their resulting Mentions.
            await Task.WhenAll(resolveMentionsTasks);
        }

        private async Task ResolvePostRefsAsync<T>(User user, TentPost<T> post, CancellationToken cancellationToken) where T : class
        {
            if (post.Refs == null || !post.Refs.Any())
            {
                post.Refs = null;
                return;
            }

            // Create a task for each PostRef.
            var resolvePostRefsTasks = post.Refs.Select(async postRef =>
            {
                try
                {
                    // Retrieve the user for this PostRef.
                    if (postRef.User != null)
                    {
                        postRef.UserId = postRef.User.Id;
                        postRef.Entity = this.modelHelpers.GetUserEntity(postRef.User);
                    }
                    else if (string.IsNullOrWhiteSpace(postRef.Entity))
                    {
                        postRef.UserId = user.Id;
                        postRef.User = user;
                    }
                    else
                    {
                        postRef.User = await this.userLogic.Value.GetUserAsync(postRef.Entity, cancellationToken);

                        // If no user could be found, no need to go further.
                        if (postRef.User == null)
                            return;

                        postRef.UserId = postRef.User.Id;
                    }

                    // Remove entity if it's the same as the post's.
                    if (postRef.UserId == user.Id)
                        postRef.Entity = null;

                    // Retrieve the post for this PostRef.
                    if (postRef.Post == null)
                        postRef.Post = await this.GetPostAsync<object>(user, postRef.User, postRef.User, postRef.PostId, postRef.VersionId, null, cancellationToken);
                    else
                        postRef.Type = postRef.Post.Type;

                    if (postRef.Post == null)
                        return;

                    postRef.FoundPost = true;
                    postRef.PostId = postRef.Post.Id == post.Id ? null : post.Id;
                }
                catch (Exception ex)
                {
                    this.loggingService.Exception(ex, "Error while resolving post references.");
                }
            }).ToList();

            // Wait for all of these tasks to complete, and return their resulting PostRefs.
            await Task.WhenAll(resolvePostRefsTasks);
        }

        private async Task ResolveParentsAsync<T>(User user, TentPost<T> post, CancellationToken cancellationToken) where T : class
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
                        parent.Entity = this.modelHelpers.GetUserEntity(parent.User);
                    }
                    else if (string.IsNullOrWhiteSpace(parent.Entity))
                    {
                        parent.UserId = user.Id;
                        parent.User = user;
                    }
                    else
                    {
                        parent.User = await this.userLogic.Value.GetUserAsync(parent.Entity, cancellationToken);

                        // If no user could be found, stop here.
                        if (parent.User == null)
                            return;

                        parent.UserId = parent.User.Id;
                        parent.Entity = this.modelHelpers.GetUserEntity(parent.User);
                    }

                    // Remove the entity if it's the same as the post.
                    if (parent.UserId == user.Id)
                        parent.Entity = null;

                    // Retrieve the post for this Parent.
                    if (parent.Post == null)
                    {
                        var parentPostId = string.IsNullOrWhiteSpace(parent.PostId)
                            ? post.Id
                            : parent.PostId;

                        parent.Post = await this.GetPostAsync<object>(user, parent.User, parent.User, parentPostId, parent.VersionId, null, cancellationToken);
                    }

                    // If no post was found, no need to go further.
                    if (parent.Post == null)
                        return;

                    parent.FoundPost = true;
                    parent.VersionId = parent.Post.Version.Id;

                    // We only specify the post id if it's different from our own.
                    parent.PostId = parent.Post.Id == post.Id ? null : parent.Post.Id;
                }
                catch (Exception ex)
                {
                    this.loggingService.Exception(ex, "Error while resolving version parents.");
                }
            }).ToList();

            // Wait for all of these tasks to complete, and return their resulting PostRefs.
            await Task.WhenAll(resolveParentsTasks);
        }

        private async Task ResolvePermissionsAsync(User user, TentPost post, CancellationToken cancellationToken)
        {
            // If this post doesn't have any permissions, assume default.
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
                    .Select(e => this.userLogic.Value.GetUserAsync(e, cancellationToken))
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

            // TODO: Process permissions for groups.
        }

        #endregion
    }
}