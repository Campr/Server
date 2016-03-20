using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db.Factories
{
    class TentPostFactory : ITentPostFactory
    {
        public TentPostFactory(ITextHelpers textHelpers)
        {
            Ensure.Argument.IsNotNull(textHelpers, nameof(textHelpers));
            this.textHelpers = textHelpers;
        }

        private readonly ITextHelpers textHelpers;
        
        public ITentPostFactoryBuilder<T> FromContent<T>(User user, T content, string type) where T : ModelBase
        {
            return new TentPostFactoryBuilder<T>(new TentPost<T>
            {
                Id = this.textHelpers.GenerateUniqueId(),
                UserId = user.Id,
                Entity = user.Entity,
                Content = content,
                Type = type,
                Version = new TentVersion
                {
                    UserId = user.Id,
                    Entity = user.Entity,
                    Type = type
                },
                Permissions = new TentPermissions
                {
                    Public = true
                }
            });
        }
    }
}