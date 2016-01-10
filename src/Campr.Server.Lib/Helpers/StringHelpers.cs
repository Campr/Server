using System;

namespace Campr.Server.Lib.Helpers
{
    class StringHelpers : IStringHelpers
    {
        public string GenerateRandomNonce()
        {
            return Guid.NewGuid().ToString("n").Substring(0, 10);
        }
    }
}