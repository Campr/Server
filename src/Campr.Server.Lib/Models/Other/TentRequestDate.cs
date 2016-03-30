using System;
using System.Globalization;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Models.Other
{
    class TentRequestDate : ITentRequestDate
    {
        #region Constructor.

        public TentRequestDate(IUriHelpers uriHelpers)
        {
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            this.uriHelpers = uriHelpers;
        }

        private readonly IUriHelpers uriHelpers;

        #endregion

        #region Public properties.

        public DateTime Date { get; set; }
        public string Version { get; set; }

        #endregion
        
        #region Methods.

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(this.Version)
                ? this.Date.ToUnixTime().ToString()
                : string.Format(CultureInfo.InvariantCulture, "{0}+{1}", this.Date.ToUnixTime(), this.uriHelpers.UrlEncode(this.Version));
        }

        #endregion
    }
}
