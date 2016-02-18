using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Helpers
{
    public interface IModelHelpers
    {
        string GetUserEntity(User user);
        string GetVersionIdFromPost<T>(TentPost<T> post) where T : class;
        string GetShortVersionId(string versionId);
    }
}