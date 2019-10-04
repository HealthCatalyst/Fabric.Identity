using System.IO;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Extensions;
using Fabric.Platform.Shared.Configuration.Docker;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Fabric.Identity.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((hostContext, config) =>
                    {
                        config.AddDockerSecrets(typeof(IAppConfiguration));
                        config.SetBasePath(hostContext.HostingEnvironment.ContentRootPath);
                    })
                .UseUrls("http://*:5001")
                .Build();
    }
}
