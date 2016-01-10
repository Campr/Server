using System.Collections.Generic;
using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentPermissions : ModelBase
    {
        public bool IsDefault()
        {
            return this.Public.GetValueOrDefault();
        }

        [DbProperty]
        public IList<string> UserIds { get; set; }

        [DbProperty]
        [WebProperty]
        public List<string> Entities { get; set; }
        
        [DbProperty]
        [WebProperty]
        public bool? Public { get; set; }
        
        [DbProperty]
        [WebProperty]
        public List<TentPermissionGroup> Groups { get; set; }
    }
}