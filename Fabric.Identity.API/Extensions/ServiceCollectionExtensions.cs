using System;
using System.Linq;
using Fabric.Identity.API.Authorization;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Infrastructure;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Persistence.Couchdb.Configuration;
using Fabric.Identity.API.Persistence.CouchDb.Services;
using Fabric.Identity.API.Persistence.CouchDb.Stores;
using Fabric.Identity.API.Persistence.InMemory.Services;
using Fabric.Identity.API.Persistence.InMemory.Stores;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using Fabric.Platform.Shared.Exceptions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            serviceCollection.AddTransient<UserApiModelValidator, UserApiModelValidator>();
            serviceCollection.AddTransient<ExternalProviderApiModelValidator>();
        }

        public static IServiceCollection AddCouchDbBackedIdentityServer(this IServiceCollection serviceCollection,
            ICouchDbSettings couchDbSettings,
            IAppConfiguration appConfiguration,
            ICertificateService certificateService,
            ILogger logger)
        {
            serviceCollection.AddSingleton<IDocumentDbService, CouchDbAccessService>();
            serviceCollection.AddSingleton(couchDbSettings);
            serviceCollection.AddTransient<ICorsPolicyProvider, FabricCorsPolicyProvider>();
            serviceCollection.AddTransient<IClientManagementStore, CouchDbClientStore>();
            serviceCollection.AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.IssuerUri = appConfiguration.IssuerUri;
                })
                .AddSigningCredentialAndValidationKeys(appConfiguration.SigningCertificateSettings, certificateService,
                    logger)
                .AddTestUsersIfConfigured(appConfiguration.HostingOptions)
                .AddCorsPolicyService<CorsPolicyDocumentDbService>()
                .AddResourceStore<CouchDbApiResourceStore>()
                .AddResourceStore<CouchDbIdentityResourceStore>()
                .AddClientStore<CouchDbClientStore>()
                .Services.AddTransient<IPersistedGrantStore, CouchDbPersistedGrantStore>();

            return serviceCollection;
        }

        public static IServiceCollection AddInMemoryIdentityServer(this IServiceCollection serviceCollection,
            IAppConfiguration appConfiguration)
        {
            serviceCollection.TryAddSingleton<IDocumentDbService, InMemoryDocumentService>();
            serviceCollection.AddTransient<IClientManagementStore, InMemoryClientManagementStore>();
            serviceCollection.AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.IssuerUri = appConfiguration.IssuerUri;
                })
                .AddTemporarySigningCredential()
                .AddTestUsersIfConfigured(appConfiguration.HostingOptions)
                .AddCorsPolicyService<CorsPolicyDocumentDbService>()
                .AddResourceStore<CouchDbResourceStore>()
                .AddClientStore<CouchDbClientStore>()
                .Services.AddTransient<IPersistedGrantStore, CouchDbPersistedGrantStore>();


            return serviceCollection;
        }

        public static IServiceCollection
            AddScopedDecorator<TService, TImplementation>(this IServiceCollection serviceCollection)
            where TService : class where TImplementation : class, TService
        {
            //since the asp .net core built in dependency injection does not provide
            //a mechanism for registering decorators, we have to do it ourselves
            //adapted from a similar method in IdentityServer4
            var registration = GetRegistration<TService>(serviceCollection);
            serviceCollection.Remove(registration);
            var type = GetImplementationType(registration);
            var innerType =
                typeof(Decorator<,>).MakeGenericType(typeof(TService), type);
            serviceCollection.Add(new ServiceDescriptor(typeof(Decorator<TService>), innerType,
                ServiceLifetime.Singleton));
            serviceCollection.TryAddScoped<TService, TImplementation>();
            serviceCollection.Add(new ServiceDescriptor(type, type, registration.Lifetime));
            return serviceCollection;
        }

        private static ServiceDescriptor GetRegistration<TService>(IServiceCollection serviceCollection)
        {
            var registration = serviceCollection.FirstOrDefault(r => r.ServiceType == typeof(TService));
            if (registration == null)
            {
                throw new FabricConfigurationException(
                    $"Service {typeof(TService).Name} is not yet registered, please register before trying to decorate it.");
            }
            return registration;
        }

        private static Type GetImplementationType(ServiceDescriptor registration)
        {
            if (registration.ImplementationType != null)
            {
                return registration.ImplementationType;
            }

            if (registration.ImplementationInstance != null)
            {
                return registration.ImplementationInstance.GetType();
            }

            throw new FabricConfigurationException("No IDocumentDbService is registered");
        }

        public static IServiceCollection AddIdentityServer(this IServiceCollection serviceCollection,
            IAppConfiguration appConfiguration, ICertificateService certificateService, ILogger logger)
        {
            serviceCollection.AddSingleton<IUserStore, CouchDbUserStore>();
            serviceCollection.AddSingleton<IProfileService, UserProfileService>();

            if (appConfiguration.HostingOptions.UseInMemoryStores)
            {
                serviceCollection.AddInMemoryIdentityServer(appConfiguration);
            }
            else
            {
                serviceCollection.AddCouchDbBackedIdentityServer(appConfiguration.CouchDbSettings, appConfiguration,
                    certificateService, logger);
            }

            return serviceCollection;
        }

        public static IServiceCollection AddAuthorizationServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IAuthorizationHandler, RegistrationAuthorizationHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, ReadAuthorizationHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, SearchUserAuthorizationHandler>();

            serviceCollection.AddAuthorization(options =>
            {
                options.AddPolicy(FabricIdentityConstants.AuthorizationPolicyNames.RegistrationThreshold,
                    policy => policy.Requirements.Add(new RegisteredClientThresholdRequirement(1)));
                options.AddPolicy(FabricIdentityConstants.AuthorizationPolicyNames.ReadScopeClaim,
                    policy => policy.Requirements.Add(new ReadScopeRequirement()));
                options.AddPolicy(
                    FabricIdentityConstants.AuthorizationPolicyNames.SearchUsersScopeClaim,
                    policy => policy.Requirements.Add(new SearchUserScopeRequirement()));
            });

            return serviceCollection;
        }
    }
}