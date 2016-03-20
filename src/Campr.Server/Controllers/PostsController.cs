using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Exceptions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Logic;
using Campr.Server.Lib.Models.Tent;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace Campr.Server.Controllers
{
    public class PostsController : Controller
    {
        public PostsController(IPostLogic postLogic,
            IUserLogic userLogic,
            IFollowLogic followLogic,
            IBewitLogic bewitLogic,
            ITypeSpecificLogic typeSpecificLogic,
            IAuthenticationService auth,
            ITentAttachmentFactory tentAttachmentFactory,
            IJsonHelpers jsonHelpers,
            IUriHelpers uriHelpers,
            IHttpHelpers httpHelpers,
            ITentConstants tentConstants)
        {
            Ensure.Argument.IsNotNull(postLogic, "postLogic");
            Ensure.Argument.IsNotNull(userLogic, "userLogic");
            Ensure.Argument.IsNotNull(followLogic, "followLogic");
            Ensure.Argument.IsNotNull(bewitLogic, "bewitLogic");
            Ensure.Argument.IsNotNull(typeSpecificLogic, "typeSpecificLogic");
            Ensure.Argument.IsNotNull(auth, "authenticationService");
            Ensure.Argument.IsNotNull(tentAttachmentFactory, "tentAttachmentFactory");
            Ensure.Argument.IsNotNull(jsonHelpers, "jsonHelpers");
            Ensure.Argument.IsNotNull(uriHelpers, "uriHelpers");
            Ensure.Argument.IsNotNull(httpHelpers, "httpHelpers");
            Ensure.Argument.IsNotNull(tentConstants, "tentConstants");

            this.postLogic = postLogic;
            this.userLogic = userLogic;
            this.followLogic = followLogic;
            this.bewitLogic = bewitLogic;
            this.typeSpecificLogic = typeSpecificLogic;
            this.auth = auth;
            this.tentAttachmentFactory = tentAttachmentFactory;
            this.jsonHelpers = jsonHelpers;
            this.uriHelpers = uriHelpers;
            this.httpHelpers = httpHelpers;
            this.tentConstants = tentConstants;
        }

        private readonly IPostLogic postLogic;
        private readonly IUserLogic userLogic;
        private readonly IFollowLogic followLogic;
        private readonly IBewitLogic bewitLogic;
        private readonly ITypeSpecificLogic typeSpecificLogic;
        private readonly IAuthenticationService auth;
        private readonly ITentAttachmentFactory tentAttachmentFactory;
        private readonly IJsonHelpers jsonHelpers;
        private readonly IUriHelpers uriHelpers;
        private readonly IHttpHelpers httpHelpers;
        private readonly ITentConstants tentConstants;

        /// <summary>
        ///     Retrieve a single post.
        /// </summary>
        /// <param name="userHandle">The handle of the client user.</param>
        /// <param name="entity">The entity of the target user.</param>
        /// <param name="itemId">The Id of the post to retrieve.</param>
        /// <returns>The requested post.</returns>
        [HttpGet("{userHandle}/{entity}/{postId}")]
        public async Task<TentSinglePostResult<object>> GetPost(string userHandle, string entity, string postId)
        {
            

            // Identify if the requested user is internal or not.
            var targetUser = await this.userLogic.GetUserAsync(entity);
            if (targetUser == null
                || (targetUser.Handle != userHandle && this.auth.AuthType == AuthType.Bewit)
                || (!targetUser.IsInternal() && !this.User.Identity.IsAuthenticated))
                throw new ApiException(HttpStatusCode.NotFound, "The specified post could not be found");
            
            // Retrieve the requested post.
            var requestingUser = this.auth.AuthType == AuthType.Bewit || this.auth.AuthType == AuthType.App
                ? targetUser
                : this.auth.User;

            var post = await this.postLogic.GetPostAsync(requestingUser, targetUser, postId, this.auth.Parameters.VersionId, this.auth.Parameters.CacheControl);
            if (post == null
                || (this.auth.AuthType == AuthType.App
                    && post.Type != this.tentConstants.AppPostType())
                || (!post.Permissions.Public
                    && this.auth.AppPostTypes != null 
                    && !this.auth.AppPostTypes.IsReadMatch(post.Post.Type)))
            {
                throw new CustomHttpException(HttpStatusCode.NotFound);
            }

            // Edit the post for the response.
            var isNotUserAuth = this.auth.AuthType != AuthType.User;
            post.Post.ResponseClean(isNotUserAuth, true);
            
            Request.Properties.Add("PostType", post.Post.Type);

            // Retrieve post refs.
            var postRefs = this.auth.IsAuthenticated 
                ? this.postLogic.GetPostRefsForPost(post.Post, this.auth.Parameters.MaxRefs.GetValueOrDefault()).ToList()
                : null;

            return new TentSinglePostResult<object>
            {
                Post = post.Post,
                Profiles = this.auth.IsAuthenticated ? await this.postLogic.GetMetaProfilesForPostAsync(post.Post, postRefs, this.auth.Parameters.Profiles) : null,
                PostRefs = postRefs
            };
        }

        /// <summary>
        ///     Create a new post.
        /// </summary>
        /// <param name="userHandle">The handle of the targeted user.</param>
        /// <returns>The newly created post.</returns>
        public async Task<HttpResponseMessage> PostPost(string userHandle)
        {
            TentPost newPost;

            // Read the content of the request.
            if (Request.Content.IsMimeMultipartContent())
            {
                var multipart = await Request.Content.ReadAsMultipartAsync();

                var postContent = multipart.Contents.FirstOrDefault(
                        c => c.Headers.ContentType == null
                        || c.Headers.ContentType.MediaType == this.tentConstants.PostContentType());
                if (postContent == null)
                {
                    throw new CustomHttpException(HttpStatusCode.BadRequest);
                }

                // Parse the new post version.
                var stringPostContent = await postContent.ReadAsStringAsync();
                newPost = this.jsonHelpers.TryFromJsonString<TentPost>(stringPostContent);
                newPost.NewAttachments = new List<HttpContent>();

                // If it doesn't exist, create the Attachment collection.
                if (newPost.Attachments == null)
                {
                    newPost.Attachments = new List<ApiPostAttachment>();
                }

                // Create attachment objects from the multipart request.
                var createAttachmentsTasks = multipart.Contents
                    .Where(c => c.Headers.ContentType != null
                        && c.Headers.ContentType.MediaType != this.tentConstants.PostContentType())
                    .Select(c => this.tentAttachmentFactory.FromHttpContentAsync(c))
                    .ToList();

                await Task.WhenAll(createAttachmentsTasks);

                // Add the attachments to the post.
                newPost.Attachments.AddRange(createAttachmentsTasks
                    .Where(t => t.Result != null)
                    .Select(t => t.Result));
            }
            else
            {
                var stringContent = await Request.Content.ReadAsStringAsync();
                newPost = this.jsonHelpers.TryFromJsonString<TentPost>(stringContent);
            }

            // Ensure the new post's validity.
            if (newPost == null || !newPost.Validate())
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest);
            }

            // At this point, the user must be authenticated, or this has to be either an App or Relationship post.
            if ((!this.auth.Check(Request) 
                && newPost.Type != this.tentConstants.AppPostType()))
            {
                throw new CustomHttpException(HttpStatusCode.Unauthorized);
            }

            if (this.auth.AppPostTypes != null && !this.auth.AppPostTypes.IsWriteMatch(newPost.Type))
            {
                throw new CustomHttpException(HttpStatusCode.Forbidden);
            }

            // Retrieve the user.
            var user = await this.userLogic.GetUserAsync(userHandle);
            
            // Try to create the post in our system. If the post already exists, it will be returned.
            var post = await this.postLogic.CreatePostAsync(user, newPost);
            if (post == null)
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest);
            }

            // If this is an application post, create the corresponding credentials.
            // TODO: Move this to a type specific action.
            if (post.Post.Type == this.tentConstants.AppPostType())
            {
                await this.postLogic.CreateNewCredentialsPostAsync(user, user, post);
            }

            // Edit the post for the response.
            post.Post.ResponseClean(false, true);

            // Create the response message, and add a Link to a credentials post if needed.
            var response = Request.CreateResponse(new TentSinglePostResult
            {
                Post = post.Post
            });

            Request.Properties.Add("PostType", post.Post.Type);

            // If needed, add a credentials link to the response.
            await this.AddCredentialsToResponse(response, userHandle, post.Post);

            return response;
        }

        /// <summary>
        ///     Post a new version of a post.
        /// </summary>
        /// <param name="userHandle">The handle of the targeted user.</param>
        /// <param name="entity">The entity of the posting user.</param>
        /// <param name="itemId">The Id of the post to create a new version of.</param>
        /// <returns>The updated post.</returns>
        public async Task<HttpResponseMessage> PutPost(string userHandle, string entity, string itemId)
        {
            // Retrieve the necessary users.
            var userTasks = new[]
            {
                this.userLogic.GetUserAsync(userHandle),
                this.userLogic.GetUserAsync(this.uriHelpers.UrlDecode(entity))
            };

            await Task.WhenAll(userTasks);

            if (userTasks.Any(t => t.Result == null))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest);
            }
           
            TentPost newPost;
            string requestRel;

            // Read the content of the request.
            if (Request.Content.IsMimeMultipartContent())
            {
                var multipart = await Request.Content.ReadAsMultipartAsync();

                var postContent = multipart.Contents.FirstOrDefault(
                        c => c.Headers.ContentType == null
                        || c.Headers.ContentType.MediaType == this.tentConstants.PostContentType());
                if (postContent == null)
                {
                    throw new CustomHttpException(HttpStatusCode.BadRequest);
                }

                // Parse the new post version.
                var stringPostContent = await postContent.ReadAsStringAsync();
                newPost = this.jsonHelpers.TryFromJsonString<TentPost>(stringPostContent);
                newPost.NewAttachments = new List<HttpContent>();

                requestRel = this.httpHelpers.ReadRelInContentType(postContent.Headers.ContentType);
                
                // If it doesn't exist, create the Attachment collection.
                if (newPost.Attachments == null)
                {
                    newPost.Attachments = new List<ApiPostAttachment>();
                }

                // Create attachment objects from the multipart request.
                var createAttachmentsTasks = multipart.Contents
                    .Where(c => c.Headers.ContentType != null
                        && c.Headers.ContentType.MediaType != this.tentConstants.PostContentType())
                    .Select(c => this.tentAttachmentFactory.FromHttpContentAsync(c))
                    .ToList();

                await Task.WhenAll(createAttachmentsTasks);

                // Add the attachments to the post.
                newPost.Attachments.AddRange(createAttachmentsTasks
                    .Where(t => t.Result != null)
                    .Select(t => t.Result));
            }
            else
            {
                var stringContent = await Request.Content.ReadAsStringAsync();
                newPost = this.jsonHelpers.TryFromJsonString<TentPost>(stringContent);
                requestRel = this.httpHelpers.ReadRelInContentType(Request.Content.Headers.ContentType);
            }
            
            // Ensure the new post's validity.
            if (newPost == null || !newPost.Validate() || string.IsNullOrEmpty(itemId))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest);
            }

            // Check authentication.
            this.auth.Check(Request);

            newPost.Id = itemId;
            var isRelationshipPost = newPost.Type.StartsWith(this.tentConstants.RelationshipPostType());
            var isValidAppPost = newPost.Type.StartsWith(this.tentConstants.AppPostType())
                && this.auth.AuthType == AuthType.App
                && this.auth.AppId == newPost.Id;
            var isImport = requestRel == this.tentConstants.ImportRel();

            // If this is an authenticated request, check the user's identity.
            if (!(this.auth.IsAuthenticated
                && ((this.auth.UserId.GetValueOrDefault() == userTasks[0].Result.Id && this.auth.AuthType != AuthType.Server)
                    || (this.auth.UserId.GetValueOrDefault() == userTasks[1].Result.Id && this.auth.RelationshipUserId.GetValueOrDefault() == userTasks[0].Result.Id && this.auth.AuthType == AuthType.Server)))
                && !isRelationshipPost
                && !isValidAppPost)
            {
                throw new CustomHttpException(HttpStatusCode.Forbidden);
            }

            if (this.auth.AppPostTypes != null && !this.auth.AppPostTypes.IsWriteMatch(newPost.Type))
            {
                throw new CustomHttpException(HttpStatusCode.Forbidden);
            }

            IDbPost<object> post = null;

            // Check if this is a foreign relationship post.
            if (!this.auth.IsAuthenticated && isRelationshipPost && !isImport)
            {
                // Read the credentials link from the headers.
                var credentialsLinks = this.httpHelpers.ReadLinksInHeaders(this.Request.Headers, this.tentConstants.CredentialsRel());
                if (!credentialsLinks.Any())
                {
                    throw new CustomHttpException(HttpStatusCode.BadRequest);
                }

                // React accordingly to the relationship handshake.
                post = await this.followLogic.AcceptRelationship(userTasks[0].Result, userTasks[1].Result, credentialsLinks.FirstOrDefault(), newPost.Entity, newPost.Id);
            }
            else if (this.auth.IsAuthenticated
                && (this.auth.AuthType != AuthType.Server || this.auth.UserId.GetValueOrDefault() == userTasks[1].Result.Id))
            {
                // Try to create the post in our system. If the post already exists, it will be returned.
                post = await this.postLogic.CreatePostAsync(userTasks[1].Result, newPost, false, true, isImport);
            }

            // If we have no Post at this point, something went wrong.
            if (post == null)
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest);
            }

            // If this is an external notification add the post to our user's Timeline.
            if (this.auth.AuthType == AuthType.Server)
            {
                // Perform the type specific actions.
                await this.typeSpecificLogic.SpecificActionNotificationPostAsync(userTasks[0].Result, userTasks[1].Result, post.Post);

                // Create the feed item.
                await this.postLogic.CreateFeedItemAsync(this.auth.RelationshipUserId.GetValueOrDefault(), post);
            }

            // If this is an import post belonging to a different user, add it to our user's feed.
            if (isImport && post.Post.UserId != this.auth.UserId.GetValueOrDefault())
            {
                await this.postLogic.CreateFeedItemAsync(this.auth.UserId.GetValueOrDefault(), post);
            }

            // Edit the post for the response.
            post.Post.ResponseClean(false, true);

            // Create the response message, and add a Link to a credentials post if needed.
            var response = Request.CreateResponse(this.auth.AuthType == AuthType.Server
                ? null
                : new TentSinglePostResult { Post = post.Post });
            
            Request.Properties.Add("PostType", post.Post.Type);

            // If needed, add a credentials link to the response.
            await this.AddCredentialsToResponse(response, userHandle, post.Post);

            return response;
        }

        /// <summary>
        ///     Delete a specific post.
        /// </summary>
        /// <param name="entity">The entity of the post's owner.</param>
        /// <param name="itemId">The Id of the post to delete.</param>
        /// <returns>Nothing.</returns>
        public async Task<HttpResponseMessage> DeletePost(string entity, string itemId)
        {
            this.auth.Check(Request);
            await this.auth.ResolveUsers();

            // We need a post Id.
            if (string.IsNullOrEmpty(itemId))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest);
            }

            // Retrieve the target user.
            var targetUser = await this.userLogic.GetUserAsync(this.uriHelpers.UrlDecode(entity));
            if (targetUser == null)
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest);
            }

            // Retrieve the corresponding post.
            var post = await this.postLogic.GetPostAsync(this.auth.User, targetUser, itemId, this.auth.Parameters.VersionId);
            if (post == null)
            {
                throw new CustomHttpException(HttpStatusCode.NotFound);
            }

            // If this is an authenticated request, check the user's identity.
            if (!this.auth.IsAuthenticated || this.auth.UserId.GetValueOrDefault() != targetUser.Id)
            {
                if (post.Post.Permissions.Public)
                {
                    throw new CustomHttpException(HttpStatusCode.Unauthorized);
                }
                
                throw new CustomHttpException(HttpStatusCode.NotFound);
            }

            if (this.auth.UserId.GetValueOrDefault() != targetUser.Id)
            {
                throw new CustomHttpException(HttpStatusCode.Forbidden);
            }

            // Check the "Create-Delete-Post" header.
            var dontCreateDeletePost = Request.Headers.Contains(this.tentConstants.CreateDeletePostHeader())
                && Request.Headers.GetValues(this.tentConstants.CreateDeletePostHeader()).FirstOrDefault() == "false";
            
            // Check app authorization.
            if (this.auth.AppPostTypes != null && !this.auth.AppPostTypes.IsWriteMatch(post.Post.Type))
            {
                if (post.Post.Permissions.Public || this.auth.AppPostTypes.IsReadMatch(post.Post.Type))
                {
                    throw new CustomHttpException(HttpStatusCode.Forbidden);
                }
                else
                {
                    throw new CustomHttpException(HttpStatusCode.NotFound);
                }
            }

            // Delete the post.
            var deletePost = await this.postLogic.DeletePostAsync(targetUser, post, !string.IsNullOrWhiteSpace(this.auth.Parameters.VersionId), !dontCreateDeletePost);

            // Create the response (required for 0.3 validator).
            if (deletePost == null)
            {
                return Request.CreateResponse(HttpStatusCode.OK);
            }

            var response = Request.CreateResponse(new TentPostResult<object>
            {
                Post = deletePost.Post
            });

            // If needed, add the type of the Delete Post to the response.
            Request.Properties.Add("PostType", deletePost.Post.Type);

            return response;
        }

        private async Task AddCredentialsToResponse(HttpResponseMessage response, string userHandle, TentPost<object> post)
        {
            if (post.PassengerCredentials == null)
                return;

            // Generate a bewit signature for the credentials post.
            var bewit = await this.bewitLogic.CreateBewitForPostAsync(userHandle, post.PassengerCredentials.Id);

            // Add the resulting Link header to the response.
            var bewitUri = this.uriHelpers.GetCamprPostBewitUri(userHandle, post.PassengerCredentials.Id, bewit).AbsoluteUri;
            response.Headers.Add("Link", $"<{bewitUri}>; rel=\"{this.tentConstants.CredentialsRel}\"");
        }
    }
}