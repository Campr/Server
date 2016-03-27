using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db.Factories
{
    class TentPostFactory : ITentPostFactory
    {
        public TentPostFactory(
            IModelHelpers modelHelpers, 
            ITextHelpers textHelpers)
        {
            Ensure.Argument.IsNotNull(modelHelpers, nameof(modelHelpers));
            Ensure.Argument.IsNotNull(textHelpers, nameof(textHelpers));

            this.modelHelpers = modelHelpers;
            this.textHelpers = textHelpers;
        }

        private readonly ITextHelpers textHelpers;
        private readonly IModelHelpers modelHelpers;
        
        public ITentPostFactoryBuilder<T> FromContent<T>(User user, T content, ITentPostType type) where T : ModelBase
        {
            return new TentPostFactoryBuilder<T>(this.modelHelpers, new TentPost<T>
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