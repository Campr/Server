using System;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Models.Other.Factories
{
    class TentPostTypeFactory : ITentPostTypeFactory
    {
        public ITentPostType FromString(string postType, bool forceWildcard)
        {
            Ensure.Argument.IsNotNullOrWhiteSpace(postType, nameof(postType));

            // Normalize the provided type.
            postType = postType.Trim().ToLower();
            return this.FromUri(new Uri(postType, UriKind.Absolute), forceWildcard);
        }

        public ITentPostType FromUri(Uri postTypeUri, bool forceWildcard)
        {
            Ensure.Argument.IsNotNull(postTypeUri, nameof(postTypeUri));

            // Create a new Post Type from the provided Uri.
            return new TentPostType(
                postTypeUri.AbsoluteUri.Substring(0, postTypeUri.AbsoluteUri.Length - postTypeUri.Fragment.Length),
                forceWildcard ? null : string.IsNullOrEmpty(postTypeUri.Fragment) || postTypeUri.Fragment.Length <= 1 ? string.Empty : postTypeUri.Fragment.Substring(1),
                forceWildcard || string.IsNullOrEmpty(postTypeUri.Fragment)
            );
        }
    }
}