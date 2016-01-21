using System;
using Campr.Server.Lib.Services;

namespace Campr.Server.Couchbase
{
    public class LoggingService : ILoggingService
    {
        public void Info(string str, params object[] strFormat)
        {
            Console.WriteLine("Info: " + str, strFormat);
        }

        public void Error(string str, params object[] strFormat)
        {
            Console.WriteLine("Error: " + str, strFormat);
        }

        public void Exception(Exception ex, string str, params object[] strFormat)
        {
            Console.WriteLine("Exception: " + str, strFormat);
        }
    }
}