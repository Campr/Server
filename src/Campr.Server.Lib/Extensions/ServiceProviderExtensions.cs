using System;

namespace Campr.Server.Lib.Extensions
{
    public static class ServiceProviderExtensions
    {
        public static T Resolve<T>(this IServiceProvider provider)
        {
            return (T)provider.GetService(typeof (T));
        }
    }
}