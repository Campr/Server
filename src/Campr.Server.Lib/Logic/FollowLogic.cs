using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Other.Factories;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;
using Campr.Server.Lib.Net.Tent;
using Campr.Server.Lib.Models.Db.Factories;

namespace Campr.Server.Lib.Logic
{
    class FollowLogic : IFollowLogic
    {
        public FollowLogic(IPostLogic postLogic,
            ITentPostFactory postFactory,
            ITentPostTypeFactory postTypeFactory,
            ITentRequestPostFactory requestPostFactory,
            ITentClientFactory tentClientFactory,
            ITentHawkSignatureFactory hawkSignatureFactory,
            ITentConstants tentConstants)
        {
            Ensure.Argument.IsNotNull(postLogic, nameof(postLogic));
            Ensure.Argument.IsNotNull(postFactory, nameof(postFactory));
            Ensure.Argument.IsNotNull(postTypeFactory, nameof(postTypeFactory));
            Ensure.Argument.IsNotNull(requestPostFactory, nameof(requestPostFactory));
            Ensure.Argument.IsNotNull(tentClientFactory, nameof(tentClientFactory));
            Ensure.Argument.IsNotNull(hawkSignatureFactory, nameof(hawkSignatureFactory));
            Ensure.Argument.IsNotNull(tentConstants, nameof(tentConstants));

            this.postLogic = postLogic;
            this.postFactory = postFactory;
            this.postTypeFactory = postTypeFactory;
            this.requestPostFactory = requestPostFactory;
            this.tentClientFactory = tentClientFactory;
            this.hawkSignatureFactory = hawkSignatureFactory;
            this.tentConstants = tentConstants;
        }

        private readonly IPostLogic postLogic;
        private readonly ITentPostFactory postFactory;
        private readonly ITentPostTypeFactory postTypeFactory;
        private readonly ITentRequestPostFactory requestPostFactory;
        private readonly ITentClientFactory tentClientFactory;
        private readonly ITentHawkSignatureFactory hawkSignatureFactory;
        private readonly ITentConstants tentConstants;

        public Task<TentPost<object>> GetRelationship(User user, User targetUser, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetRelationship(user, targetUser, true, true, false, cancellationToken);
        }

        public Task<TentPost<object>> GetRelationship(User user, User targetUser, bool createIfNotFound, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetRelationship(user, targetUser, createIfNotFound, true, false, cancellationToken);
        }

        public Task<TentPost<object>> GetRelationship(User user, User targetUser, bool createIfNotFound, bool propagate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetRelationship(user, targetUser, createIfNotFound, propagate, false, cancellationToken);
        }

