using System;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Json;
using Campr.Server.Lib.Models.Tent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Models.Db
{
    public class User : DbVersionModelBase
    {
        #region Public properties.

        [DbProperty]
        public string Handle { get; set; }

        [DbProperty]
        public string Entity { get; set; }

        [DbProperty]
        public string Email { get; set; }

        [DbProperty]
        public byte[] Password { get; set; }

        [DbProperty]
        public byte[] PasswordSalt { get; set; }

        [DbProperty]
        public bool? IsBotFollowed { get; set; }

        [DbProperty]
        public DateTime? LastDiscoveryAttempt { get; set; }

        [DbProperty]
        public DateTime? DeletedAt { get; set; }

        #endregion

        #region Public methods.

        public bool IsInternal()
        {
            return !string.IsNullOrWhiteSpace(this.Handle);
        }

        public override JObject GetCanonicalJson(IJsonHelpers jsonHelpers)
        {
            var result = new JObject
            {
                {"id", this.Id},
                {"created_at", this.CreatedAt.GetValueOrDefault()},
                {"updated_at", this.UpdatedAt.GetValueOrDefault()},
                {"handle", this.Handle},
                {"entity", this.Entity},
                {"email", this.Email},
                {"password", this.Password},
                {"password_salt", this.PasswordSalt},
                {"is_bot_followed", this.IsBotFollowed},
                {"last_discovery_attempt", this.LastDiscoveryAttempt.GetValueOrDefault()},
                {"deleted_at", this.DeletedAt.GetValueOrDefault()}
            };

            return result.Sort();
        }

        #endregion
    }
}