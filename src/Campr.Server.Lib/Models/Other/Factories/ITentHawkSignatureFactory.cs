using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;

namespace Campr.Server.Lib.Models.Other.Factories
{
    public interface ITentHawkSignatureFactory
    {
        ITentHawkSignature FromAuthorizationHeader(string header);
        ITentHawkSignature FromBewit(string bewit);
        ITentHawkSignature FromCredentials(TentPost<TentContentCredentials> credentials);
    }
}