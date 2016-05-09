using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Logic;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;
using Campr.Server.Lib.Net.Base.Factories;

namespace Campr.Server.Lib.Net.Tent
{
    public class TentClient : ITentClient
    {
        public TentClient(
            IHttpRequestFactory httpRequestFactory,
            IHttpClientFactory httpClientFactory,
            IQueryStringHelpers queryStringHelpers,
            IUriHelpers uriHelpers,
            ITentConstants tentConstants,

            TentPost<TentContentMeta> target, 
            ITentHawkSignature credentials = null)
        {
            Ensure.Argument.IsNotNull(httpRequestFactory, nameof(httpRequestFactory));
            Ensure.Argument.IsNotNull(httpClientFactory, nameof(httpClientFactory));
            Ensure.Argument.IsNotNull(queryStringHelpers, nameof(queryStringHelpers));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            Ensure.Argument.IsNotNull(tentConstants, nameof(tentConstants));

            Ensure.Argument.IsNotNull(target, nameof(target));
            
            this.httpRequestFactory = httpRequestFactory;
            this.httpClientFactory = httpClientFactory;
            this.queryStringHelpers = queryStringHelpers;
            this.uriHelpers = uriHelpers;
            this.tentConstants = tentConstants;

            this.target = target;
            this.credentials = credentials;

            // Make sure that the provided meta post has at least one server.
            if (this.target.Content?.Servers.FirstOrDefault() == null)
                throw new ArgumentException("The provided meta post doesn't have any servers.", nameof(target));
        }
        
        private readonly IHttpRequestFactory httpRequestFactory;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IQueryStringHelpers queryStringHelpers;
        private readonly IUriHelpers uriHelpers;
        private readonly ITentConstants tentConstants;

        private readonly TentPost<TentContentMeta> target;
        private readonly ITentHawkSignature credentials;

        #region Public interface.

        public Task<TentPost<T>> GetAsync<T>(string postId, CancellationToken cancellationToken = new CancellationToken()) where T : class
        {
            return this.GetAsync<T>(postId, null, cancellationToken);
        }

        public Task<TentPost<T>> GetAsync<T>(string postId, string versionId, CancellationToken cancellationToken = new CancellationToken()) where T : class
        {
            Ensure.Argument.IsNotNullOrWhiteSpace(postId, nameof(postId));

            // Build the parameters for this request.
            var parameters = new Dictionary<string, string>
            {
                { "post", postId }
            };

            // Build the query parameters for this request.
            IDictionary<string, object> queryParameters = null;
            if (!string.IsNullOrWhiteSpace(versionId))
            {
                queryParameters = new Dictionary<string, object>
                {
                    { "version", versionId }
                };
            }

            // Build the Uri for the specified post and perform the request.
            var postUri = this.GetEndpointUriFromMetaPost(TentMetaEndpointEnum.Post, parameters, queryParameters);
            return this.GetAtUriAsync<T>(postUri, cancellationToken);
        }

        public async Task<IList<TentPost<T>>> GetAsync<T>(ITentFeedRequest feedRequest, CancellationToken cancellationToken = new CancellationToken()) where T : class
        {
            Ensure.Argument.IsNotNull(feedRequest, nameof(feedRequest));

            // Perform the requests.
            var baseUri = this.GetEndpointUriFromMetaPost(TentMetaEndpointEnum.PostsFeed);
            var client = this.httpClientFactory.Make();

            // Loop to retrieve all the pages for this request.
            var posts = new List<TentPost<T>>();
            var pages = 0;
            string nextPageParameter = null;
            do
            {
                // Create the request.
                var request = this.httpRequestFactory
                    .Get(await feedRequest.AsUriAsync(baseUri, nextPageParameter, cancellationToken))
                    .AddAccept(this.tentConstants.PostFeedContentType);

                // If needed, add credentials.
                if (this.credentials != null)
                    request.AddCredentials(this.credentials);
                
                var response = await client.SendAsync<TentPostResult<T>>(request, cancellationToken);

                // Validate the feed response.
                if (response.Posts == null)
                    throw new Exception("The server didn't return a post array.");

                // Add the posts to the list.
                posts.AddRange(response.Posts);

                // Check if have a next page parameter.
                nextPageParameter = response.Pages?.Next;
                pages++;

            // As long as we can paginate, do it.
            } while (!string.IsNullOrWhiteSpace(nextPageParameter)
                && posts.Count < feedRequest.AsLimit().GetValueOrDefault(uint.MaxValue)
                && pages < 200);

            return posts;
        }

