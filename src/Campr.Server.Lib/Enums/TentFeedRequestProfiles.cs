using System;

namespace Campr.Server.Lib.Enums
{
    [Flags]
    public enum TentFeedRequestProfiles
    {
        None = 0,
        Entity = 1,
        Mentions = 2,
        Refs = 4,
        Parents = 8,
        Permissions = 16
    }
}