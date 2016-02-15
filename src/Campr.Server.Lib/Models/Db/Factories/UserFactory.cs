using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Models.Db.Factories
{
    class UserFactory : IUserFactory
    {
        public UserFactory(ITextHelpers textHelpers)
        {
            Ensure.Argument.IsNotNull(textHelpers, nameof(textHelpers));
            this.textHelpers = textHelpers;
        }

        private readonly ITextHelpers textHelpers;

        public User CreateUserFromEntity(string entity)
        {
            return new User
            {
                Id = this.textHelpers.GenerateUniqueId(),
                Entity = entity
            };
        }

        public User CreateUserFromHandle(string handle)
        {
            return new User
            {
                Id = this.textHelpers.GenerateUniqueId(),
                Handle = handle
            };
        }
    }
}