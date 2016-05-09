using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Net.Base
{
    public interface IAuthenticatedHttpClientFactory
    {
        IAuthenticatedHttpClient MakeAuthenticatedHttpClient();
        IAuthenticatedHttpClient MakeAuthenticatedHttpClientWithCustomCredentials(TentPost<object> credentialsPost);
    }
}
