using Fabric.Identity.API.Persistence.SqlServer.Services;
using Fabric.Identity.API.Services;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public abstract class SqlServerBaseStore
    {
        protected IIdentityDbContext IdentityDbContext;
        protected IUserResolverService UserResolverService;
        protected IEventService EventService;
        protected ISerializationSettings SerializationSettings;
        protected IdentityServerOptions Options;
        protected IClientStore Inner;
        protected ICache<EntityModels.Client> Cache;
        protected ILogger Logger;

        protected SqlServerBaseStore(IIdentityDbContext identityDbContext,
            IEventService eventService,
            IUserResolverService userResolverService,
            ISerializationSettings serializationSettings,
            IdentityServerOptions options,
            IClientStore inner,
            ICache<EntityModels.Client> cache,
            ILogger logger)
        {
            IdentityDbContext = identityDbContext;
            EventService = eventService;
            UserResolverService = userResolverService;
            SerializationSettings = serializationSettings;
            Options = options;
            Inner = inner;
            Cache = cache;
            Logger = logger;
        }
    }
}
