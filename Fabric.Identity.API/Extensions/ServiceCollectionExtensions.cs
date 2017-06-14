using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.DocumentDbStores;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Fabric.Identity.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFluentValidations(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ClientValidator, ClientValidator>();
            serviceCollection.AddTransient<IdentityResourceValidator, IdentityResourceValidator>();
            serviceCollection.AddTransient<ApiResourceValidator, ApiResourceValidator>();
        }

        public static IServiceCollection AddCouchDbBackedIdentityServer(this IServiceCollection serviceCollection, 
            ICouchDbSettings couchDbSettings, 
            string issuerUri, 
            SigningCertificateSettings certificateSettings, 
            ILogger logger)
        {

            serviceCollection.AddSingleton<IDocumentDbService, CouchDbAccessService>();
            serviceCollection.AddSingleton(couchDbSettings);
            serviceCollection.AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                    options.IssuerUri = issuerUri;
                })
                .AddSigningCredentialAndValidationKeys(certificateSettings, logger)
                .AddTestUsers(TestUsers.Users)
                .AddCorsPolicyService<CorsPolicyDocumentDbService>()
                .AddResourceStore<DocumentDbResourceStore>()
                .AddClientStore<DocumentDbClientStore>()
                .Services.AddTransient<IPersistedGrantStore, DocumentDbPersistedGrantStore>();

            return serviceCollection;
        }

        public static IServiceCollection AddInMemoryIdentityServer(this IServiceCollection serviceCollection, string issuerUri)
        {
            serviceCollection.AddSingleton<IDocumentDbService, InMemoryDocumentService>();
            serviceCollection.AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                    options.IssuerUri = issuerUri;
                })
                .AddTemporarySigningCredential()
                .AddTestUsers(TestUsers.Users)
                .AddCorsPolicyService<CorsPolicyDocumentDbService>()
                .AddResourceStore<DocumentDbResourceStore>()
                .AddClientStore<DocumentDbClientStore>()
                .Services.AddTransient<IPersistedGrantStore, DocumentDbPersistedGrantStore>();

            return serviceCollection;
        }

        public static IServiceCollection AddIdentityServer(this IServiceCollection serviceCollection,
            IAppConfiguration appConfiguration, ILogger logger)
        {
            if (appConfiguration.HostingOptions.UseInMemoryStores)
            {
                serviceCollection.AddInMemoryIdentityServer(appConfiguration.IssuerUri);
            }
            else
            {
                serviceCollection.AddCouchDbBackedIdentityServer(appConfiguration.CouchDbSettings, appConfiguration.IssuerUri, appConfiguration.SigningCertificateSettings, logger);
            }
            return serviceCollection;
        }
    }
}
