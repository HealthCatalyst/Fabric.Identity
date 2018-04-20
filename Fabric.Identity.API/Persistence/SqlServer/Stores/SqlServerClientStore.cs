using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Events;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Fabric.Identity.API.Services;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerClientStore : SqlServerBaseStore, IClientManagementStore
    {
        public SqlServerClientStore(IIdentityDbContext identityDbContext,
            IEventService eventService,
            IUserResolverService userResolverService,
            ISerializationSettings serializationSettings) : base(identityDbContext, eventService, userResolverService,
            serializationSettings)
        {
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            var client = IdentityDbContext.Clients
                .Include(x => x.ClientGrantTypes)
                .Include(x => x.ClientRedirectUris)
                .Include(x => x.ClientPostLogoutRedirectUris)
                .Include(x => x.ClientScopes)
                .Include(x => x.ClientSecrets)
                .Include(x => x.ClientClaims)
                .Include(x => x.ClientIdpRestrictions)
                .Include(x => x.ClientCorsOrigins)
                .Where(c => !c.IsDeleted)
                .FirstOrDefault(x => x.ClientId == clientId);
            var clientEntity = client?.ToModel();

            return Task.FromResult(clientEntity);
        }

        public IEnumerable<Client> GetAllClients()
        {
            var clients = IdentityDbContext.Clients
                .Include(x => x.ClientGrantTypes)
                .Include(x => x.ClientRedirectUris)
                .Include(x => x.ClientPostLogoutRedirectUris)
                .Include(x => x.ClientScopes)
                .Include(x => x.ClientSecrets)
                .Include(x => x.ClientClaims)
                .Include(x => x.ClientIdpRestrictions)
                .Include(x => x.ClientCorsOrigins)
                .Where(c => !c.IsDeleted)
                .Select(c => c.ToModel());

            return clients;
        }

        public int GetClientCount()
        {
            return IdentityDbContext.Clients.Count();
        }

        public void AddClient(Client client)
        {
            AddClientAsync(client).Wait();
        }

        public void UpdateClient(string clientId, Client client)
        {
            UpdateClientAsync(clientId, client).Wait();
        }

        public void DeleteClient(string id)
        {
            DeleteClientAsync(id).Wait();
        }

        public async Task AddClientAsync(Client client)
        {
            var domainModelClient = client.ToEntity();

            IdentityDbContext.Clients.Add(domainModelClient);
            await IdentityDbContext.SaveChangesAsync();
            await EventService.RaiseAsync(
                new EntityCreatedAuditEvent<Client>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    client.ClientId,
                    client,
                    SerializationSettings));
        }

        public async Task UpdateClientAsync(string clientId, Client client)
        {
            var existingClient = await IdentityDbContext.Clients.FirstOrDefaultAsync(c =>
                c.ClientId.Equals(client.ClientId, StringComparison.OrdinalIgnoreCase));

            client.ToEntity(existingClient);

            IdentityDbContext.Clients.Update(existingClient);
            await IdentityDbContext.SaveChangesAsync();
            await EventService.RaiseAsync(
                new EntityUpdatedAuditEvent<Client>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    client.ClientId,
                    client,
                    SerializationSettings));
        }

        public async Task DeleteClientAsync(string id)
        {
            var clientToDelete =
                await IdentityDbContext.Clients.FirstOrDefaultAsync(c =>
                    c.ClientId.Equals(id, StringComparison.OrdinalIgnoreCase));

            clientToDelete.IsDeleted = true;

            await IdentityDbContext.SaveChangesAsync();
            await EventService.RaiseAsync(
                new EntityDeletedAuditEvent<Client>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    clientToDelete.ClientId,
                    clientToDelete.ToModel(),
                    SerializationSettings));
        }
    }
}