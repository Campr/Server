using System.Collections.Generic;
using System.Linq;
using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentAppPostTypes : ModelBase
    {
        [DbProperty]
        [WebProperty]
        public IList<string> Read { get; set; }

        [DbProperty]
        [WebProperty]
        public IList<string> Write { get; set; }

        public IList<string> GetRead()
        {
            return this.Read ?? (this.Read = new List<string>());
        }

        public IEnumerable<string> GetAllRead()
        {
            return this.GetRead().Concat(this.GetWrite()).Distinct();
        }

        public IList<string> GetWrite()
        {
            return this.Write ?? (this.Write = new List<string>());
        }

        public bool IsReadMatch(string type)
        {
            return this.GetAllRead().Any(t =>
                t == "all"
                || t == type
                || (!t.Contains("#") && type.StartsWith(t)));
        }

        public bool IsWriteMatch(string type)
        {
            return this.GetWrite().Any(t =>
                t == "all"
                || t == type
                || (!t.Contains("#") && type.StartsWith(t)));
        }
    }
}