using System;
using System.Diagnostics;
using Campr.Server.Lib.Services;

namespace Campr.Server.Tests.Infrastructure
{
    public class LoggingService : ILoggingService
    {
        public void Info(string str, params object[] strFormat)
        {
            Debug.WriteLine("Info: " + str, strFormat);
        }

        public void Error(string str, params object[] strFormat)
        {
            Debug.WriteLine("Error: " + str, strFormat);
        }

        public void Exception(Exception ex, string str, params object[] strFormat)
        {
            Debug.WriteLine("Exception: " + str, strFormat);
        }
    }
}