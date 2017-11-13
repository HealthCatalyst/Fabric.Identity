using Fabric.Identity.API.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Fabric.Identity.API.Persistence
{
    public abstract class BaseIdentityServerConfigurator : IIdentityServerConfigurator
    {
        protected readonly IIdentityServerBuilder IdentityServerBuilder;
        protected readonly IServiceCollection ServiceCollection;
        protected readonly HostingOptions HostingOptions;
        protected readonly ILogger Logger;

        protected BaseIdentityServerConfigurator(IIdentityServerBuilder identityServerBuilder, IServiceCollection serviceCollection, HostingOptions hostingOptions, ILogger logger)
        {
            IdentityServerBuilder = identityServerBuilder;
            ServiceCollection = serviceCollection;
            HostingOptions = hostingOptions;
            Logger = logger;
        }

        public void Configure()
        {
            ConfigureInternalStores();
            ConfigureIdentityServer();
        }

        protected abstract void ConfigureInternalStores();
        protected abstract void ConfigureIdentityServer();
    }
}