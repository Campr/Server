using System;
using Campr.Server.Lib.Enums;

namespace Campr.Server.Lib.Models.Other
{
    public interface ITentHawkSignature
    {
        string Id { get; }
        DateTime Timestamp { get; }
        string Nonce { get; }
        string Mac { get; }
        string ContentHash { get; }
        string Extension { get; }
        string App { get; }
        HawkMacType Type { get; }

        bool Validate(string verb, Uri targetUri, byte[] key);
        string ToHeader(string verb, Uri targetUri);
    }
}