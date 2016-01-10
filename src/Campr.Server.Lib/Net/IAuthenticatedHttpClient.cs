using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Net
{
    public interface IAuthenticatedHttpClient : IHttpClient
    {
        void SetCredentials(TentPost<object> credentialsPost);
    }
}