using System;
using System.Net;

namespace Campr.Server.Lib.Net.Exceptions
{
    public class HttpException : Exception
    {
        public HttpException(HttpStatusCode statusCode, string message = null)
        {
            // TODO: Do something with those values.
        }
    }
}
