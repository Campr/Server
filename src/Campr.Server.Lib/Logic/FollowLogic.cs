using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Connectors.Queues;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Other.Factories;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;
using Campr.Server.Lib.Net.Tent;
using Campr.Server.Lib.Repositories;
using System.Linq;

namespace Campr.Server.Lib.Logic
{
    class FollowLogic : IFollowLogic
    {
        public FollowLogic(IPostLogic postLogic,
            IPostRepository postRepository,
            ITentClient tentClient,
            ITentQueues tentQueues,
            IDbPostFactory dbPostFactory,
            ITentPostTypeFactory postTypeFactory,
            ITentHawkSignatureFactory hawkSignatureFactory,
            ITentConstants tentConstants)
        {
            Ensure.Argument.IsNotNull(postLogic, "postLogic");
            Ensure.Argument.IsNotNull(postRepository, "postRepository");
            Ensure.Argument.IsNotNull(tentClient, "tentClient");
            Ensure.Argument.IsNotNull(tentQueues, "tentQueues");
            Ensure.Argument.IsNotNull(dbPostFactory, "dbPostFactory");
            Ensure.Argument.IsNotNull(postTypeFactory, "postTypeFactory");
            Ensure.Argument.IsNotNull(hawkSignatureFactory, "hawkSignatureFactory");
            Ensure.Argument.IsNotNull(tentConstants, "tentConstants");

            this.postLogic = postLogic;
            this.postRepository = postRepository;
            this.tentClient = tentClient;
            this.tentQueues = tentQueues;
            this.dbPostFactory = dbPostFactory;
            this.postTypeFactory = postTypeFactory;
            this.hawkSignatureFactory = hawkSignatureFactory;
            this.tentConstants = tentConstants;
        }

        private readonly IPostLogic postLogic;
        private readonly IPostRepository postRepository;
        private readonly ITentClient tentClient;
        private readonly ITentQueues tentQueues;
        private readonly IDbPostFactory dbPostFactory;
        private readonly ITentPostTypeFactory postTypeFactory;
        private readonly ITentRequestPostFactory requestPostFactory;
        private readonly ITentHawkSignatureFactory hawkSignatureFactory;
        private readonly ITentConstants tentConstants;

        public Task<TentPost> GetRelationship(User user, User targetUser, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetRelationship(user, targetUser, true, true, false, cancellationToken);
        }

        public Task<TentPost> GetRelationship(User user, User targetUser, bool createIfNotFound, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetRelationship(user, targetUser, createIfNotFound, true, false, cancellationToken);
        }

        public Task<TentPost> GetRelationship(User user, User targetUser, bool createIfNotFound, bool propagate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetRelationship(user, targetUser, createIfNotFound, propagate, false, cancellationToken);
        }

