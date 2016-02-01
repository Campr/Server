using System;

namespace Campr.Server.Lib.Models.Other.Factories
{
    class TentPostTypeFactory : ITentPostTypeFactory
    {
        public ITentPostType FromString(string postType, bool forceWildcard)
        {
            postType = postType.Trim().ToLower();
            return this.FromUri(new Uri(postType, UriKind.Absolute), forceWildcard);
        }

        public ITentPostType FromUri(Uri postTypeUri, bool forceWildcard)
        {
            return new TentPostType
            {
                Type = postTypeUri.AbsoluteUri.Substring(0, postTypeUri.AbsoluteUri.Length - postTypeUri.Fragment.Length),
                SubType = forceWildcard
                    ? null
                    : string.IsNullOrEmpty(postTypeUri.Fragment) || postTypeUri.Fragment.Length <= 1 ? string.Empty : postTypeUri.Fragment.Substring(1),
                WildCard = forceWildcard || string.IsNullOrEmpty(postTypeUri.Fragment)
            };
        }
    }
}