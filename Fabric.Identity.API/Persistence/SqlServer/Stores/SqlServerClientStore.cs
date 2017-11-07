using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerClientStore : IClientManagementStore
    {
        private readonly IIdentityDbContext _identityDbContext;

        public SqlServerClientStore(IIdentityDbContext identityDbContext)
        {
            _identityDbContext = identityDbContext;            
        }

        public async Task<IdentityServer4.Models.Client> FindClientByIdAsync(string clientId)
        {
            var client = await _identityDbContext.Clients
                .Include(x => x.ClientGrantTypes)
                .Include(x => x.ClientRedirectUris)
                .Include(x => x.ClientPostLogoutRedirectUris)
                .Include(x => x.ClientScopes)
                .Include(x => x.ClientSecrets)
                .Include(x => x.ClientClaims)
                .Include(x => x.ClientIdPrestrictions)
                .Include(x => x.ClientCorsOrigins)
                .FirstOrDefaultAsync(x => x.ClientId == clientId);
            var clientEntity = client?.ToModel();

            return clientEntity;
        }

        public IEnumerable<IdentityServer4.Models.Client> GetAllClients()
        {
            var clients = _identityDbContext.Clients
                .Where(c => !c.IsDeleted)
                .Select(c => c.ToModel());

            return clients;
        }

        public int GetClientCount()
        {
            return GetAllClients().Count();
        }

        public void AddClient(IdentityServer4.Models.Client client)
        {
            AddClientAsync(client).Wait();
        }

        public void UpdateClient(string clientId, IdentityServer4.Models.Client client)
        {
            UpdateClientAsync(clientId, client).Wait();
        }

        public void DeleteClient(string id)
        {
            DeleteClientAsync(id).Wait();
        }

        public async Task AddClientAsync(IdentityServer4.Models.Client client)
        {
            var domainModelClient = client.ToFabricEntity();

            //TODO: set entity model properties

            await _identityDbContext.Clients.AddAsync(domainModelClient);
        }

        public async Task UpdateClientAsync(string clientId, IdentityServer4.Models.Client client)
        {
            var clientDomainModel = client.ToFabricEntity();

            //TODO: set entity model properties

            _identityDbContext.Clients.Update(clientDomainModel);
            await _identityDbContext.SaveChangesAsync();
        }

        public async Task DeleteClientAsync(string id)
        {
            var clientToDelete =
               await _identityDbContext.Clients.FirstOrDefaultAsync(c =>
                    c.ClientId.Equals(id, StringComparison.OrdinalIgnoreCase));

            //TODO: set entity domain model properties

            clientToDelete.IsDeleted = true;

            await _identityDbContext.SaveChangesAsync();
        }
    }
}
