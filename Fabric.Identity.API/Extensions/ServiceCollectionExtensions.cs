using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.CouchDb;
using Fabric.Identity.API.Validation;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;

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

        public static IServiceCollection AddCouchDbBackedIdentityServer(this IServiceCollection serviceCollection, ICouchDbSettings couchDbSettings)
        {
            serviceCollection.AddSingleton<IDocumentDbService, CouchDbAccessService>();
            serviceCollection.AddSingleton(couchDbSettings);
            serviceCollection.AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                })
                .AddTemporarySigningCredential()
                .AddTestUsers(TestUsers.Users)
                .AddCorsPolicyService<CorsPolicyService>()
                .AddResourceStore<CouchDbResourcesStore>()
                .AddClientStore<CouchDbClientStore>()
                .Services.AddTransient<IPersistedGrantStore, CouchDbPersistedGrantStore>();

            return serviceCollection;
        }

        public static IServiceCollection AddInMemoryIdentityServer(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                })
                .AddTemporarySigningCredential()
                .AddTestUsers(TestUsers.Users)
                .AddInMemoryApiResources(Config.GetApiResources())
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryClients(Config.GetClients());

            return serviceCollection;
        }
    }
}
