using System;
using System.Linq;
using Campr.Server.Lib.Enums;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;

namespace Campr.Server.Lib.Models.Other.Factories
{
    class TentRequestDateFactory : ITentRequestDateFactory
    {
        public TentRequestDateFactory(IUriHelpers uriHelpers)
        {
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            this.uriHelpers = uriHelpers;

            // Build the MinValue TentRequestDate.
            this.minValue = new TentRequestDate(uriHelpers)
            {
                Date = DateTime.MinValue
            };
        }
        
        private readonly IUriHelpers uriHelpers;
        private readonly TentRequestDate minValue;

        public ITentRequestDate FromString(string date)
        {
            Ensure.Argument.IsNotNullOrWhiteSpace(date, nameof(date));
            var requestDateParts = date.Split('+');

            // Make sure we have something in the array.
            if (!requestDateParts.Any())
                throw new ArgumentOutOfRangeException(nameof(date), "The provided Tent date isn't valid.");

            // Try to extract the date.
            var dateValue = requestDateParts[0].TryParseLong();
            if (!dateValue.HasValue)
                throw new ArgumentOutOfRangeException(nameof(date), "The provided Tent date isn't valid.");

            // Create the resulting Request Date object.
            var result = new TentRequestDate(this.uriHelpers)
            {
                Date = dateValue.Value.FromUnixTime()
            };

            // Extract the version, if any.
            if (requestDateParts.Length > 1)
                result.Version = this.uriHelpers.UrlDecode(requestDateParts[1]);

            return result;
        }

        public ITentRequestDate FromPost(ITentRequestPost post, TentFeedRequestSort sortBy)
        {
            Ensure.Argument.IsNotNull(post, nameof(post));
            Ensure.Argument.IsNotNull(post.Post, nameof(post.Post));

            // Create the resulting Request Date object.
            var result = new TentRequestDate(this.uriHelpers)
            {
                Version = post.Post.Version.Id
            };

            // Pick the correct date on the specified post.
            switch (sortBy)
            {
                case TentFeedRequestSort.PublishedAt:
                    result.Date = post.Post.PublishedAt.GetValueOrDefault();
                    break;
                case TentFeedRequestSort.ReceivedAt:
                    result.Date = post.Post.ReceivedAt.GetValueOrDefault();
                    break;
                case TentFeedRequestSort.VersionPublishedAt:
                    result.Date = post.Post.Version.PublishedAt.GetValueOrDefault();
                    break;
                default:
                    result.Date = post.Post.Version.ReceivedAt.GetValueOrDefault();
                    break;
            }

            return result;
        }

        public ITentRequestDate MinValue()
        {
            return this.minValue;
        }
    }
}