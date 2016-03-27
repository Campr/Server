using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Other
{
    public interface ITentRequestPost
    {
        User User { get; }
        TentPost Post { get; }
        Task Resolve();
    }
}