using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Net
{
    public interface IAuthenticatedHttpClientFactory
    {
        IAuthenticatedHttpClient MakeAuthenticatedHttpClient();
        IAuthenticatedHttpClient MakeAuthenticatedHttpClientWithCustomCredentials(TentPost<object> credentialsPost);
    }
}
