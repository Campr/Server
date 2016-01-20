using Campr.Server.Lib;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Models.Db.Factories;
using Campr.Server.Lib.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Campr.Server.TestClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Initialize dependency injection.
            var services = new ServiceCollection();

            // Register required dependencies.
            services.AddSingleton<IExternalConfiguration, TestExternalConfiguration>();
            CamprCommonInitializer.Register(services);

            var container = services.BuildServiceProvider();

            var userFactory = container.GetService<IUserFactory>();
            var userRepository = container.GetService<IUserRepository>();

            //// Create first user.
            //var user = userFactory.CreateUserFromHandle("quentez");
            //user.Email = "quentez@outlook.com";

            //userRepository.UpdateAsync(user).Wait();

            //// Create first external user.
            //var externalUser = userFactory.CreateUserFromEntity("https://titanous.tent.is");
            //userRepository.UpdateAsync(externalUser).Wait();

            //// Retrieve a user by handle.
            //var user = userRepository.GetFromHandleAsync("quentez").Result;

            //// Retrieve a user by entity.
            //var user = userRepository.GetFromEntityAsync("https://quentez.tent.is").Result;

            //// Update an existing user.
            //var user = userRepository.GetFromHandleAsync("quentez").Result;
            //user.Email = "email2@gmail.com";
            //userRepository.UpdateAsync(user).Wait();

        }
    }
}
