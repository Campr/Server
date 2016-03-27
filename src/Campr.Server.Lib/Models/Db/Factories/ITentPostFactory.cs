using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db.Factories
{
    public interface ITentPostFactory
    {
        ITentPostFactoryBuilder<T> FromContent<T>(User user, T content, ITentPostType type) where T : ModelBase;
    }
}