using System;
using System.Collections.Generic;
using Campr.Server.Lib.Json;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db
{
    public class User : DbModelBase
    {
        #region Public properties.

        [DbProperty]
        public string Id { get; set; }

        [DbProperty]
        public string Handle { get; set; }

        [DbProperty]
        public IList<string> Entities { get; set; }

        [DbProperty]
        public string Email { get; set; }

        [DbProperty]
        public byte[] Password { get; set; }

        [DbProperty]
        public byte[] PasswordSalt { get; set; }

        [DbProperty]
        public bool IsBotFollowed { get; set; }

        [DbProperty]
        public DateTime? LastDiscoveryAttempt { get; set; }

        [DbProperty]
        public DateTime CreatedAt { get; set; }

        [DbProperty]
        public DateTime UpdatedAt { get; set; }

        [DbProperty]
        public DateTime? DeletedAt { get; set; }

        #endregion

        #region Public methods.

        public bool IsInternal()
        {
            return !string.IsNullOrEmpty(this.Handle);
        }

        #endregion

        public override string GetId()
        {
            return this.Id;
        }
    }
}