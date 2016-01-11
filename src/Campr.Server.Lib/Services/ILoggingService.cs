using System;

namespace Campr.Server.Lib.Services
{
    public interface ILoggingService
    {
        void Info(string str, params object[] strFormat);
        void Error(string str, params object[] strFormat);
        void Exception(Exception ex, string str, params object[] strFormat);
    }
}