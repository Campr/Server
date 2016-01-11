using System;
using Campr.Server.Lib.Json;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db
{
    public class Bewit : DbModelBase
    {
        [DbProperty]
        public string Id { get; set; }

        [DbProperty]
        public byte[] Key { get; set; }

        [DbProperty]
        public DateTime ExpiresAt { get; set; }

        public override string GetId()
        {
            return this.Id;
        }
    }
}