using System.Net;

namespace Campr.Server.Lib.Exceptions
{
    public class StaleTimestampException : CustomHttpException
    {
        public StaleTimestampException(byte[] key) 
            : base(HttpStatusCode.Unauthorized, "Stale timestamp")
        {
            this.Key = key;
        }

        public byte[] Key { get; private set; }
    }
}