        public async Task<TentPost> GetRelationship(User user, User targetUser, bool createIfNotFound, bool propagate, bool alwaysIncludeCredentials, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.Argument.IsNotNull(user, nameof(user));
            Ensure.Argument.IsNotNull(targetUser, nameof(targetUser));

            // First, try to find an existing relationship between our user and the target user.
            var existingRelationship = await this.postLogic.GetLastPostOfTypeMentioningAsync<object>(user, user, user, this.tentConstants.RelationshipPostType, this.requestPostFactory.FromUser(targetUser), cancellationToken);
            if (((existingRelationship != null && existingRelationship.Type.SubType != "initial") || !createIfNotFound) && !alwaysIncludeCredentials)
                return existingRelationship;

            // If needed, create the local Relationship post. Don't propagate it just yet.
            var localRelationshipPost = existingRelationship
                ?? await this.postLogic.CreateNewPostAsync(
                    user,
                    this.tentConstants.RelationshipPostType() + "initial",
                    (object)null,
                    false,
                    new[]
                    {
                        new ApiMention
                        {
                            User = targetUser
                        }
                    },
                    null,
                    null,
                    false);

            // If we just created a new relationship post, create the corresponding credentials post.
            IDbPost<TentContentCredentials> localCredentialsPost;
            if (existingRelationship == null)
            {
                localCredentialsPost = await this.postLogic.CreateNewCredentialsPostAsync(user, user, localRelationshipPost);
            }
            // Otherwise, retrieve the existing credentials post.
            else
            {
                var credentialsPostType = this.postTypeFactory.FromString(this.tentConstants.CredentialsPostType(), true);
                localCredentialsPost = await this.postLogic.GetLastPostByTypeWithMentionAsync<TentContentCredentials>(user.Id, credentialsPostType, new TentPostReference
                {
                    UserId = user.Id,
                    PostId = localRelationshipPost.Post.Id
                });

                localRelationshipPost.Post.PassengerCredentials = localCredentialsPost.Post;
            }

            // If we were instructed not to propagate, this stops here.
            if (!propagate)
                return localRelationshipPost;

            // If no relationship currently exists, establish one.
            IDbPost<object> remoteRelationshipPost;

            // If the user is internal, just create the necessary posts.
            if (targetUser.IsInternal())
            {
                // Add the local credentials to the target user's feed.
                var localCredentialsFeedItem = this.dbPostFactory.CreateFeedItem(targetUser.Id, localCredentialsPost, false);
                this.postRepository.CreatePost(localCredentialsFeedItem);

                // Create the relationship post for the target user.
                remoteRelationshipPost = await this.postLogic.CreateNewPostAsync(
                    targetUser,
                    this.tentConstants.RelationshipPostType() + "subscriber",
                    (object)null,
                    true,
                    new[]
                    {
                        new ApiMention
                        {
                            User = user,
                            Post = localRelationshipPost
                        }
                    },
                    null,
                    null,
                    false);

                // Create the Credentials post for the target user.
                var remoteCredentialsPost = await this.postLogic.CreateNewCredentialsPostAsync(targetUser, targetUser, remoteRelationshipPost);

                // Add the remote Credentials to our user's feed.
                var remoteCredentialsFeedItem = this.dbPostFactory.CreateFeedItem(user.Id, remoteCredentialsPost, false);
                this.postRepository.CreatePost(remoteCredentialsFeedItem);
            }
            // Otherwise, contact the remote server and negociate a new relationship.
            else
            {
                // Retrieve the Meta Post for the target user.
                var targetUserMetaPost = this.postLogic.GetMetaPostForUser(targetUser.Id);

                // Send the Relationship Post to the remote server.
                var remoteCredentialsLink = await this.tentClient.PostRelationshipAsync(user.Handle, targetUserMetaPost.Post, localRelationshipPost.Post);
                if (remoteCredentialsLink == null)
                {
                    return null;
                }

                // Retrieve the remote Credentials Post, and import it into our feed.
                var remoteCredentialsPost = await this.postLogic.ImportPostFromLinkAsync<TentContentCredentials>(user, targetUser, remoteCredentialsLink);
                if (remoteCredentialsPost == null
                    || remoteCredentialsPost.Post.Mentions == null
                    || !remoteCredentialsPost.Post.Mentions.Any(m => m.FoundPost))
                {
                    return null;
                }

                // Retrieve the remote Relationship Post.
                var remoteRelationshipPostMention = remoteCredentialsPost.Post.Mentions.First(m => m.FoundPost);
                remoteRelationshipPost = await this.postLogic.GetPostAsync(user, targetUser, remoteRelationshipPostMention.PostId, remoteRelationshipPostMention.VersionId);
            }

            if (remoteRelationshipPost == null)
                return null;

            // Update the Relationship post for our user.
            localRelationshipPost.Post.Type = this.tentConstants.RelationshipPostType() + "subscribing";
            localRelationshipPost.Post.Permissions = null; // Set the visibility to public.

            var localMentionToRemoteRelationshipPost = localRelationshipPost.Post.Mentions.First();
            localMentionToRemoteRelationshipPost.PostId = remoteRelationshipPost.Post.Id;
            localMentionToRemoteRelationshipPost.Type = remoteRelationshipPost.Post.Type;

            // Create the new version, this one will be propagated through mentions.
            localRelationshipPost = await this.postLogic.CreatePostAsync(user, localRelationshipPost.Post, true);

            // Create a new subscription for meta posts.
            var subscriptionType = this.postTypeFactory.FromString(this.tentConstants.MetaPostType(), true);
            await this.postLogic.CreateNewPostAsync<object>(
                user,
                this.tentConstants.SubscriptionPostType() + subscriptionType,
                new TentContentSubscription
                {
                    Type = subscriptionType.ToString()
                },
                true,
                new[]
                {
                    new ApiMention
                    {
                        User = targetUser
                    }
                });

            return localRelationshipPost;
        }