        public async Task<TentPost<object>> GetRelationship(User user, User targetUser, bool createIfNotFound, bool propagate, bool alwaysIncludeCredentials, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.Argument.IsNotNull(user, nameof(user));
            Ensure.Argument.IsNotNull(targetUser, nameof(targetUser));

            // First, try to find an existing relationship between our user and the target user.
            var existingRelationship = await this.postLogic.GetLastPostOfTypeMentioningAsync<object>(user, user, user, this.tentConstants.RelationshipPostType, this.requestPostFactory.FromUser(targetUser), cancellationToken);
            if (((existingRelationship != null && existingRelationship.Type.SubType != "initial") || !createIfNotFound) && !alwaysIncludeCredentials)
                return existingRelationship;

            // If needed, create the local Relationship post. Don't propagate it just yet.
            var localRelationshipPost = existingRelationship ?? this.postFactory.Make(user, this.postTypeFactory.FromType(this.tentConstants.RelationshipPostType, "initial"))
                .WithMentions(new TentMention { User = targetUser })
                .WithPublic(false)
                .Post();

            // If we just created a new relationship post, create the corresponding credentials post.
            TentPost<TentContentCredentials> localCredentialsPost;
            if (existingRelationship == null)
            {
                // Save the relationship post.
                await this.postLogic.CreatePostAsync(user, localRelationshipPost, cancellationToken);

                // Create a new credentials post to go along with this new relationship post.
                localCredentialsPost = await this.postLogic.CreateNewCredentialsPostAsync(user, user, localRelationshipPost);
            }
            // Otherwise, retrieve the existing credentials post.
            else
            {
                // Find the associated credentials post.
                localCredentialsPost = await this.postLogic.GetLastPostOfTypeMentioningAsync<TentContentCredentials>(user, user, user,
                    this.tentConstants.CredentialsPostType,
                    this.requestPostFactory.FromPost(existingRelationship),
                    cancellationToken);

                // Attach the credentials to the relationship post.
                existingRelationship.PassengerCredentials = localCredentialsPost;
            }

            // If we were instructed not to propagate, this stops here.
            if (!propagate)
                return localRelationshipPost;

            // If no relationship currently exists, establish one.
            TentPost<object> remoteRelationshipPost;

            // If the user is internal, just create the necessary posts.
            if (targetUser.IsInternal())
            {
                // Add the local credentials to the target user's feed.
                await this.postLogic.CreateUserPostAsync(targetUser, localCredentialsPost, false, cancellationToken);

                // Create the relationship post for the target user.
                remoteRelationshipPost = this.postFactory.Make(targetUser, this.postTypeFactory.FromType(this.tentConstants.RelationshipPostType, "subscriber"))
                    .WithMentions(new TentMention { User = user, Post = localRelationshipPost })
                    .WithPublic(false)
                    .Post();
                await this.postLogic.CreatePostAsync(targetUser, remoteRelationshipPost, cancellationToken);

                // Create the Credentials post for the target user.
                var remoteCredentialsPost = await this.postLogic.CreateNewCredentialsPostAsync(targetUser, targetUser, remoteRelationshipPost);

                // Add the remote Credentials to our user's feed.
                await this.postLogic.CreateUserPostAsync(user, remoteCredentialsPost, false, cancellationToken);
            }
            // Otherwise, contact the remote server and negociate a new relationship.
            else
            {
                // Retrieve the Meta Post for the target user.
                var targetUserMetaPost = await this.postLogic.GetMetaPostAsync(targetUser, cancellationToken);

                // Send the Relationship Post to the remote server.
                var tentClient = this.tentClientFactory.Make(targetUserMetaPost);
                var remoteCredentialsLink = await tentClient.PostRelationshipAsync(user, localRelationshipPost, cancellationToken);
                if (remoteCredentialsLink == null)
                    return null;

                // Retrieve the remote Credentials Post, and import it into our feed.
                var remoteCredentialsPost = await this.postLogic.ImportPostFromLinkAsync<TentContentCredentials>(user, targetUser, remoteCredentialsLink, cancellationToken);
                if (remoteCredentialsPost?.Mentions == null || !remoteCredentialsPost.Mentions.Any(m => m.FoundPost))
                    return null;

                // Retrieve the remote Relationship Post.
                var remoteRelationshipPostMention = remoteCredentialsPost.Mentions.First(m => m.FoundPost);
                remoteRelationshipPost = await this.postLogic.GetPostAsync<object>(user, targetUser, targetUser, remoteRelationshipPostMention.PostId, remoteRelationshipPostMention.VersionId, remoteCredentialsPost, cancellationToken);
            }

            if (remoteRelationshipPost == null)
                return null;

            // Update the Relationship post for our user.
            localRelationshipPost.Type = this.postTypeFactory.FromType(this.tentConstants.RelationshipPostType, "subscribing");
            localRelationshipPost.Permissions = null; // Set the visibility to public.

            var localMentionToRemoteRelationshipPost = localRelationshipPost.Mentions.First();
            localMentionToRemoteRelationshipPost.PostId = remoteRelationshipPost.Id;
            localMentionToRemoteRelationshipPost.Type = remoteRelationshipPost.Type;

            // Create the new version, this one will be propagated through mentions.
            localRelationshipPost = await this.postLogic.CreatePostAsync(user, localRelationshipPost, cancellationToken);

            // Create a new subscription for meta posts.
            var subscriptionType = this.postTypeFactory.FromType(this.tentConstants.SubscriptionPostType, this.tentConstants.MetaPostType.Type);
            var subscriptionContent = new TentContentSubscription { Type = subscriptionType.ToString() };
            var subscriptionPost = this.postFactory.FromContent(user, subscriptionContent, subscriptionType)
                .WithMentions(new TentMention { User = targetUser })
                .Post();
            await this.postLogic.CreatePostAsync(user, subscriptionPost, cancellationToken);

            return localRelationshipPost;
        }

