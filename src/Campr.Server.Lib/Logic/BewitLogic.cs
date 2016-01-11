using System;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Repositories;

namespace Campr.Server.Lib.Logic
{
    class BewitLogic : IBewitLogic
    {
        public BewitLogic(IBewitRepository bewitRepository,
            IBewitFactory bewitFactory, 
            ICryptoHelpers cryptoHelpers, 
            IUriHelpers uriHelpers, 
            IGeneralConfiguration configuration)
        {
            Ensure.Argument.IsNotNull(bewitRepository, nameof(bewitRepository));
            Ensure.Argument.IsNotNull(bewitFactory, nameof(bewitFactory));
            Ensure.Argument.IsNotNull(cryptoHelpers, nameof(cryptoHelpers));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));

            this.bewitRepository = bewitRepository;
            this.bewitFactory = bewitFactory;
            this.cryptoHelpers = cryptoHelpers;
            this.uriHelpers = uriHelpers;
            this.configuration = configuration;
        }

        private readonly IBewitRepository bewitRepository;
        private readonly IBewitFactory bewitFactory;
        private readonly ICryptoHelpers cryptoHelpers;
        private readonly IUriHelpers uriHelpers;
        private readonly IGeneralConfiguration configuration;

        public async Task<string> CreateBewitForPostAsync(string userHandle, string postId, TimeSpan? expiresIn = null)
        {
            // Compute the expiration date for this bewit.
            var expiresAt = DateTime.UtcNow + expiresIn.GetValueOrDefault(this.configuration.DefaultBewitExpiration);

            // Create the bewit object and save it.
            var bewit = this.bewitFactory.FromExpirationDate(expiresAt);
            await this.bewitRepository.UpdateBewitAsync(bewit);

            // Generate the bewit signature.
            return this.cryptoHelpers.CreateBewit(expiresAt, this.uriHelpers.GetCamprPostUri(userHandle, postId), null, bewit.Id, bewit.Key);
        }
    }
}