        public async Task<long> CountAsync(ITentFeedRequest feedRequest, CancellationToken cancellationToken = new CancellationToken())
        {
            Ensure.Argument.IsNotNull(feedRequest, nameof(feedRequest));

            // Build the Uri for this request.
            var baseUri = this.GetEndpointUriFromMetaPost(TentMetaEndpointEnum.PostsFeed);
            var requestUri = await feedRequest.AsUriAsync(baseUri, null, cancellationToken);

            // Perform the request and return.
            return await this.CountAtUriAsync(requestUri, this.tentConstants.PostFeedContentType, cancellationToken);
        }

        #endregion

        #region Private methods.

        private async Task<TentPost<T>> GetAtUriAsync<T>(Uri postUri, CancellationToken cancellationToken) where T : class
        {
            // Build the request.
            var request = this.httpRequestFactory
                .Get(postUri)
                .AddAccept(this.tentConstants.PostContentType);

            // If needed, and an Authorization header to this request.
            if (this.credentials != null)
                request.AddCredentials(this.credentials);

            // Perform the request using an HTTP client.
            var client = this.httpClientFactory.Make();
            var postResult = await client.SendAsync<TentPostResult<T>>(request, cancellationToken);

            // TODO: Perform validation.

            // Return the post.
            return postResult?.Post;
        }

        private async Task<long> CountAtUriAsync(Uri uri, string accept, CancellationToken cancellationToken)
        {
            // Build the request.
            var request = this.httpRequestFactory
                .Head(uri)
                .AddAccept(accept);

            // If needed, and an Authorization header to this request.
            if (this.credentials != null)
                request.AddCredentials(this.credentials);

            // Perform the request using an HTTP client.
            var client = this.httpClientFactory.Make();
            var response = await client.SendAsync(request, cancellationToken);

            // Extract the count from the response.
            int result;
            if (response?.Content == null
                || !response.Headers.Contains(this.tentConstants.CountHeaderName)
                || !response.Headers.GetValues(this.tentConstants.CountHeaderName).Any()
                || !int.TryParse(response.Headers.GetValues(this.tentConstants.CountHeaderName).First(), out result))
                throw new Exception("The count request failed.");

            return result;
        }

        private Uri GetEndpointUriFromMetaPost(TentMetaEndpointEnum endpoint, IDictionary<string, string> parameters = null, IDictionary<string, object> queryParameters = null)
        {
            // Extract the Uri from the MetaPost.
            var baseUriString = this.target.Content.Servers.First().GetEndpoint(endpoint);
            if (string.IsNullOrWhiteSpace(baseUriString))
                throw new Exception("An endpoint of the requested type couldn't be found.");

            // Replace the parameters.
            if (parameters == null)
                parameters = new Dictionary<string, string>();

            // Add the target's user entity if none was specified.
            if (!parameters.ContainsKey("entity"))
                parameters.Add("entity", this.target.Entity);

            // Build the Uri.
            baseUriString = parameters.Aggregate(baseUriString, (current, kv) => current.Replace("{" + kv.Key + "}", this.uriHelpers.UrlEncode(kv.Value)));

            // If we have query parameters, build the query string.
            if (queryParameters != null && queryParameters.Any())
                baseUriString += '?' + this.queryStringHelpers.BuildQueryString(queryParameters);

            // Parse it as an actual Uri.
            var endpointUri = new Uri(baseUriString, UriKind.RelativeOrAbsolute);

            // If this is an absolute Uri, we can return now.
            if (endpointUri.IsAbsoluteUri)
                return endpointUri;

            // Otherwise, combine it with the entity.
            var entityUri = new Uri(this.target.Entity, UriKind.Absolute);
            return new Uri(entityUri, endpointUri);
        }