        public async Task<TentPost<object>> AcceptRelationship(User user, User targetUser, Uri credentialsLinkUri, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Retrieve any local existing relationship post.
            var localRelationshipPost = await this.GetRelationship(user, targetUser, true, false, true, cancellationToken);
            var localCredentialsPost = localRelationshipPost.PassengerCredentials;

            // Retrieve the meta post for the remote user.
            var metaPost = await this.postLogic.GetMetaPostAsync(targetUser, cancellationToken);

            // Check the meta post's server to see if we have a match with the provided credentials link.
            if (!metaPost.Content.IsUrlServerMatch(credentialsLinkUri))
                return null;

            // Retrieve and import the credentials post into our user's feed.
            var remoteCredentialsPost = await this.postLogic.ImportPostFromLinkAsync<TentContentCredentials>(user, targetUser, credentialsLinkUri, cancellationToken);
            if (remoteCredentialsPost?.Mentions == null || remoteCredentialsPost.Mentions.All(m => m.Post == null))
                return null;

            // Extract the remote relationship post.
            var remoteRelationshipPost = remoteCredentialsPost.Mentions.First(m => m.Post != null).Post;

            // Update the Relationship post for our user.
            localRelationshipPost.Type = this.tentConstants.RelationshipPostType;
            localRelationshipPost.Permissions = null; // Set the visibility to public.

            var localMentionToRemoteRelationshipPost = localRelationshipPost.Mentions.First();
            localMentionToRemoteRelationshipPost.PostId = remoteRelationshipPost.Id;
            localMentionToRemoteRelationshipPost.Type = remoteRelationshipPost.Type;

            // Create the new version, this one will be propagated through mentions.
            localRelationshipPost = await this.postLogic.CreatePostAsync(user, localRelationshipPost, false, cancellationToken);

            // Send a link to the local credentials back to the remote server.
            remoteRelationshipPost.PassengerCredentials = localCredentialsPost;

            //// Queue the creation of a meta post subscription for that user.
            //await this.tentQueues.MetaSubscriptions.AddMessageAsync(new QueueMetaSubscriptionMessage
            //{
            //    UserId = user.Id,
            //    TargetUserId = targetUser.Id
            //});

            return remoteRelationshipPost;
        }

        public Task<ITentHawkSignature> GetCredentialsForUserAsync(User user, User targetUser, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.GetCredentialsForUserAsync(user, targetUser, false, null, cancellationToken);
        }

        public Task<ITentHawkSignature> GetCredentialsForUserAsync(User user, User targetUser, bool createIfNotFound, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.GetCredentialsForUserAsync(user, targetUser, createIfNotFound, null, cancellationToken);
        }

        public async Task<ITentHawkSignature> GetCredentialsForUserAsync(User user, User targetUser, bool createIfNotFound, TentPost<TentContentCredentials> credentials, CancellationToken cancellationToken = new CancellationToken())
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
            var remoteCredentialsPost = await this.postLogic.GetLastPostOfTypeMentioningAsync<TentContentCredentials>(user, user, targetUser,
                this.tentConstants.CredentialsPostType,
                this.requestPostFactory.FromMention(remoteRelationshipPostMention),
                cancellationToken);

            return remoteCredentialsPost == null
                ? null
                : this.hawkSignatureFactory.FromCredentials(remoteCredentialsPost);
        }
    }
}