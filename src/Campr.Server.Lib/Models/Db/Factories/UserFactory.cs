using System;
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
            var currentDate = DateTime.UtcNow;
            return new User
            {
                Id = this.textHelpers.GenerateUniqueId(),
                Entities = { entity },
                CreatedAt = currentDate,
                UpdatedAt = currentDate
            };
        }

        public User CreateUserFromHandle(string handle)
        {
            var currentDate = DateTime.UtcNow;
            return new User
            {
                Id = this.textHelpers.GenerateUniqueId(),
                Handle = handle,
                CreatedAt = currentDate,
                UpdatedAt = currentDate
            };
        }
    }
}