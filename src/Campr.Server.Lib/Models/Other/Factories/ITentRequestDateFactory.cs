using System;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Other.Factories
{
    public interface ITentRequestDateFactory
    {
        ITentRequestDate FromString(string date);
        ITentRequestDate FromPost(TentPost post, Func<TentPost, DateTime?> dateProperty);
        ITentRequestDate MinValue();
    }
}