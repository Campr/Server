using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Tent;
using Campr.Server.Lib.Models.Tent.PostContent;

namespace Campr.Server.Lib.Models.Other.Factories
{
    class TentHawkSignatureFactory : ITentHawkSignatureFactory
    {
        public TentHawkSignatureFactory(ICryptoHelpers cryptoHelpers, 
            ITextHelpers textHelpers, 
            IUriHelpers uriHelpers)
        {
            Ensure.Argument.IsNotNull(cryptoHelpers, "cryptoHelpers");
            Ensure.Argument.IsNotNull(textHelpers, "textHelpers");
            Ensure.Argument.IsNotNull(uriHelpers, "uriHelpers");

            this.cryptoHelpers = cryptoHelpers;
            this.textHelpers = textHelpers;
            this.uriHelpers = uriHelpers;
        }

        private readonly ICryptoHelpers cryptoHelpers;
        private readonly ITextHelpers textHelpers;
        private readonly IUriHelpers uriHelpers;
        
        public ITentHawkSignature FromAuthorizationHeader(string header)
        {
            Ensure.Argument.IsNotNullOrWhiteSpace(header, "header");

            var regex = new Regex("(id|ts|nonce|mac|ext|hash|app)=\"([^\"\\\\]*)\"");
            var matches = regex.Matches(header);

            var matchDictionary = new Dictionary<string, string>();
            foreach (Match match in matches)
            {
                matchDictionary[match.Groups[1].Value] = match.Groups[2].Value;
            }

            if (matchDictionary.Count < 4)
            {
                return null;
            }

            return new TentHawkSignature(this.cryptoHelpers, this.textHelpers, this.uriHelpers)
            {
                Id = matchDictionary["id"],
                Timestamp = long.Parse(matchDictionary["ts"]).FromSecondTime(),
                Nonce = matchDictionary["nonce"],
                Mac = matchDictionary["mac"],
                ContentHash = matchDictionary.TryGetValue("hash"),
                Extension = matchDictionary.TryGetValue("ext", string.Empty),
                App = matchDictionary.TryGetValue("app"),
                Type = HawkMacTypeEnum.Header
            }; 
        }

        public ITentHawkSignature FromBewit(string bewit)
        {
            // Fix the padding of the bewit string.
            if (bewit.Length % 4 > 0)
            {
                bewit = bewit.PadRight(bewit.Length + 4 - bewit.Length % 4, '=');
            }

            // Read the actual string.
            var bewitValue = Encoding.UTF8.GetString(Convert.FromBase64String(bewit));

            // Parse it.
            var bewitParts = bewitValue.Split('\\');
            if (bewitParts.Count() != 4)
            {
                return null;
            }

            // Parse the date.
            long unixDate;
            if (!long.TryParse(bewitParts[1], out unixDate))
            {
                return null;
            }

            return new TentHawkSignature(this.cryptoHelpers, this.textHelpers, this.uriHelpers)
            {
                Id = bewitParts[0],
                Timestamp = unixDate.FromSecondTime(),
                Nonce = string.Empty,
                Mac = bewitParts[2],
                Extension = bewitParts[3],
                Type = HawkMacTypeEnum.Bewit
            };
        }

        public ITentHawkSignature FromCredentials(TentPost<TentContentCredentials> credentials)
        {
            return new TentHawkSignature(this.cryptoHelpers, this.textHelpers, this.uriHelpers)
            {
                Id = credentials.Id,
                Key = Encoding.UTF8.GetBytes(credentials.Content.HawkKey),
                Type = HawkMacTypeEnum.Header
            };
        }
    }
}