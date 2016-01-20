using System;
using Campr.Server.Lib;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Campr.Server.Tests.TestInfrastructure
{
    public class ServiceProvider
    {
        private static readonly Lazy<IServiceProvider> CurrentServiceProvider = new Lazy<IServiceProvider>(() =>
        {
            // Initialize dependency injection.
            var services = new ServiceCollection();

            // Register core components.
            services.AddSingleton<IExternalConfiguration, TestConfiguration>();
            services.AddSingleton<ILoggingService, LoggingService>();
            CamprCommonInitializer.Register(services);
            services.AddSingleton<BucketConfigurator>();

            // Create the final container.
            return services.BuildServiceProvider();
        });

        public static IServiceProvider Current => ServiceProvider.CurrentServiceProvider.Value;
    }
}