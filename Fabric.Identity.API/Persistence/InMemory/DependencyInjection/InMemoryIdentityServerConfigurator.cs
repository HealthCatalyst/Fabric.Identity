using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Persistence.InMemory.Services;
using Fabric.Identity.API.Persistence.InMemory.Stores;
using Fabric.Identity.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace Fabric.Identity.API.Persistence.InMemory.DependencyInjection
{
    public class InMemoryIdentityServerConfigurator : BaseIdentityServerConfigurator
    {
        public InMemoryIdentityServerConfigurator(
            IIdentityServerBuilder identityServerBuilder,
            IServiceCollection serviceCollection,
            HostingOptions hostingOptions,
            ILogger logger)
            : base(identityServerBuilder, serviceCollection, hostingOptions, logger)
        {
        }

        protected override void ConfigureInternalStores()
        {
            ServiceCollection.TryAddSingleton<IDocumentDbService, InMemoryDocumentService>();
            ServiceCollection.AddTransient<IApiResourceStore, InMemoryApiResourceStore>();
            ServiceCollection.AddTransient<IIdentityResourceStore, InMemoryIdentityResourceStore>();
            ServiceCollection.AddTransient<IClientManagementStore, InMemoryClientManagementStore>();
            ServiceCollection.AddTransient<IUserStore, InMemoryUserStore>();
            ServiceCollection.AddTransient<IDbBootstrapper, InMemoryDbBootstrapper>();
            ServiceCollection.AddTransient<IdentityServer4.Stores.IPersistedGrantStore, InMemoryPersistedGrantStore>();
        }

        protected override void ConfigureIdentityServer()
        {
            IdentityServerBuilder
                .AddDeveloperSigningCredential()
                .AddTestUsersIfConfigured(HostingOptions)
                .AddCorsPolicyService<CorsPolicyService>()
                .AddResourceStore<InMemoryResourceStore>()
                .AddClientStore<InMemoryClientManagementStore>();
        }
    }
}