using System;
using System.Net;

namespace Campr.Server.Lib.Exceptions
{
    public class ApiException : Exception
    {
        public ApiException(HttpStatusCode statusCode, string errorMessage = null)
            : base(errorMessage)
        {
            this.StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; private set; }
    }
}