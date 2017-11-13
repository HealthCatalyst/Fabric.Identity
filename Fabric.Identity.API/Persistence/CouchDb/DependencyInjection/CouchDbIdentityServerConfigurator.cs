using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Persistence.CouchDb.Configuration;
using Fabric.Identity.API.Persistence.CouchDb.Services;
using Fabric.Identity.API.Persistence.CouchDb.Stores;
using Fabric.Identity.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace Fabric.Identity.API.Persistence.CouchDb.DependencyInjection
{
    public class CouchDbIdentityServerConfigurator : BaseIdentityServerConfigurator
    {
        private readonly ICertificateService _certificateService;
        private readonly ICouchDbSettings _couchDbSettings;
        private readonly SigningCertificateSettings _signingCertificateSettings;

        public CouchDbIdentityServerConfigurator(
            IIdentityServerBuilder identityServerBuilder,
            IServiceCollection serviceCollection,
            ICertificateService certificateService,
            SigningCertificateSettings signingCertificateSettings,
            HostingOptions hostingOptions,
            ICouchDbSettings couchDbSettings,
            ILogger logger) : base(identityServerBuilder, serviceCollection, hostingOptions, logger)
        {
            _certificateService = certificateService;
            _signingCertificateSettings = signingCertificateSettings;
            _couchDbSettings = couchDbSettings;
        }

        protected override void ConfigureInternalStores()
        {
            ServiceCollection.TryAddSingleton(_couchDbSettings);
            ServiceCollection.AddSingleton<IDocumentDbService, CouchDbAccessService>();
            ServiceCollection.AddScopedDecorator<IDocumentDbService, AuditingDocumentDbService>();
            ServiceCollection.AddTransient<IApiResourceStore, CouchDbApiResourceStore>();
            ServiceCollection.AddTransient<IIdentityResourceStore, CouchDbIdentityResourceStore>();
            ServiceCollection.AddTransient<IClientManagementStore, CouchDbClientStore>();
            ServiceCollection.AddTransient<IUserStore, CouchDbUserStore>();
            ServiceCollection.AddTransient<IDbBootstrapper, CouchDbBootstrapper>();
            ServiceCollection.AddTransient<IdentityServer4.Stores.IPersistedGrantStore, CouchDbPersistedGrantStore>();
        }

        protected override void ConfigureIdentityServer()
        {
            IdentityServerBuilder
                .AddSigningCredentialAndValidationKeys(
                    _signingCertificateSettings,
                    _certificateService,
                    Logger)
                .AddTestUsersIfConfigured(HostingOptions)
                .AddCorsPolicyService<CorsPolicyService>()
                .AddResourceStore<CouchDbResourceStore>()
                .AddClientStore<CouchDbClientStore>();
        }
    }
}
