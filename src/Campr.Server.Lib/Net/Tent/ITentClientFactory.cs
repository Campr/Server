using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;

namespace Campr.Server.Lib.Net.Tent
{
    public interface ITentClientFactory
    {
        ISimpleTentClient Make();
        ITentClient Make(TentPost<TentContentMeta> target);
        ITentClient MakeAuthenticated(TentPost<TentContentMeta> target, ITentHawkSignature credentials);
    }
}