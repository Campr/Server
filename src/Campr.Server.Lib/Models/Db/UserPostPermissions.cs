using System.Collections.Generic;
using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Db
{
    public class UserPostPermissions
    {
        [DbProperty]
        public bool Public { get; set; }

        [DbProperty]
        public IList<string> UserIds { get; set; } 
    }
}