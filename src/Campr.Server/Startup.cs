using Campr.Server.Lib;
using Campr.Server.Lib.Json;
using Campr.Server.Middleware;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Campr.Server
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            this.configuration = builder.Build();

            // Create JSON serializer settings.
            this.webJsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        private readonly IConfigurationRoot configuration;
        private readonly JsonSerializerSettings webJsonSerializerSettings;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Register core components.
            CamprCommonInitializer.Register(services);

            // Configure Dependency Injection.
            services.AddMvc(options =>
            {
                // Configure input Json formatter.
                options.InputFormatters.Clear();
                options.InputFormatters.Add(new JsonInputFormatter
                {
                    SerializerSettings = this.webJsonSerializerSettings
                });

                // Configure output Json formatter.
                options.OutputFormatters.Clear();
                options.OutputFormatters.Add(new JsonOutputFormatter
                {
                    SerializerSettings = this.webJsonSerializerSettings
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(this.configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseIISPlatformHandler();
            app.UseStaticFiles();

            // Use our custom query parameters parser.
            app.UseTentParameters();

            // Use our custom authentication middleware.
            app.UseHawkAuthentication(new HawkOptions
            {
                AuthenticationScheme = "Hawk"
            });

            // Use the builtin MVC framework.
            app.UseMvc();

            // Resolve our custom contract resolver and update the JSON settings.
            this.webJsonSerializerSettings.ContractResolver = app.ApplicationServices.GetService<IWebContractResolver>();
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
