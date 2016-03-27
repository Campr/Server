﻿using System;
using System.Linq;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Other.Factories
{
    class TentRequestDateFactory : ITentRequestDateFactory
    {
        public TentRequestDateFactory(
            IServiceProvider serviceProvider, 
            IUriHelpers uriHelpers)
        {
            Ensure.Argument.IsNotNull(serviceProvider, nameof(serviceProvider));
            Ensure.Argument.IsNotNull(uriHelpers, nameof(serviceProvider));

            this.serviceProvider = serviceProvider;
            this.uriHelpers = uriHelpers;

            // Build the MinValue TentRequestDate.
            this.minValue = this.serviceProvider.Resolve<TentRequestDate>();
            this.minValue.Date = DateTime.MinValue;
        }

        private readonly IServiceProvider serviceProvider;
        private readonly IUriHelpers uriHelpers;
        private readonly TentRequestDate minValue;

        public ITentRequestDate FromString(string date)
        {
            Ensure.Argument.IsNotNullOrWhiteSpace(date, nameof(date));

            var result = this.serviceProvider.Resolve<TentRequestDate>();
            var requestDateParts = date.Split('+', ' ');

            // Make sure we have something in the array.
            if (!requestDateParts.Any())
                throw new ArgumentOutOfRangeException(nameof(date), "The provided Tent date isn't valid.");

            // Try to extract the date.
            var dateValue = requestDateParts[0].TryParseLong();
            if (dateValue.HasValue)
            {
                result.Date = dateValue.Value.FromUnixTime();

                // Extract the version, if any.
                if (requestDateParts.Length > 1)
                    result.Version = this.uriHelpers.UrlDecode(requestDateParts[1]);
            }
            // Otherwise, this may be entity + postId situation.
            else if (requestDateParts.Length > 1)
            {
                result.Entity = this.uriHelpers.UrlDecode(requestDateParts[0]);
                result.PostId = this.uriHelpers.UrlDecode(requestDateParts[1]);
            }

            return result;
        }

        public ITentRequestDate FromPost(ITentRequestPost post)
        {
            Ensure.Argument.IsNotNull(post, nameof(post));
            // TODO.
            return null;
        }

        public ITentRequestDate MinValue()
        {
            return this.minValue;
        }
    }
}