        #endregion

        //public Task<TentPost<T>> RetrievePostForUserAsync<T>(TentPost<TentContentMeta> metaPost, ITentHawkSignature credentials, string postId, string versionId = null) where T : class
        //{
        //    // Retrieve the Uri for this post.
        //    var parameters = new Dictionary<string, string>
        //    {
        //        { "post", postId }
        //    };

        //    IDictionary<string, object> queryParameters = null;
        //    if (!string.IsNullOrWhiteSpace(versionId))
        //    {
        //        queryParameters = new Dictionary<string, object>
        //        {
        //            { "version", versionId }
        //        };
        //    }

        //    var postUri = this.GetEndpointUriFromMetaPost(metaPost, "post", parameters, queryParameters);
        //    return this.RetrievePostAtUriAsync<T>(postUri, credentials);
        //}

        //public Task<IList<TentPost<T>>> RetrievePublicationsForUserAsync<T>(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials) where T : class
        //{
        //    var postFeedUri = this.GetEndpointUriFromMetaPost(metaPost, "posts_feed", null, parameters.ToDictionary());
        //    return this.RetrievePostsAtUriAsync<T>(postFeedUri, credentials);
        //}

        //public Task<long> RetrievePublicationsCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials)
        //{
        //    var postFeedUri = this.GetEndpointUriFromMetaPost(metaPost, "posts_feed", null, parameters.ToDictionary());
        //    return this.RetrieveCountAtUriAsync(postFeedUri, this.tentConstants.PostFeedContentType, credentials);
        //}

        //public Task<IList<TentMention>> RetrieveMentionsForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, string versionId = null)
        //{
        //    // Create the Uri for this post.
        //    var uriParameters = new Dictionary<string, string>
        //    {
        //        { "post", postId }
        //    };

        //    var queryParameters = parameters.ToDictionary();
        //    if (!string.IsNullOrWhiteSpace(versionId))
        //    {
        //        queryParameters["version"] = versionId;
        //    }

        //    var postMentionsUri = this.GetEndpointUriFromMetaPost(metaPost, "post", uriParameters, queryParameters);
        //    return this.RetrieveMentionsAtUriAsync(postMentionsUri, credentials);
        //}

        //public Task<long> RetrieveMentionsCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId, string versionId = null)
        //{
        //    // Create the Uri for this post.
        //    var uriParameters = new Dictionary<string, string>
        //    {
        //        { "post", postId }
        //    };

        //    var queryParameters = parameters.ToDictionary();
        //    if (!string.IsNullOrWhiteSpace(versionId))
        //    {
        //        queryParameters["version"] = versionId;
        //    }

        //    var postMentionsUri = this.GetEndpointUriFromMetaPost(metaPost, "post", uriParameters, queryParameters);
        //    return this.RetrieveCountAtUriAsync(postMentionsUri, this.tentConstants.MentionsContentType, credentials);
        //}

        //public async Task<IList<TentVersion>> RetrieveVersionsForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId)
        //{
        //    // Create the Uri for this request.
        //    var uriParameters = new Dictionary<string, string>
        //    {
        //        { "post", postId }
        //    };

        //    var postVersionsUri = this.GetEndpointUriFromMetaPost(metaPost, "post", uriParameters, parameters.ToDictionary());
        //    var versions = await this.RetrieveVersionsAtUriAsync(postVersionsUri, this.tentConstants.VersionsContentType, credentials);

        //    // Add a UserId to the versions without entity.
        //    versions.Where(v => string.IsNullOrWhiteSpace(v.Entity))
        //        .ForEach(v => v.UserId = metaPost.UserId);

        //    return versions;
        //}

        //public Task<long> RetrieveVersionsCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId)
        //{
        //    // Create the Uri for this request.
        //    var uriParameters = new Dictionary<string, string>
        //    {
        //        { "post", postId }
        //    };

