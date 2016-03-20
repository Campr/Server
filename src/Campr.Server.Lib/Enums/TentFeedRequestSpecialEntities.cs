using System;

namespace Campr.Server.Lib.Enums
{
    [Flags]
    public enum TentFeedRequestSpecialEntities
    {
        None = 0,
        Followings = 1,
        Followers = 2,
        Friends = 4
    }
}