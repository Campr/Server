using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Models.Other
{
    public interface ITentRequestPost
    {
        string Entity { get; }
        string PostId { get; }
        User User { get; }
        Task ResolveEntity();
    }
}