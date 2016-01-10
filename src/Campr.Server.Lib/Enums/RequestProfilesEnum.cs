using System;

namespace Campr.Server.Lib.Enums
{
    [Flags]
    public enum RequestProfilesEnum
    {
        None = 0,
        Entity = 2,
        Refs = 4,
        Mentions = 8,
        Permissions = 16,
        Parents = 32
    }
}