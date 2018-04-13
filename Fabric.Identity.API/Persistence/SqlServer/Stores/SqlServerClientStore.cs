using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;
using Fabric.Identity.API.Services;
using IdentityServer4.Services;
using Fabric.Identity.API.Events;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerClientStore : IClientManagementStore
    {
        private readonly IIdentityDbContext _identityDbContext;

        private readonly IUserResolverService _userResolverService;

        private readonly IEventService _eventService;

        private readonly ISerializationSettings _serializationSettings;

        public SqlServerClientStore(IIdentityDbContext identityDbContext, IEventService eventService, IUserResolverService userResolverService, ISerializationSettings serializationSettings)
        {
            _identityDbContext = identityDbContext;
            _eventService = eventService;
            _userResolverService = userResolverService;
            _serializationSettings = serializationSettings;
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            var client = _identityDbContext.Clients
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
            var clients = _identityDbContext.Clients
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
            return _identityDbContext.Clients.Count();
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

            _identityDbContext.Clients.Add(domainModelClient);
            await _identityDbContext.SaveChangesAsync();
            await _eventService.RaiseAsync(
                new EntityCreatedAuditEvent<Client>(
                    _userResolverService.Username,
                    _userResolverService.ClientId,
                    _userResolverService.Subject,
                    client.ClientId,
                    client,
                    _serializationSettings));
        }

        public async Task UpdateClientAsync(string clientId, Client client)
        {
            var existingClient = await _identityDbContext.Clients.FirstOrDefaultAsync(c =>
                c.ClientId.Equals(client.ClientId, StringComparison.OrdinalIgnoreCase));

            client.ToEntity(existingClient);

            _identityDbContext.Clients.Update(existingClient);
            await _identityDbContext.SaveChangesAsync();
            await _eventService.RaiseAsync(
                new EntityUpdatedAuditEvent<Client>(
                    _userResolverService.Username,
                    _userResolverService.ClientId,
                    _userResolverService.Subject,
                    client.ClientId,
                    client,
                    _serializationSettings));
        }

        public async Task DeleteClientAsync(string id)
        {
            var clientToDelete =
               await _identityDbContext.Clients.FirstOrDefaultAsync(c =>
                    c.ClientId.Equals(id, StringComparison.OrdinalIgnoreCase));

            clientToDelete.IsDeleted = true;

            await _identityDbContext.SaveChangesAsync();
            await _eventService.RaiseAsync(
                new EntityDeletedAuditEvent<Client>(
                    _userResolverService.Username,
                    _userResolverService.ClientId,
                    _userResolverService.Subject,
                    clientToDelete.ClientId,
                    clientToDelete.ToModel(),
                    _serializationSettings));
        }
    }
}
