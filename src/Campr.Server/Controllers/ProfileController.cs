using System.Net;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Exceptions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Logic;
using Microsoft.AspNet.Mvc;

namespace Campr.Server.Controllers
{
    public class ProfileController : Controller
    {
        public ProfileController(
            IUserLogic userLogic,
            IPostLogic postLogic,
            IUriHelpers uriHelpers, 
            ITentConstants tentConstants)
        {
            Ensure.Argument.IsNotNull(userLogic, nameof(userLogic));
            Ensure.Argument.IsNotNull(postLogic, nameof(postLogic));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            Ensure.Argument.IsNotNull(tentConstants, nameof(tentConstants));

            this.userLogic = userLogic;
            this.postLogic = postLogic;
            this.uriHelpers = uriHelpers;
            this.tentConstants = tentConstants;
        }

        private readonly IUserLogic userLogic;
        private readonly IPostLogic postLogic;
        private readonly IUriHelpers uriHelpers;
        private readonly ITentConstants tentConstants;
    
        [HttpHead("{userHandle}")]
        public async Task<IActionResult> HeadProfile(string userHandle = null)
        {
            // Add a Link header pointing towards the meta post for this user.
            await this.AddLinkHeader(userHandle);

            // Don't return anything. This is a HEAD request.
            return new NoContentResult();
        }

        [HttpGet("{userHandle")]
        public async Task<IActionResult> GetProfileRedirect(string userHandle = null)
        {
            // Add a Link header pointing towards the meta post for this user.
            await this.AddLinkHeader(userHandle);

            // Redirect the user.
            return new RedirectResult(this.uriHelpers.GetCamprUriFromPath(userHandle).ToString());
        }

        private async Task AddLinkHeader(string userHandle)
        {
            // If the UserHandle is null, try to get it from the domain.
            if (string.IsNullOrEmpty(userHandle) && !this.uriHelpers.IsCamprDomain(this.Request.Host.Value, out userHandle))
                throw new ApiException(HttpStatusCode.BadRequest);

            // Verify that this user exists.
            var user = await this.userLogic.GetUserAsync(userHandle);
            if (user == null)
                throw new ApiException(HttpStatusCode.NotFound);

            // Retrieve the meta post for this user.
            var metaPost = await this.postLogic.GetMetaPostForUserAsync(user);
            if (metaPost == null)
                throw new ApiException(HttpStatusCode.NotFound);

            // Add link headers to the response.
            this.Response.Headers.Add("Link", $"<{this.uriHelpers.GetCamprPostUri(userHandle, metaPost.Id).AbsoluteUri}>; " +
                                              $"rel=\"{this.tentConstants.MetaPostRel}\"");
        }
    }
}