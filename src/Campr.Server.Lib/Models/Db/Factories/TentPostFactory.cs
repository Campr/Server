using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other;
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
        
        public TentPost<T> FromContent<T>(User author, T content, ITentPostType type) where T : ModelBase
        {
            return new TentPost<T>
            {
                Id = this.textHelpers.GenerateUniqueId(),
                UserId = author.Id,
                Content = content,
                Type = type.ToString(),
                Version = new TentVersion
                {
                    UserId = author.Id,
                    Type = type.ToString()
                }
            };
        }
    }
}