using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db.Factories
{
    public interface ITentPostFactory
    {
        TentPost<T> FromContent<T>(User author, T content, ITentPostType type) where T : ModelBase;
    }
}