using System;
using System.IO;

namespace Campr.Server.Lib.Helpers
{
    public interface ICryptoHelpers
    {
        byte[] HmacSha256Hash(byte[] key, byte[] content);
        byte[] CreatePasswordKeyAndSalt(string password, out byte[] salt);
        byte[] CreatePasswordKey(string password, byte[] salt);
        string EncryptString(string src, string key);
        string DecryptString(string src, string key);
        string ConvertToSha512TruncatedWithPrefix(string src, int length = 32);
        string ConvertToSha512TruncatedWithPrefix(byte[] src, int length = 32);
        string ConvertToSha512TruncatedWithPrefix(Stream src, int length = 32);
        string ConvertToSha512Truncated(string src, int length = 32);
        string ConvertToSha512Truncated(byte[] src, int length = 32);
        string ConvertToSha512Truncated(Stream src, int length = 32);
        string CreateBewit(DateTime expiresAt, Uri uri, string ext, string bewitId, byte[] key);
        string CreateMac(string header, DateTime timestamp, string nonce, string verb, Uri uri, string contentHash, string ext, string app, byte[] key);
        string CreateStaleTimestampMac(DateTime timestamp, byte[] key);
        string GenerateNewSecret();
        byte[] GenerateNewSecretBytes();
    }
}