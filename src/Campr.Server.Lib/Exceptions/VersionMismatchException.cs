using System;

namespace Campr.Server.Lib.Exceptions
{
    public class VersionMismatchException : Exception
    {
        public VersionMismatchException(string expectedVersion, string computedVersion) 
            : base($"Tent post version mismatch: Computed {computedVersion}, Expected {expectedVersion}")
        {
            
        }
    }
}