        //    var postVersionsUri = this.GetEndpointUriFromMetaPost(metaPost, "post", uriParameters, parameters.ToDictionary());
        //    return this.RetrieveCountAtUriAsync(postVersionsUri, this.tentConstants.VersionsContentType, credentials);
        //}

        //public async Task<IList<TentVersion>> RetrieveVersionChildrenForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId)
        //{
        //    // Create the Uri for this request.
        //    var uriParameters = new Dictionary<string, string>
        //    {
        //        { "post", postId }
        //    };

        //    var postVersionsUri = this.GetEndpointUriFromMetaPost(metaPost, "post", uriParameters, parameters.ToDictionary());
        //    var versions = await this.RetrieveVersionsAtUriAsync(postVersionsUri, this.tentConstants.VersionChildrenContentType, credentials);

        //    // Add a UserId to the versions without entity.
        //    versions.Where(v => string.IsNullOrWhiteSpace(v.Entity))
        //        .ForEach(v => v.UserId = metaPost.UserId);

        //    return versions;
        //}

        //public Task<long> RetrieveVersionChildrenCountForUserAsync(TentPost<TentContentMeta> metaPost, ITentRequestParameters parameters, ITentHawkSignature credentials, string postId)
        //{// Create the Uri for this request.
        //    var uriParameters = new Dictionary<string, string>
        //    {
        //        { "post", postId }
        //    };

        //    var postVersionsUri = this.GetEndpointUriFromMetaPost(metaPost, "post", uriParameters, parameters.ToDictionary());
        //    return this.RetrieveCountAtUriAsync(postVersionsUri, this.tentConstants.VersionChildrenContentType, credentials);
        //}

        //public Task<IHttpResponseMessage> GetAttachmentAsync(TentPost<TentContentMeta> metaPost, ITentHawkSignature credentials, string digest)
        //{
        //    var attachmentUri = this.GetEndpointUriFromMetaPost(metaPost, "attachment", new Dictionary<string, string>
        //    {
        //        { "digest", digest }
        //    });

        //    var request = this.requestFactory.Get(attachmentUri);

        //    // If needed, add an Authorization header to this request.
        //    if (credentials != null)
        //    {
        //        request.AddCredentials(credentials);
        //    }

        //    var client = this.CreateClient();
        //    return client.SendAsync(request);
        //}

        //public async Task<Uri> PostRelationshipAsync(string userHandle, TentPost<TentContentMeta> metaPost, TentPost<object> relationshipPost)
        //{
        //    Ensure.Argument.IsNotNullOrWhiteSpace(userHandle, nameof(userHandle));
        //    Ensure.Argument.IsNotNull(metaPost, nameof(metaPost));
        //    Ensure.Argument.IsNotNull(relationshipPost, nameof(relationshipPost));
        //    Ensure.Argument.IsNotNull(relationshipPost.PassengerCredentials, "relationshipPost.PassengerCredentials");

        //    // Generate a bewit signature for the credentials post.
        //    var bewit = await this.bewitLogic.CreateBewitForPostAsync(userHandle, relationshipPost.PassengerCredentials.Id);

        //    // Create the Uri for the request.
        //    var postUri = this.GetEndpointUriFromMetaPost(metaPost, "post", new Dictionary<string, string>
        //    {
        //        { "entity", relationshipPost.Entity },
        //        { "post", relationshipPost.Id }
        //    });

        //    // Create the request.
        //    var request = this.requestFactory
        //        .Put(postUri)
        //        .AddLink(this.uriHelpers.GetCamprPostBewitUri(userHandle, relationshipPost.PassengerCredentials.Id, bewit), this.tentConstants.CredentialsRel)
        //        .AddAccept(this.tentConstants.PostContentType)
        //        .AddContent(relationshipPost);

        //    var client = this.CreateClient();
        //    var response = await client.SendAsync(request);

        //    // Extract the Link header to credentials from the response.
        //    return response?.FindLinkInHeader(this.tentConstants.CredentialsRel);
        //}

