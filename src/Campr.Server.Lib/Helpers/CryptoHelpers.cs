using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Helpers
{
    class CryptoHelpers : ICryptoHelpers
    {
        public CryptoHelpers(IUriHelpers uriHelpers,
            ITentConstants tentConstants)
        {
            Ensure.Argument.IsNotNull(uriHelpers, "uriHelpers");
            Ensure.Argument.IsNotNull(tentConstants, "tentConstants");

            this.tentConstants = tentConstants;
            this.uriHelpers = uriHelpers;
        }

        private readonly IUriHelpers uriHelpers;
        private readonly ITentConstants tentConstants;
        
        public byte[] HmacSha256Hash(byte[] key, byte[] content)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(content);
            }
        }

        public byte[] CreatePasswordKeyAndSalt(string password, out byte[] salt)
        {
            // Specify that we want to randomly generate a 20-byte salt.
            // Check if we can replace this with Scrypt or Bcrypt before release.
            using (var deriveBytes = new Rfc2898DeriveBytes(password, 20))
            {
                salt = deriveBytes.Salt;
                return deriveBytes.GetBytes(20);  // Derive a 20-byte key.
            }
        }

        public byte[] CreatePasswordKey(string password, byte[] salt)
        {
            // Specify the salt to use.
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt))
            {
                return deriveBytes.GetBytes(20);  // Derive a 20-byte key.
            }
        }

        public string EncryptString(string src, string key)
        {
            var srcKey = key.Split(';');
            var sKey = srcKey.First();
            var sIv = srcKey.Last();
            
            // Create the AES provider.
            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var encryptedStream = new MemoryStream())
            using (var cryptoStream = new CryptoStream(
                encryptedStream,
                aes.CreateEncryptor(Convert.FromBase64String(sKey), Convert.FromBase64String(sIv)),
                CryptoStreamMode.Write))
            {
                var plainBytes = Encoding.UTF8.GetBytes(src);
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();

                // Url encode the result and return.
                return this.uriHelpers.UrlTokenEncode(encryptedStream.ToArray());
            }
        }

        public string DecryptString(string src, string key)
        {
            var srcKey = key.Split(';');
            var sKey = srcKey.First();
            var sIv = srcKey.Last();

            // Create the AES provider.
            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var encryptedBytes = this.uriHelpers.UrlTokenDecode(src);
            using (var encryptedStream = new MemoryStream(encryptedBytes))
            using (var cryptoStream = new CryptoStream(
                encryptedStream,
                aes.CreateDecryptor(Convert.FromBase64String(sKey), Convert.FromBase64String(sIv)),
                CryptoStreamMode.Read))
            {
                var sessionKeyReader = new StreamReader(cryptoStream);
                return sessionKeyReader.ReadToEnd();
            }
        }

        public string ConvertToSha512TruncatedWithPrefix(string src, int length = 32)
        {
            return this.tentConstants.HashPrefix + this.ConvertToSha512Truncated(src, length);
        }

        public string ConvertToSha512TruncatedWithPrefix(byte[] src, int length = 32)
        {
            return this.tentConstants.HashPrefix + this.ConvertToSha512Truncated(src, length);
        }

        public string ConvertToSha512TruncatedWithPrefix(Stream src, int length = 32)
        {
            return this.tentConstants.HashPrefix + this.ConvertToSha512Truncated(src, length);
        }

        public string ConvertToSha512Truncated(string src, int length = 32)
        {
            return this.ConvertToSha512Truncated(Encoding.UTF8.GetBytes(src), length);
        }

        public string ConvertToSha512Truncated(byte[] src, int length = 32)
        {
            return this.ConvertToSha512Truncated(new MemoryStream(src), length);
        }

        public string ConvertToSha512Truncated(Stream src, int length = 32)
        {
            using (var sha512 = SHA512.Create())
            {
                // Compute the Hash.
                var hashBytes = sha512.ComputeHash(src);

                // Make a standard string version.
                var sb = new StringBuilder();
                for (var i = 0; i < length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public string CreateBewit(DateTime expiresAt, Uri uri, string ext, string bewitId, byte[] key)
        {
            // Create the mac hash.
            var mac = this.CreateMac("bewit", expiresAt, null, "GET", uri, null, ext, null, key);

            // Use it to create the bewit.
            var bewit = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}\\{2}\\{3}", bewitId, expiresAt.ToSecondTime(), mac, ext);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(bewit)).TrimEnd('=');
        }

        public string CreateMac(string header, DateTime timestamp, string nonce, string verb, Uri uri, string contentHash, string ext, string app, byte[] key)
        {
            // Escape the ext string.
            if (!string.IsNullOrEmpty(ext))
            {
                ext = ext.Replace("\\", "\\\\").Replace("\n", "\\n");
            }

            // Create our version of the hash.
            var hashContent = new List<string>
            {
                "hawk.1." + header,
                timestamp.ToSecondTime().ToString(CultureInfo.InvariantCulture),
                nonce,
                verb.ToUpper(),
                uri.PathAndQuery,
                uri.Host,
                uri.Port.ToString(CultureInfo.InvariantCulture),
                contentHash,
                ext
            };

            // If needed, add the App parameter.
            if (!string.IsNullOrEmpty(app))
            {
                hashContent.Add(app);
                hashContent.Add(string.Empty);
            }

            // Trailing breaks.
            hashContent.Add(string.Empty);

            var hashContentStr = string.Join("\n", hashContent);

            // Hash it using the HMAC256 algorithm.
            using (var hmac = new HMACSHA256(key))
            {
                var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashContentStr));
                return Convert.ToBase64String(hashValue);
            }
        }

        public string CreateStaleTimestampMac(DateTime timestamp, byte[] key)
        {
            // Create the unashed version of the mac.
            var hashContent = new List<string>
            {
                "hawk.1.ts",
                timestamp.ToSecondTime().ToString(CultureInfo.InvariantCulture)
            };

            var hashContentStr = string.Join("\n", hashContent);

            // Hash it using the HMAC256 algorithm.
            using (var hmac = new HMACSHA256(key))
            {
                var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashContentStr));
                return Convert.ToBase64String(hashValue);
            }
        }

        public string GenerateNewSecret()
        {
            return Encoding.ASCII.GetString(this.GenerateNewSecretBytes());
        }

        public byte[] GenerateNewSecretBytes()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var key = new byte[14];
                rng.GetBytes(key);
                return key;
            }
        }
    }
}