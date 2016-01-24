using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Models.Other
{
    class TentHawkSignature : ITentHawkSignature
    {
        #region Constructor & Private variables.

        public TentHawkSignature(ICryptoHelpers cryptoHelpers, 
            ITextHelpers textHelpers, 
            IUriHelpers uriHelpers)
        {
            Ensure.Argument.IsNotNull(cryptoHelpers, nameof(cryptoHelpers));
            Ensure.Argument.IsNotNull(textHelpers, nameof(textHelpers));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));

            this.cryptoHelpers = cryptoHelpers;
            this.textHelpers = textHelpers;
            this.uriHelpers = uriHelpers;
        }

        private readonly ICryptoHelpers cryptoHelpers;
        private readonly ITextHelpers textHelpers;
        private readonly IUriHelpers uriHelpers;
        
        #endregion

        #region Interface implementation.

        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Nonce { get; set; }
        public string Mac { get; set; }
        public string ContentHash { get; set; }
        public string Extension { get; set; }
        public string App { get; set; }
        public HawkMacTypeEnum Type { get; set; }
        public byte[] Key { get; set; }

        public bool Validate(string verb, Uri targetUri, byte[] key)
        {
            // Update the target uri to always use the campr domain.
            targetUri = this.uriHelpers.GetCamprUriFromUri(targetUri);

            // Compute our own hash, and compare.
            var hash = this.CreateMac(verb, targetUri, key);
            return hash == this.Mac;
        }

        public string ToHeader(string verb, Uri targetUri)
        {
            // Set the timestamp and the nonce.
            this.Timestamp = DateTime.UtcNow;
            this.Nonce = this.textHelpers.GenerateUniqueId();

            // Compute the MAC.
            this.Mac = this.CreateMac(verb, targetUri);

            // Create the values dictionary.
            var values = new Dictionary<string, string>
            {
                { "id", this.Id },
                { "ts", this.Timestamp.ToSecondTime().ToString(CultureInfo.InvariantCulture) },
                { "nonce", this.Nonce },
                { "mac", this.Mac }
            };

            // If needed, add the content hash.
            if (!string.IsNullOrEmpty(this.ContentHash))
                values["hash"] = this.ContentHash;

            // If needed, add the extension.
            if (!string.IsNullOrEmpty(this.Extension))
                values["ext"] = this.Extension;

            // If needed, add the app.
            if (!string.IsNullOrEmpty(this.App))
                values["app"] = this.App;

            return string.Join(", ", values.Select(kv => $"{kv.Key}=\"{kv.Value}\""));
        }

        #endregion

        #region Private methods.

        private string CreateMac(string verb, Uri targetUri, byte[] key = null)
        {
            return this.cryptoHelpers.CreateMac(this.MacHeaderFromType(), this.Timestamp, this.Nonce, verb, targetUri, this.ContentHash, this.Extension, this.App, key ?? this.Key);
        }

        private string MacHeaderFromType()
        {
            return this.Type == HawkMacTypeEnum.Bewit ? "bewit" : "header";
        }

        #endregion
    }
}