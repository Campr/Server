using System;

namespace Campr.Server.Lib.Models.Other.Factories
{
    public interface ITentPostTypeFactory
    {
        ITentPostType FromString(string postType, bool forceWildcard = false);
        ITentPostType FromUri(Uri postType, bool forceWildcard = false);
    }
}