        //public async Task<IDbPost<object>> AcceptRelationship(DbUser user, DbUser targetUser, Uri credentialsLinkUri, string entity, string postId)
        //{
        //    // Retrieve any local existing relationship post.
        //    var localRelationshipPost = await this.GetRelationship(user, targetUser, true, false, true);
        //    var localCredentialsPost = localRelationshipPost.Post.PassengerCredentials;

        //    // Retrieve the meta post for the remote user.
        //    var metaPost = this.postLogic.GetMetaPostForUser(targetUser.Id);

        //    // Check the meta post's server to see if we have a match with the provided credentials link.
        //    if (!metaPost.Post.Content.IsUrlServerMatch(credentialsLinkUri))
        //    {
        //        return null;
        //    }

        //    // Retrieve and import the credentials post into our user's feed.
        //    var remoteCredentialsPost = await this.postLogic.ImportPostFromLinkAsync<TentContentCredentials>(user, targetUser, credentialsLinkUri);
        //    if (remoteCredentialsPost == null
        //        || remoteCredentialsPost.Post.Mentions == null
        //        || remoteCredentialsPost.Post.Mentions.All(m => m.Post == null))
        //    {
        //        return null;
        //    }

        //    // Extract the remote relationship post.
        //    var remoteRelationshipPost = remoteCredentialsPost.Post.Mentions.First(m => m.Post != null).Post;

        //    // Update the Relationship post for our user.
        //    localRelationshipPost.Post.Type = this.tentConstants.RelationshipPostType();
        //    localRelationshipPost.Post.Permissions = new ApiPermissions { Public = true }; // Set the visibility to public.

        //    var localMentionToRemoteRelationshipPost = localRelationshipPost.Post.Mentions.First();
        //    localMentionToRemoteRelationshipPost.PostId = remoteRelationshipPost.Post.Id;
        //    localMentionToRemoteRelationshipPost.Type = remoteRelationshipPost.Post.Type;

        //    // Create the new version, this one will be propagated through mentions.
        //    localRelationshipPost = await this.postLogic.CreatePostAsync(user, localRelationshipPost.Post, true, false);

        //    // Send a link to the local credentials back to the remote server.
        //    remoteRelationshipPost.Post.PassengerCredentials = localCredentialsPost;

        //    // Queue the creation of a meta post subscription for that user.
        //    await this.tentQueues.MetaSubscriptions.AddMessageAsync(new QueueMetaSubscriptionMessage
        //    {
        //        UserId = user.Id,
        //        TargetUserId = targetUser.Id
        //    });

        //    return remoteRelationshipPost;
        //}

        public Task<ITentHawkSignature> GetCredentialsForUser(User user, User targetUser, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.GetCredentialsForUser(user, targetUser, false, null, cancellationToken);
        }

        public Task<ITentHawkSignature> GetCredentialsForUser(User user, User targetUser, bool createIfNotFound, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.GetCredentialsForUser(user, targetUser, createIfNotFound, null, cancellationToken);
        }

        public async Task<ITentHawkSignature> GetCredentialsForUser(User user, User targetUser, bool createIfNotFound, TentPost<TentContentCredentials> credentials, CancellationToken cancellationToken = new CancellationToken())
        {
            Ensure.Argument.IsNotNull(user, nameof(user));
            Ensure.Argument.IsNotNull(targetUser, nameof(targetUser));

            // If a credentials post was provided, use it to create the signature.
            if (credentials != null)
                return this.hawkSignatureFactory.FromCredentials(credentials);

            // Otherwise, retrieve the relationship between our two users.
            var localRelationshipPost = await this.GetRelationship(user, targetUser, createIfNotFound, cancellationToken);
            if (localRelationshipPost?.Mentions == null || !localRelationshipPost.Mentions.Any(m => m.UserId != user.Id && m.FoundPost))
                return null;

            // Extract the mention to the remote relationship post.
            var remoteRelationshipPostMention = localRelationshipPost.Mentions.First(m =>
                m.UserId != user.Id && m.FoundPost);

            // Retrieve the credentials post for the remote relationship post.
            var remoteCredentialsPost = await this.postLogic.GetLastPostOfTypeMentioningAsync<TentContentCredentials>(user, user, targetUser, this.tentConstants.CredentialsPostType, new TentPostReference
            {
                UserId = remoteRelationshipPostMention.UserId,
                PostId = remoteRelationshipPostMention.PostId
            }, cancellationToken);

            return remoteCredentialsPost == null
                ? null
                : this.hawkSignatureFactory.FromCredentials(remoteCredentialsPost);
        }
    }
}