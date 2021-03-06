﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Models.Tent;
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

        public Task<string> CreateBewitForPostAsync(User user, TentPost post, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.CreateBewitForPostAsync(user, post, this.configuration.DefaultBewitExpiration, cancellationToken);
        }

        public async Task<string> CreateBewitForPostAsync(User user, TentPost post, TimeSpan expiresIn, CancellationToken cancellationToken = default(CancellationToken))
        {            
            // Compute the expiration date for this bewit.
            var expiresAt = DateTime.UtcNow + expiresIn;

            // Create the bewit object and save it.
            var bewit = this.bewitFactory.FromExpirationDate(expiresAt);
            await this.bewitRepository.UpdateAsync(bewit, cancellationToken);

            // Generate the bewit signature.
            return this.cryptoHelpers.CreateBewit(expiresAt, this.uriHelpers.GetCamprPostUri(user.Handle, post.Id), null, bewit.Id, bewit.Key);

        }
    }
}