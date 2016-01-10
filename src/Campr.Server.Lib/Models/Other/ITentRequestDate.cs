using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Other
{
    public interface ITentRequestDate
    {
        DateTime? Date { get; }
        string Version { get; }
        bool IsValid { get; }

        Task ResolvePost(Func<TentPost, DateTime?> sortPropertySelector);
    }
}