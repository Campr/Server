using System;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Models.Db.Factories
{
    class BewitFactory : IBewitFactory
    {
        public BewitFactory(ICryptoHelpers cryptoHelpers, 
            ITextHelpers textHelpers)
        {
            Ensure.Argument.IsNotNull(cryptoHelpers, nameof(cryptoHelpers));
            Ensure.Argument.IsNotNull(textHelpers, nameof(textHelpers));
            this.cryptoHelpers = cryptoHelpers;
            this.textHelpers = textHelpers;
        }

        private readonly ICryptoHelpers cryptoHelpers;
        private readonly ITextHelpers textHelpers;
        
        public Bewit FromExpirationDate(DateTime expiresAt)
        {
            return new Bewit
            {
                Id = this.textHelpers.GenerateUniqueId(),
                ExpiresAt = expiresAt,
                Key = this.cryptoHelpers.GenerateNewSecretBytes()
            };
        }
    }
}