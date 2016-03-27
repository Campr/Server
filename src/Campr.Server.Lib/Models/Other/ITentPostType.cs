using System;

namespace Campr.Server.Lib.Models.Other
{
    public interface ITentPostType : IEquatable<ITentPostType>
    {
        string Type { get; }
        string SubType { get; }
        bool WildCard { get; }
    }
}