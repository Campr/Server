using System.Globalization;
using System.Threading.Tasks;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Logic;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Models.Other
{
    public class TentRequestPost : ITentRequestPost
    {
        #region Constructor.
        
        public TentRequestPost(IUserLogic userLogic, IUriHelpers uriHelpers)
        {
            Ensure.Argument.IsNotNull(userLogic, nameof(userLogic));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));

            this.userLogic = userLogic;
            this.uriHelpers = uriHelpers;
        }

        private readonly IUserLogic userLogic;
        private readonly IUriHelpers uriHelpers;
        
        #endregion

        #region Public properties.

        public string UserId { get; set; }
        public string Entity { get; set; }
        public string PostId { get; set; }
        public User User { get; private set; }

        #endregion

        #region Methods.

        public async Task ResolveEntity()
        {
            this.User = await this.userLogic.GetUserAsync(this.Entity);
        }

        public bool Validate(bool requirePostId = false)
        {
            return !(this.User == null
                || (string.IsNullOrEmpty(this.PostId) && requirePostId));
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(this.PostId) 
                ? this.uriHelpers.UrlEncode(this.Entity)
                : string.Format(
                    CultureInfo.InvariantCulture, 
                    "{0} {1}", 
                    this.uriHelpers.UrlEncode(this.Entity), 
                    this.uriHelpers.UrlEncode(this.PostId));
        }

        #endregion
    }
}