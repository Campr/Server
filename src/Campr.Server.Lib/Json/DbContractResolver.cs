using Campr.Server.Lib.Helpers;

namespace Campr.Server.Lib.Json
{
    class DbContractResolver : BaseContractResolver, IDbContractResolver
    {
        public DbContractResolver(ITextHelpers textHelpers)
            : base(textHelpers, typeof(DbPropertyAttribute))
        {
        }
    }
}