        //public Task<bool> PostNotificationAsync(TentPost<TentContentMeta> metaPost, TentPost<object> post, ITentHawkSignature credentials)
        //{
        //    Ensure.Argument.IsNotNull(metaPost, nameof(metaPost));

        //    // Create the Uri for the request.
        //    var postUri = this.GetEndpointUriFromMetaPost(metaPost, "post", new Dictionary<string, string>
        //    {
        //        { "entity", post.Entity },
        //        { "post", post.Id }
        //    });

        //    return this.PostNotificationAtUriAsync(postUri, post, credentials);
        //}

        //public async Task<bool> PostNotificationAtUriAsync(Uri uri, TentPost<object> post, ITentHawkSignature credentials)
        //{
        //    // Create the request.
        //    var request = this.requestFactory
        //        .Put(uri)
        //        .AddContent(post);

        //    // If needed, add the credentials to the request.
        //    if (credentials != null)
        //    {
        //        request.AddCredentials(credentials);
        //    }

        //    var client = this.CreateClient();
        //    var response = await client.SendAsync(request);

        //    return response != null;
        //}

        //private async Task<IList<TentPost<T>>> RetrievePostsAtUriAsync<T>(Uri uri, ITentHawkSignature credentials) where T : class
        //{
        //    // Make the request.
        //    var postResult = await this.RetrievePostResultAtUriAsync<T>(uri, this.tentConstants.PostFeedContentType, credentials);
        //    if (postResult?.Posts == null)
        //    {
        //        return new List<TentPost<T>>();
        //    }

        //    // Remove non-valid posts.
        //    postResult.Posts = postResult.Posts.Where(p => p.Validate()).ToList();

        //    return postResult.Posts;
        //}

        //private async Task<IList<TentMention>> RetrieveMentionsAtUriAsync(Uri uri, ITentHawkSignature credentials)
        //{
        //    // Make the request.
        //    var postResult = await this.RetrievePostResultAtUriAsync<object>(uri, this.tentConstants.MentionsContentType, credentials);
        //    if (postResult?.Mentions == null)
        //    {
        //        return new List<TentMention>();
        //    }

        //    // Remove non-valid posts.
        //    postResult.Mentions = postResult.Mentions.Where(p => p.Validate()).ToList();

        //    return postResult.Mentions;
        //}

        //private async Task<IList<TentVersion>> RetrieveVersionsAtUriAsync(Uri uri, string accept, ITentHawkSignature credentials)
        //{
        //    // Make the request.
        //    var postResult = await this.RetrievePostResultAtUriAsync<object>(uri, accept, credentials);
        //    if (postResult?.Versions == null)
        //    {
        //        return new List<TentVersion>();
        //    }

        //    // Remove non-valid posts.
        //    postResult.Versions = postResult.Versions.Where(p => p.Validate()).ToList();

        //    return postResult.Versions;
        //}

        //private async Task<TentPostResult<T>> RetrievePostResultAtUriAsync<T>(Uri uri, string accept, ITentHawkSignature credentials) where T : class
        //{
        //    // Make the request.
        //    var request = this.requestFactory
        //        .Get(uri)
        //        .AddAccept(accept);

        //    if (credentials != null)
        //    {
        //        request.AddCredentials(credentials);
        //    }

        //    var client = this.CreateClient();
        //    var response = await client.SendAsync(request);
        //    if (response?.Content == null)
        //    {
        //        return null;
        //    }

        //    // Parse the content.
        //    var contentJsonString = await response.Content.ReadAsStringAsync();
        //    var postResult = this.jsonHelpers.FromJsonString<TentPostResult<T>>(contentJsonString);
        //    if (postResult == null)
        //    {
        //        return null;
        //    }

        //    // Extract the count from the response.
        //    int count;
        //    if (!response.Headers.Contains(this.tentConstants.CountHeaderName)
        //        || !response.Headers.GetValues(this.tentConstants.CountHeaderName).Any()
        //        || !int.TryParse(response.Headers.GetValues(this.tentConstants.CountHeaderName).First(), out count))
        //    {
        //        return postResult;
        //    }

        //    postResult.Count = count;

        //    return postResult;
        //}
    }
}