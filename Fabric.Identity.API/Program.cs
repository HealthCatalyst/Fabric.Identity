using System;
using System.IO;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Logging;
using Fabric.Identity.API.Services;
using Fabric.Platform.Shared.Configuration.Docker;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Fabric.Identity.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var loggerConfiguration = new LoggerConfiguration();

            //Create a builder just so that we can read the app insights instrumentation key from the config
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .Build();

            var certificateService = IdentityConfigurationProvider.MakeCertificateService();
            var decryptionService = new DecryptionService(certificateService);
            var appConfig = new IdentityConfigurationProvider(configuration).GetAppConfiguration(decryptionService);

            LogFactory.ConfigureTraceLogger(loggerConfiguration, appConfig.ApplicationInsights);

            Log.Logger = loggerConfiguration.CreateLogger();

            try
            {
                Log.Information("Starting web host");
                CreateWebHostBuilder(args).Build().Run();

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");

                Environment.Exit(1);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddDockerSecrets(typeof(IAppConfiguration));
                    config.SetBasePath(hostContext.HostingEnvironment.ContentRootPath);
                })
                .UseUrls("http://*:5001")
                .ConfigureKestrel((context, options) =>
                {
                    // Set properties and call methods on options
                })
                .UseSerilog()
                .UseStartup<Startup>();
    }
}
